import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { AuthService } from './auth';

// Functional interceptor for modern Angular
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  // Get the auth token from the service
  const token = authService.getToken();
  
  // Clone the request and add the authorization header if token exists
  let authReq = req;
  if (token && authService.isAuthenticated()) {
    authReq = req.clone({
      setHeaders: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      // Handle authentication errors
      if (error.status === 401 || error.status === 403) {
        // Token is invalid or expired
        authService.logout();
        router.navigate(['/login']);
      }
      
      // Handle other errors
      let errorMessage = 'An unknown error occurred';
      if (error.error && error.error.message) {
        errorMessage = error.error.message;
      } else if (error.message) {
        errorMessage = error.message;
      }
      
      console.error('HTTP Error:', errorMessage);
      return throwError(() => error);
    })
  );
};
