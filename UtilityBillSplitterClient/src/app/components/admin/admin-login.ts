import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../services/api';

@Component({
  standalone: true,
  selector: 'app-admin-login',
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="admin-login">
      <div class="card">
        <div class="logo">🔐</div>
        <h1>Admin Portal</h1>
        <p class="subtitle">Manage users and view activity</p>
        <form [formGroup]="form" (ngSubmit)="login()">
          <label>Username</label>
          <input formControlName="username" placeholder="admin username" />
          <label>Password</label>
          <input type="password" formControlName="password" placeholder="password" />
          <div class="actions">
            <button type="button" class="btn ghost" (click)="goBack()">← Back to Sign in</button>
            <button type="submit" class="btn primary" [disabled]="form.invalid || loading">{{ loading ? 'Signing in...' : 'Sign In' }}</button>
          </div>
          <div class="error" *ngIf="error">{{ error }}</div>
        </form>
      </div>
      <div class="bg"></div>
    </div>
  `,
  styles: [`
    .admin-login{min-height:100vh;display:flex;align-items:center;justify-content:center;position:relative;overflow:hidden}
    .bg{position:absolute;inset:0;background:linear-gradient(135deg,#0ea5e9 0%, #7c3aed 100%);filter:blur(60px);opacity:.35;z-index:0}
    .card{position:relative;z-index:1;max-width:460px;width:90%;background:#fff;border-radius:16px;padding:28px 24px;box-shadow:0 30px 60px rgba(0,0,0,.15)}
    .logo{width:60px;height:60px;border-radius:14px;display:flex;align-items:center;justify-content:center;background:linear-gradient(135deg,#0ea5e9,#7c3aed);color:#fff;font-size:28px;margin:0 auto 8px}
    h1{margin:6px 0 4px 0;text-align:center}
    .subtitle{text-align:center;color:#64748b;margin-bottom:18px}
    label{display:block;margin:12px 0 6px;font-weight:600;color:#334155}
    input{width:100%;padding:12px;border:1px solid #e2e8f0;border-radius:10px}
    .actions{display:flex;gap:10px;justify-content:space-between;margin-top:16px}
    .btn{padding:10px 14px;border-radius:10px;border:1px solid transparent;cursor:pointer;font-weight:700}
    .btn.primary{background:linear-gradient(135deg,#7c3aed,#0ea5e9);color:#fff}
    .btn.ghost{background:#f1f5f9;color:#334155;border-color:#e2e8f0}
    .btn[disabled]{opacity:.6;cursor:not-allowed}
    .error{color:#c62828;margin-top:12px;text-align:center}
  `]
})
export class AdminLoginComponent implements OnInit {
  form!: FormGroup;
  loading = false;
  error = '';

  constructor(private fb: FormBuilder, private api: ApiService, private router: Router) {}

  ngOnInit() {
    this.form = this.fb.group({
      username: ['', Validators.required],
      password: ['', Validators.required]
    });
  }

  goBack(){
    this.router.navigate(['/login']);
  }

  login() {
    if (this.form.invalid) return;
    this.loading = true; this.error = '';
    this.api.adminLogin(this.form.value as any).subscribe({
      next: (res) => {
        localStorage.setItem('adminToken', res.token);
        this.router.navigate(['/admin']);
      },
      error: (err) => {
        this.error = err?.error || 'Login failed';
        this.loading = false;
      }
    });
  }
} 