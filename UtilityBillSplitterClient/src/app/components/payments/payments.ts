import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService, Payment, PaymentCreate, Bill, BillParticipant } from '../../services/api';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './payments.html',
  styleUrls: ['./payments.scss']
})
export class PaymentsComponent implements OnInit {
  payments: Payment[] = [];
  showCreateForm = false;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  paymentForm: FormGroup;

  bills: Bill[] = [];
  participants: BillParticipant[] = [];

  constructor(
    private apiService: ApiService,
    private authService: AuthService,
    private fb: FormBuilder,
    private router: Router
  ) {
    this.paymentForm = this.fb.group({
      amountPaid: ['', [Validators.required, Validators.min(0.01)]],
      method: ['', Validators.required],
      billId: ['', [Validators.required, Validators.min(1)]],
      billShareId: ['', [Validators.required, Validators.min(1)]],
      userId: ['', [Validators.required, Validators.min(1)]]
    });
  }

  ngOnInit() {
    this.loadPayments();
    this.loadBills();

    const currentUser = this.authService.currentUserValue;
    if (currentUser?.id) {
      this.paymentForm.patchValue({ userId: currentUser.id });
    }
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }

  loadBills() {
    this.apiService.getBills().subscribe({
      next: bills => { this.bills = bills; },
      error: err => console.error('Error loading bills:', err)
    });
  }

  onBillChange() {
    const billId = parseInt(this.paymentForm.value.billId);
    if (!billId) {
      this.participants = [];
      this.paymentForm.patchValue({ billShareId: '', amountPaid: '' });
      return;
    }

    this.apiService.getBill(billId).subscribe({
      next: bill => {
        // Only show participants with outstanding > 0
        this.participants = (bill.participants || []).filter((p: BillParticipant) => (p.outstanding ?? (p.shareAmount - (p.paidAmount || 0))) > 0);
        this.paymentForm.patchValue({ billShareId: '', amountPaid: '' });
      },
      error: err => {
        console.error('Error loading bill details:', err);
        this.participants = [];
      }
    });
  }

  onParticipantChange() {
    const shareId = parseInt(this.paymentForm.value.billShareId);
    const selected = this.participants.find(p => p.id === shareId);
    if (selected) {
      const outstanding = (selected.outstanding ?? (selected.shareAmount - (selected.paidAmount || 0)));
      this.paymentForm.patchValue({
        userId: selected.userId,
        amountPaid: Math.max(outstanding, 0)
      });
    }
  }

  loadPayments() {
    this.isLoading = true;
    this.errorMessage = '';
    this.apiService.getPayments().subscribe({
      next: (payments) => {
        this.payments = payments;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading payments:', error);
        this.errorMessage = 'Failed to load payments. Please try again.';
        this.isLoading = false;
      }
    });
  }

  createPayment() {
    if (this.paymentForm.valid) {
      // Client-side outstanding validation
      const selected = this.participants.find(p => p.id === parseInt(this.paymentForm.value.billShareId));
      const requested = parseFloat(this.paymentForm.value.amountPaid);
      const outstanding = selected ? (selected.outstanding ?? (selected.shareAmount - (selected.paidAmount || 0))) : 0;
      if (!selected || outstanding <= 0) {
        this.errorMessage = 'This participant has no outstanding amount.';
        return;
      }
      if (requested > outstanding) {
        this.errorMessage = `Amount exceeds outstanding (₹${outstanding}).`;
        return;
      }

      this.isLoading = true;
      this.errorMessage = '';
      this.successMessage = '';

      const paymentData: PaymentCreate = {
        amountPaid: requested,
        method: this.paymentForm.value.method,
        billShareId: parseInt(this.paymentForm.value.billShareId),
        userId: parseInt(this.paymentForm.value.userId)
      };

      this.apiService.createPayment(paymentData).subscribe({
        next: (newPayment) => {
          this.successMessage = 'Payment recorded successfully!';
          this.showCreateForm = false;
          this.paymentForm.reset();
          this.loadPayments();
          this.isLoading = false;
          // Refresh bill participants to refresh outstanding
          this.onBillChange();
        },
        error: (error) => {
          console.error('Error recording payment:', error);
          const serverMsg = (error?.error && typeof error.error === 'object') ? (error.error.message || JSON.stringify(error.error)) : (error?.error || error?.message);
          this.errorMessage = serverMsg || 'Failed to record payment. Please try again.';
          this.isLoading = false;
        }
      });
    }
  }

  cancelCreate() {
    this.showCreateForm = false;
    this.paymentForm.reset();
    this.errorMessage = '';
    this.successMessage = '';
  }

  getMethodClass(method: string): string {
    return `method-${method.toLowerCase().replace(' ', '-')}`;
  }
}
