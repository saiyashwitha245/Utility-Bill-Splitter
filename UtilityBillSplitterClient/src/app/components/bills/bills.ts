import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService, Bill, BillCreate, Group } from '../../services/api';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-bills',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './bills.html',
  styleUrls: ['./bills.scss']
})
export class BillsComponent implements OnInit {
  bills: Bill[] = [];
  showCreateForm = false;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  billForm: FormGroup;
  billsGroups: Group[] = [];

  constructor(
    private apiService: ApiService,
    private authService: AuthService,
    private fb: FormBuilder,
    private router: Router
  ) {
    this.billForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(2)]],
      amount: ['', [Validators.required, Validators.min(0.01)]],
      utilityType: ['', Validators.required],
      dueDate: ['', Validators.required],
      description: [''],
      groupId: ['', Validators.required]
    });
  }

  ngOnInit() {
    this.loadBills();
    // Load groups for the group selector
    this.apiService.getGroups().subscribe({
      next: (groups) => { this.billsGroups = groups; },
      error: () => { /* non-blocking */ }
    });
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }

  loadBills() {
    this.isLoading = true;
    this.errorMessage = '';
    
    this.apiService.getBills().subscribe({
      next: (bills) => {
        this.bills = bills;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading bills:', error);
        this.errorMessage = 'Failed to load bills. Please try again.';
        this.isLoading = false;
      }
    });
  }

  createBill() {
    if (this.billForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';
      this.successMessage = '';

      // Get current user ID
      const currentUser = this.authService.currentUserValue;
      if (!currentUser?.id) {
        this.errorMessage = 'User not authenticated';
        this.isLoading = false;
        return;
      }

      const groupId = parseInt(this.billForm.value.groupId, 10);
      if (!groupId) {
        this.errorMessage = 'Please select a group';
        this.isLoading = false;
        return;
      }

      // Fetch all group members to include everyone as bill participants
      this.apiService.getGroup(groupId).subscribe({
        next: (group: Group) => {
          const memberIds = (group?.members || []).map(m => m.userId);
          if (!memberIds.includes(currentUser.id)) {
            memberIds.push(currentUser.id);
          }

          const billData: BillCreate = {
            title: this.billForm.value.title,
            amount: parseFloat(this.billForm.value.amount),
            utilityType: this.billForm.value.utilityType,
            dueDate: this.billForm.value.dueDate,
            description: this.billForm.value.description || '',
            participantIds: memberIds,
            groupId: groupId,
            payerId: currentUser.id
          };

          this.apiService.createBill(billData).subscribe({
            next: (newBill) => {
              this.successMessage = 'Bill added successfully!';
              this.showCreateForm = false;
              this.billForm.reset();
              this.loadBills(); // Reload the bills list
              this.isLoading = false;
            },
            error: (error) => {
              console.error('Error creating bill:', error);
              this.errorMessage = 'Failed to add bill. Please try again.';
              this.isLoading = false;
            }
          });
        },
        error: (err) => {
          console.error('Failed to fetch group members:', err);
          // Fallback: create bill with only current user if group fetch fails
          const fallback: BillCreate = {
            title: this.billForm.value.title,
            amount: parseFloat(this.billForm.value.amount),
            utilityType: this.billForm.value.utilityType,
            dueDate: this.billForm.value.dueDate,
            description: this.billForm.value.description || '',
            participantIds: [currentUser.id],
            groupId: groupId,
            payerId: currentUser.id
          };

          this.apiService.createBill(fallback).subscribe({
            next: (newBill) => {
              this.successMessage = 'Bill added successfully!';
              this.showCreateForm = false;
              this.billForm.reset();
              this.loadBills();
              this.isLoading = false;
            },
            error: (error) => {
              console.error('Error creating bill:', error);
              this.errorMessage = 'Failed to add bill. Please try again.';
              this.isLoading = false;
            }
          });
        }
      });
    }
  }

  cancelCreate() {
    this.showCreateForm = false;
    this.billForm.reset();
    this.errorMessage = '';
    this.successMessage = '';
  }

  viewBill(billId: number) {
    this.router.navigate(['/bills', billId]);
  }

  editBill(billId: number) {
    this.router.navigate(['/bills', billId, 'edit']);
  }

  markPaid(billId: number) {
    this.apiService.markBillAsPaid(billId).subscribe({
      next: (updatedBill) => {
        this.successMessage = 'Bill marked as paid!';
        // Optimistically update local list so Edit/Mark Paid hide immediately
        const idx = this.bills.findIndex(b => b.id === billId);
        if (idx !== -1) {
          this.bills[idx] = { ...this.bills[idx], status: 'Paid' as const };
        }
        this.loadBills(); // Reload to reflect changes
      },
      error: (error) => {
        console.error('Error marking bill as paid:', error);
        this.errorMessage = 'Failed to mark bill as paid. Please try again.';
      }
    });
  }

  isOverdue(dueDate: string): boolean {
    const due = new Date(dueDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return due < today;
  }

  getUtilityClass(utilityType: string): string {
    return `utility-${utilityType.toLowerCase()}`;
  }

  allParticipantsPaid(bill: Bill): boolean {
    if (!bill || !bill.participants || bill.participants.length === 0) {
      return false;
    }
    return bill.participants.every(p => !!p.isPaid);
  }
}
