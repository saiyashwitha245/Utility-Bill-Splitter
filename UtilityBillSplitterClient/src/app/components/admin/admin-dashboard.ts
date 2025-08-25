import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService, User } from '../../services/api';

@Component({
  standalone: true,
  selector: 'app-admin-dashboard',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="toast" *ngIf="toastMessage" [class.success]="toastType==='success'" [class.error]="toastType==='error'">
      {{ toastMessage }}
    </div>

    <div class="admin">
      <div class="header">
        <h1>Admin</h1>
        <div class="actions">
          <button class="btn" (click)="loadUsers()" [disabled]="busy">Refresh</button>
          <button class="btn danger" (click)="logoutAdmin()">Admin Logout</button>
        </div>
      </div>

      <div class="tabs">
        <button [class.active]="tab==='users'" (click)="tab='users'">Users</button>
        <button [class.active]="tab==='logs'" (click)="loadLogs()">Activity Logs</button>
      </div>

      <div *ngIf="tab==='users'" class="panel">
        <table class="table">
          <thead>
            <tr><th>ID</th><th>Username</th><th>Email</th><th>Role</th><th>Actions</th></tr>
          </thead>
          <tbody>
            <tr *ngFor="let u of users">
              <td>{{u.id}}</td>
              <td>{{u.username}}</td>
              <td>{{u.email}}</td>
              <td>
                <select [(ngModel)]="u.role">
                  <option>Admin</option>
                  <option>Member</option>
                </select>
              </td>
              <td>
                <button class="btn" (click)="updateRole(u)" [disabled]="busy">Update Role</button>
                <button class="btn danger" (click)="deleteUser(u)" [disabled]="busy">Delete</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div *ngIf="tab==='logs'" class="panel">
        <div class="log" *ngFor="let l of logs">
          <div class="row"><strong>{{l.timestamp | date:'medium'}}</strong> — {{l.actor}} — {{l.action}}</div>
          <div class="desc">{{l.description}}</div>
        </div>
        <div *ngIf="!logs.length && !error" class="hint">No logs</div>
      </div>

      <div class="alert" *ngIf="error">{{error}}</div>
      <div class="alert success" *ngIf="success">{{success}}</div>
    </div>
  `,
  styles: [`
    .toast{position:fixed;top:12px;left:50%;transform:translateX(-50%);padding:10px 16px;border-radius:10px;color:#fff;z-index:1000;box-shadow:0 10px 30px rgba(0,0,0,.15);animation:slideDown .25s ease}
    .toast.success{background:#16a34a}
    .toast.error{background:#dc2626}
    @keyframes slideDown{from{opacity:0;transform:translate(-50%,-10px)}to{opacity:1;transform:translate(-50%,0)}}

    .admin{max-width:1000px;margin:24px auto;padding:0 16px}
    .header{display:flex;justify-content:space-between;align-items:center}
    .actions{display:flex;gap:8px}
    .tabs{display:flex;gap:8px;margin:16px 0}
    .tabs button{padding:8px 12px;border:1px solid #e2e8f0;background:#fff;border-radius:6px;cursor:pointer}
    .tabs button.active{background:#4f46e5;color:#fff;border-color:#4f46e5}
    .table{width:100%;border-collapse:collapse}
    .table th,.table td{padding:10px;border-bottom:1px solid #eaeaea}
    .btn{padding:6px 10px;border:none;border-radius:6px;background:#4f46e5;color:#fff;cursor:pointer}
    .btn.danger{background:#e11d48}
    .panel{background:#fff;border-radius:8px;box-shadow:0 8px 24px rgba(0,0,0,.06);padding:12px}
    .log{padding:8px 0;border-bottom:1px solid #f1f5f9}
    .desc{color:#64748b}
    .hint{color:#64748b;padding:8px 0}
    .alert{margin-top:12px;color:#c62828}
    .alert.success{color:#166534}
  `]
})
export class AdminDashboardComponent {
  tab: 'users'|'logs' = 'users';
  users: User[] = [];
  logs: any[] = [];
  error = '';
  success = '';
  busy = false;
  toastMessage = '';
  toastType: 'success'|'error'|'' = '';
  private toastTimer: any;

  constructor(private api: ApiService, private router: Router) {
    this.loadUsers();
  }

  private showToast(message: string, type: 'success'|'error'){ 
    this.toastMessage = message; this.toastType = type; 
    if (this.toastTimer) clearTimeout(this.toastTimer);
    this.toastTimer = setTimeout(() => { this.toastMessage = ''; this.toastType = ''; }, 2500);
  }

  loadUsers(preserveSuccess: boolean = false){
    this.error='';
    if (!preserveSuccess) this.success='';
    this.busy = true;
    this.api.adminGetUsers().subscribe({
      next: u => { this.users = u; this.busy = false; },
      error: err => { const msg = err?.error?.message || err?.error || 'Failed to load users'; this.error = msg; this.showToast(msg,'error'); this.busy = false; }
    });
  }

  loadLogs(){
    this.tab = 'logs'; this.error=''; this.success=''; this.busy = true;
    this.api.adminGetLogs().subscribe({
      next: l => {
        this.logs = (l || []).map((x: any) => ({
          timestamp: x.timestamp ?? x.Timestamp,
          actor: x.actor ?? x.Actor,
          action: x.action ?? x.Action,
          description: x.description ?? x.Description
        }));
        this.busy = false;
      },
      error: err => { const msg = err?.error?.message || err?.error || 'Failed to load logs'; this.error = msg; this.showToast(msg,'error'); this.busy = false; }
    });
  }

  updateRole(u: User){
    this.error=''; this.success=''; this.busy = true;
    this.api.adminUpdateRole(u.id, u.role).subscribe({
      next: (res: any) => { const msg = res?.message || 'Role updated'; this.success = msg; this.showToast(msg,'success'); this.loadUsers(true); },
      error: err => { const msg = err?.error?.message || err?.error || 'Failed to update role'; this.error = msg; this.showToast(msg,'error'); this.busy = false; }
    });
  }

  deleteUser(u: User){
    if (!confirm(`Delete user ${u.username}?`)) return;
    this.error=''; this.success=''; this.busy = true;
    this.api.adminDeleteUser(u.id).subscribe({
      next: (res: any) => { const msg = res?.message || 'User deleted'; this.success = msg; this.showToast(msg,'success'); this.loadUsers(true); },
      error: err => { const msg = err?.error?.message || err?.error || 'Failed to delete user'; this.error = msg; this.showToast(msg,'error'); this.busy = false; }
    });
  }

  logoutAdmin(){
    localStorage.removeItem('adminToken');
    this.router.navigate(['/admin/login']);
  }
} 