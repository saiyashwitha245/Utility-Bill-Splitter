import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService, Bill } from '../../services/api';

@Component({
  selector: 'app-bill-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './bill-detail.html',
  styleUrls: ['./bill-detail.scss']
})
export class BillDetailComponent implements OnInit {
  bill: Bill | null = null;
  isLoading = false;
  errorMessage = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiService
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.errorMessage = 'Invalid bill id';
      return;
    }
    this.loadBill(id);
  }

  loadBill(id: number) {
    this.isLoading = true;
    this.errorMessage = '';
    this.api.getBill(id).subscribe({
      next: (bill) => {
        this.bill = bill;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Failed to load bill', err);
        this.errorMessage = 'Failed to load bill details';
        this.isLoading = false;
      }
    });
  }

  goBack() {
    this.router.navigate(['/bills']);
  }
} 