import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login';
import { RegisterComponent } from './components/register/register';
import { DashboardComponent } from './components/dashboard/dashboard';
import { GroupsComponent } from './components/groups/groups';
import { BillsComponent } from './components/bills/bills';
import { PaymentsComponent } from './components/payments/payments';
import { NotificationsComponent } from './components/notifications/notifications';
import { authGuard, guestGuard, adminGuard } from './services/auth-guard';
import { BillDetailComponent } from './components/bill-detail/bill-detail';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'login', component: LoginComponent, canActivate: [guestGuard], title: 'Login - Bill Splitter' },
  { path: 'register', component: RegisterComponent, canActivate: [guestGuard], title: 'Register - Bill Splitter' },
  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard], title: 'Dashboard - Bill Splitter' },
  { path: 'groups', component: GroupsComponent, canActivate: [authGuard], title: 'Groups - Bill Splitter' },
  { path: 'bills', component: BillsComponent, canActivate: [authGuard], title: 'Bills - Bill Splitter' },
  { path: 'bills/:id', component: BillDetailComponent, canActivate: [authGuard], title: 'Bill Details - Bill Splitter' },
  { path: 'payments', component: PaymentsComponent, canActivate: [authGuard], title: 'Payments - Bill Splitter' },
  { path: 'notifications', component: NotificationsComponent, canActivate: [authGuard], title: 'Notifications - Bill Splitter' },
  // Admin
  { path: 'admin/login', loadComponent: () => import('./components/admin/admin-login').then(m => m.AdminLoginComponent), title: 'Admin Login' },
  { path: 'admin', loadComponent: () => import('./components/admin/admin-dashboard').then(m => m.AdminDashboardComponent), canActivate: [adminGuard], title: 'Admin' },
  { path: '**', redirectTo: '/login' }
];
