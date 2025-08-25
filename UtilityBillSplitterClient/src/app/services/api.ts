import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface GroupMember {
	id: number;
	groupId: number;
	userId: number;
	role: string;
	username: string;
	email: string;
	joinedAt: string;
}

export interface Group {
	id: number;
	name: string;
	description: string;
	memberCount: number;
	totalBills: number;
	activeBills: number;
	createdAt: string;
	createdById: number;
	members: GroupMember[];
}

export interface GroupCreate {
	name: string;
	description: string;
	createdById: number;
	memberIds?: number[];
}

export interface BillParticipant {
	id: number;
	billId: number;
	userId: number;
	username: string;
	email: string;
	shareAmount: number;
	isPaid: boolean;
	paidAmount?: number;
	outstanding?: number;
}

export interface Bill {
	id: number;
	title: string;
	totalAmount: number;
	utilityType: string;
	dueDate: string;
	status: 'Pending' | 'Paid' | 'Overdue';
	description: string;
	groupId: number;
	createdBy: number;
	createdAt: string;
	participants: BillParticipant[];
}

export interface BillCreate {
	title: string;
	amount: number;
	utilityType: string;
	dueDate: string;
	description: string;
	participantIds: number[];
	groupId: number;
	payerId: number;
}

export interface Payment {
	id: number;
	amount: number;
	method: string;
	paidAt: string;
	billId: number;
	userId: number;
}

export interface PaymentCreate {
	amountPaid: number;
	method: string;
	billShareId: number;
	userId: number;
}

export interface Notification {
	id: number;
	title: string;
	message: string;
	type: string;
	isRead: boolean;
	createdAt: string;
	userId: number;
	user?: {
		username: string;
		email: string;
	};
	timeAgo?: string;
	formattedDate?: string;
}

export interface NotificationCreate {
	message: string;
	userId: number;
}

export interface User {
	id: number;
	username: string;
	email: string;
	role: string;
}

@Injectable({
	providedIn: 'root'
})
export class ApiService {
	private apiUrl = 'https://localhost:7083/api';

	constructor(private http: HttpClient) { }

	// Helper: admin auth header
	private adminHeaders(): HttpHeaders {
		const token = localStorage.getItem('adminToken') || '';
		return new HttpHeaders({ 'Authorization': token ? `Bearer ${token}` : '' });
	}

	// Groups
	getGroups(): Observable<Group[]> {
		return this.http.get<Group[]>(`${this.apiUrl}/groups`);
	}

	getGroup(id: number): Observable<Group> {
		return this.http.get<Group>(`${this.apiUrl}/groups/${id}`);
	}

	createGroup(group: GroupCreate): Observable<Group> {
		return this.http.post<Group>(`${this.apiUrl}/groups`, group);
	}

	updateGroup(id: number, group: Partial<Group>): Observable<Group> {
		return this.http.put<Group>(`${this.apiUrl}/groups/${id}`, group);
	}

	deleteGroup(id: number): Observable<void> {
		return this.http.delete<void>(`${this.apiUrl}/groups/${id}`);
	}

	addMemberToGroup(groupId: number, userId: number): Observable<any> {
		return this.http.post<any>(`${this.apiUrl}/groups/${groupId}/members`, { userId });
	}

	// Bills
	getBills(): Observable<Bill[]> {
		return this.http.get<Bill[]>(`${this.apiUrl}/bills`);
	}

	getBill(id: number): Observable<Bill> {
		return this.http.get<Bill>(`${this.apiUrl}/bills/${id}`);
	}

	createBill(bill: BillCreate): Observable<Bill> {
		return this.http.post<Bill>(`${this.apiUrl}/bills`, bill);
	}

	updateBill(id: number, bill: Partial<Bill>): Observable<Bill> {
		return this.http.put<Bill>(`${this.apiUrl}/bills/${id}`, bill);
	}

	deleteBill(id: number): Observable<void> {
		return this.http.delete<void>(`${this.apiUrl}/bills/${id}`);
	}

	markBillAsPaid(id: number): Observable<Bill> {
		return this.http.patch<Bill>(`${this.apiUrl}/bills/${id}/mark-paid`, {});
	}

	// Payments
	getPayments(): Observable<Payment[]> {
		return this.http.get<Payment[]>(`${this.apiUrl}/payments`);
	}

	createPayment(payment: PaymentCreate): Observable<Payment> {
		return this.http.post<Payment>(`${this.apiUrl}/payments`, payment);
	}

	// Notifications
	getNotifications(): Observable<Notification[]> {
		return this.http.get<Notification[]>(`${this.apiUrl}/notifications`);
	}

	getUserNotifications(userId: number): Observable<Notification[]> {
		return this.http.get<Notification[]>(`${this.apiUrl}/notifications/user/${userId}`);
	}

	createNotification(notification: NotificationCreate): Observable<Notification> {
		return this.http.post<Notification>(`${this.apiUrl}/notifications`, notification);
	}

	markNotificationAsRead(id: number): Observable<Notification> {
		return this.http.patch<Notification>(`${this.apiUrl}/notifications/${id}/read`, {});
	}

	// Users
	getUsers(): Observable<User[]> {
		return this.http.get<User[]>(`${this.apiUrl}/auth/users`);
	}

	// Admin API
	adminLogin(body: { username: string; password: string; }): Observable<{ token: string }> {
		return this.http.post<{ token: string }>(`${this.apiUrl}/admin/login`, body);
	}

	adminGetUsers(): Observable<User[]> {
		return this.http.get<User[]>(`${this.apiUrl}/admin/users`, { headers: this.adminHeaders() });
	}

	adminUpdateRole(userId: number, role: string): Observable<any> {
		return this.http.put(`${this.apiUrl}/admin/users/${userId}/role`, {}, { params: { role }, headers: this.adminHeaders() });
	}

	adminDeleteUser(userId: number): Observable<any> {
		return this.http.delete(`${this.apiUrl}/admin/users/${userId}`, { headers: this.adminHeaders() });
	}

	adminGetLogs(): Observable<any[]> {
		return this.http.get<any[]>(`${this.apiUrl}/admin/logs`, { headers: this.adminHeaders() });
	}
} 

