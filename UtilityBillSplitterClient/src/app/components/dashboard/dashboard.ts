import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { AuthService, User } from '../../services/auth';
import { ApiService } from '../../services/api';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl:'./dashboard.html' ,
  styleUrls: ['./dashboard.scss']
})
export class DashboardComponent implements OnInit {
  currentUser: User | null = null;
  isLoading = true;
  errorMessage = '';
  stats = {
    totalGroups: 0,
    pendingBills: 0,
    totalOwed: 0,
    unreadNotifications: 0
  };

  constructor(
    private authService: AuthService,
    private apiService: ApiService,
    private router: Router
  ) {}

  ngOnInit() {
    console.log('Dashboard component initialized');
    
    // Subscribe to current user
    this.authService.currentUser.subscribe((user: User | null) => {
      this.currentUser = user;
      console.log('Current user in dashboard:', user);
      
      if (user) {
        this.loadDashboardStats();
      }
    });

    // Refresh stats when navigating back to /dashboard
    this.router.events.subscribe((evt) => {
      if (evt instanceof NavigationEnd && evt.urlAfterRedirects === '/dashboard' && this.currentUser) {
        this.loadDashboardStats();
      }
    });
  }

  getUserInitials(): string {
    if (!this.currentUser?.username) return '?';
    return this.currentUser.username.charAt(0).toUpperCase();
  }

  navigateToGroups() {
    console.log('Navigating to groups');
    this.router.navigate(['/groups']);
  }

  navigateToBills() {
    console.log('Navigating to bills');
    this.router.navigate(['/bills']);
  }

  navigateToPayments() {
    this.router.navigate(['/payments']);
  }

  navigateToNotifications() {
    this.router.navigate(['/notifications']);
  }

  logout() {
    console.log('Logging out');
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  private loadDashboardStats() {
    this.isLoading = true;
    this.errorMessage = '';

    // Load groups, bills, and notifications in parallel
    Promise.all([
      this.apiService.getGroups().toPromise(),
      this.apiService.getBills().toPromise(),
      this.currentUser?.id ? this.apiService.getUserNotifications(this.currentUser.id).toPromise() : Promise.resolve([])
    ]).then(([groups, bills, notifications]) => {
      const userId = this.currentUser?.id;

      // Only groups where current user is a member
      const userGroups = (groups || []).filter((g: any) => Array.isArray(g?.members) && g.members.some((m: any) => m.userId === userId));
      this.stats.totalGroups = userGroups.length;
      
      // Only bills that include current user as a participant
      const userBills = (bills || []).filter((b: any) => Array.isArray(b?.participants) && b.participants.some((p: any) => p.userId === userId));
      const pendingBills = userBills.filter((bill: any) => bill.status === 'Pending' || bill.status === 'Overdue');
      this.stats.pendingBills = pendingBills.length;
      
      // Calculate total owed (sum of unpaid bills for current user)
      this.stats.totalOwed = pendingBills.reduce((total: number, bill: any) => {
        const userParticipant = bill.participants?.find((p: any) => p.userId === userId);
        if (userParticipant && !userParticipant.isPaid) {
          return total + userParticipant.shareAmount;
        }
        return total;
      }, 0);

      // Unread notifications count for the current user
      const unread = Array.isArray(notifications) ? notifications.filter((n: any) => !n.isRead).length : 0;
      this.stats.unreadNotifications = unread;

      this.isLoading = false;
    }).catch((error) => {
      console.error('Error loading dashboard stats:', error);
      this.errorMessage = 'Failed to load dashboard data. Please try refreshing the page.';
      this.isLoading = false;
    });
  }
}
