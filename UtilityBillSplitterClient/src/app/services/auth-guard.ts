import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth';

export const authGuard: CanActivateFn = () => {
	const authService = inject(AuthService);
	const router = inject(Router);
	if (authService.isAuthenticated()) return true;
	router.navigate(['/login']);
	return false;
};

export const guestGuard: CanActivateFn = () => {
	const authService = inject(AuthService);
	const router = inject(Router);
	if (!authService.isAuthenticated()) return true;
	router.navigate(['/dashboard']);
	return false;
};

export const adminGuard: CanActivateFn = () => {
	const router = inject(Router);
	const token = typeof localStorage !== 'undefined' ? localStorage.getItem('adminToken') : null;
	if (token) return true;
	router.navigate(['/admin/login']);
	return false;
};
