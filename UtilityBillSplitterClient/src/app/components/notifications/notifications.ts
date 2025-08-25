import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService, Notification, NotificationCreate, Group, GroupMember } from '../../services/api';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './notifications.html',
  styleUrls: ['./notifications.scss']
})
export class NotificationsComponent implements OnInit {
  notifications: Notification[] = [];
  showCreateForm = false;
  isLoading = false;
  errorMessage = '';
  successMessage = '';
  notificationForm: FormGroup;
  filterType: 'all' | 'unread' | 'read' = 'all';
  
  // New property to track which notifications have their details expanded
  expandedNotifications = new Set<number>();

  // New state for group/user selection
  groups: Group[] = [];
  members: GroupMember[] = [];
  selectedUserIds: number[] = [];
  showMemberError = false;

  constructor(
    private apiService: ApiService,
    private authService: AuthService,
    private fb: FormBuilder,
    private router: Router
  ) {
    this.notificationForm = this.fb.group({
      message: ['', Validators.required]
    });
  }

  ngOnInit() {
    this.loadNotifications();
    this.apiService.getGroups().subscribe({
      next: (groups) => { this.groups = groups; },
      error: () => {}
    });
  }

  goBack() {
    this.router.navigate(['/dashboard']);
  }

  // New method to toggle notification details visibility
  toggleNotificationDetails(notificationId: number) {
    if (this.expandedNotifications.has(notificationId)) {
      this.expandedNotifications.delete(notificationId);
    } else {
      this.expandedNotifications.add(notificationId);
    }
  }

  // New method to check if notification details are expanded
  isExpanded(notificationId: number): boolean {
    return this.expandedNotifications.has(notificationId);
  }

  onGroupChangeSelect(event: Event) {
    const val = (event.target as HTMLSelectElement)?.value || '';
    this.onGroupChange(val);
  }

  onMemberCheckboxChange(userId: number, event: Event) {
    const checked = (event.target as HTMLInputElement)?.checked ?? false;
    this.toggleUserSelection(userId, checked);
  }

  onGroupChange(groupIdValue: string) {
    this.members = [];
    this.selectedUserIds = [];
    this.showMemberError = false;
    const id = parseInt(groupIdValue, 10);
    if (!id) return;
    this.apiService.getGroup(id).subscribe({
      next: (group) => { this.members = group.members || []; },
      error: () => { this.members = []; }
    });
  }

  onMemberChange(event: Event, userId: number) {
    const checked = (event.target as HTMLInputElement)?.checked ?? false;
    this.toggleUserSelection(userId, checked);
  }

  toggleUserSelection(userId: number, checked: boolean) {
    this.showMemberError = false;
    if (checked) {
      if (!this.selectedUserIds.includes(userId)) this.selectedUserIds.push(userId);
    } else {
      this.selectedUserIds = this.selectedUserIds.filter(id => id !== userId);
    }
  }

  isUserSelected(userId: number): boolean {
    return this.selectedUserIds.includes(userId);
  }

  loadNotifications() {
    this.isLoading = true;
    this.errorMessage = '';
    this.apiService.getNotifications().subscribe({
      next: (notifications) => {
        this.notifications = notifications;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Error loading notifications:', error);
        this.errorMessage = 'Failed to load notifications. Please try again.';
        this.isLoading = false;
      }
    });
  }

  createNotification() {
    if (this.notificationForm.invalid) return;
    if (this.selectedUserIds.length === 0) {
      this.showMemberError = true;
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.successMessage = '';

    const message = this.notificationForm.value.message as string;
    let completed = 0;
    let success = 0;
    let failed = 0;

    const finalize = () => {
      if (++completed === this.selectedUserIds.length) {
        this.successMessage = success > 0 ? `Notification sent to ${success} user${success > 1 ? 's' : ''}${failed ? `, ${failed} failed` : ''}.` : '';
        if (failed && !success) {
          this.errorMessage = 'Failed to create notification. Please try again.';
        }
        this.showCreateForm = false;
        this.notificationForm.reset();
        this.isLoading = false;
        this.loadNotifications();
      }
    };

    this.selectedUserIds.forEach(userId => {
      const payload: NotificationCreate = { message, userId };
      this.apiService.createNotification(payload).subscribe({
        next: () => { success++; finalize(); },
        error: (err) => { console.error('Create notification error', err); failed++; finalize(); }
      });
    });
  }

  cancelCreate() {
    this.showCreateForm = false;
    this.notificationForm.reset();
    this.errorMessage = '';
    this.successMessage = '';
    this.members = [];
    this.selectedUserIds = [];
    this.showMemberError = false;
  }

  markAsRead(notificationId: number) {
    this.apiService.markNotificationAsRead(notificationId).subscribe({
      next: () => {
        // Update the local notification as read
        const notification = this.notifications.find(n => n.id === notificationId);
        if (notification) {
          notification.isRead = true;
        }
      },
      error: (error) => {
        console.error('Error marking notification as read:', error);
      }
    });
  }

  getNotificationEmoji(type: string): string {
    switch (type.toLowerCase()) {
      case 'info': return '💬';
      case 'warning': return '⚠️';
      case 'error': return '❌';
      case 'success': return '✅';
      default: return '📢';
    }
  }

  getNotificationIcon(type: string): string {
    return `icon-${type}`;
  }

  getTypeClass(type: string): string {
    return `type-${type.toLowerCase()}`;
  }

  getTimeAgo(date: string): string {
    const now = new Date();
    const notificationDate = new Date(date);
    const diffInSeconds = Math.floor((now.getTime() - notificationDate.getTime()) / 1000);

    if (diffInSeconds < 60) {
      return `${diffInSeconds}s ago`;
    } else if (diffInSeconds < 3600) {
      return `${Math.floor(diffInSeconds / 60)}m ago`;
    } else if (diffInSeconds < 86400) {
      return `${Math.floor(diffInSeconds / 3600)}h ago`;
    } else {
      return `${Math.floor(diffInSeconds / 86400)}d ago`;
    }
  }

  getNoResultsMessage(): string {
    if (this.filterType === 'all') {
      return 'Create your first notification to get started.';
    } else if (this.filterType === 'unread') {
      return 'No unread notifications yet. All notifications are marked as read.';
    } else { // filterType === 'read'
      return 'No read notifications yet. All notifications are marked as unread.';
    }
  }

  setFilter(type: 'all' | 'unread' | 'read') {
    this.filterType = type;
  }

  get filteredNotifications(): Notification[] {
    return this.notifications.filter(notification => {
      if (this.filterType === 'all') {
        return true;
      } else if (this.filterType === 'unread') {
        return !notification.isRead;
      } else { // filterType === 'read'
        return notification.isRead;
      }
    });
  }
}
