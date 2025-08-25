import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, Observable, throwError } from 'rxjs';
import { map, catchError } from 'rxjs/operators';

export interface User {
  id: number;
  username: string;
  email: string;
  role: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
}

export interface LoginResponse {
  message: string;
  token: string;
  userId: number;
  role: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = 'https://localhost:7083/api'; // Backend base URL
  private currentUserSubject: BehaviorSubject<User | null>;
  public currentUser: Observable<User | null>;

  constructor(private http: HttpClient) {
    // Initialize with user from storage if available
    this.currentUserSubject = new BehaviorSubject<User | null>(this.getUserFromStorage());
    this.currentUser = this.currentUserSubject.asObservable();
    console.log('AuthService initialized'); // Debug log
  }

  private isBrowser(): boolean {
    return typeof window !== 'undefined' && typeof localStorage !== 'undefined';
  }

  public get currentUserValue(): User | null {
    return this.currentUserSubject.value;
  }

  private getUserFromStorage(): User | null {
    if (!this.isBrowser()) {
      return null; // Return null during server-side rendering
    }
    
    const token = localStorage.getItem('token');
    if (token && !this.isTokenExpired(token)) {
      const userData = localStorage.getItem('user');
      return userData ? JSON.parse(userData) : null;
    }
    
    // Clear invalid/expired tokens
    if (token) {
      this.logout();
    }
    return null;
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/auth/login`, credentials)
      .pipe(
        map(response => {
          if (response.token && this.isBrowser()) {
            // Store token and user info
            localStorage.setItem('token', response.token);
            const user: User = {
              id: response.userId,
              username: '', // Will be filled from profile
              email: credentials.email,
              role: response.role
            };
            localStorage.setItem('user', JSON.stringify(user));
            this.currentUserSubject.next(user);
            
            // Fetch complete user profile
            this.getUserProfile(response.userId).subscribe(
              profile => {
                const updatedUser = { ...user, username: profile.username };
                if (this.isBrowser()) {
                  localStorage.setItem('user', JSON.stringify(updatedUser));
                }
                this.currentUserSubject.next(updatedUser);
              }
            );
          }
          return response;
        }),
        catchError(error => {
          console.error('Login error:', error);
          return throwError(() => error);
        })
      );
  }

  register(userData: RegisterRequest): Observable<any> {
    return this.http.post(`${this.apiUrl}/auth/register`, userData)
      .pipe(
        catchError(error => {
          console.error('Registration error:', error);
          return throwError(() => error);
        })
      );
  }

  logout(): void {
    if (this.isBrowser()) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
    }
    this.currentUserSubject.next(null);
  }

  getToken(): string | null {
    if (!this.isBrowser()) {
      return null;
    }
    return localStorage.getItem('token');
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    return token != null && !this.isTokenExpired(token);
  }

  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const currentTime = Math.floor(Date.now() / 1000);
      return payload.exp < currentTime;
    } catch (error) {
      return true;
    }
  }

  getUserProfile(userId: number): Observable<User> {
    return this.http.get<User>(`${this.apiUrl}/auth/${userId}`)
      .pipe(
        catchError(error => {
          console.error('Get profile error:', error);
          return throwError(() => error);
        })
      );
  }

  refreshToken(): Observable<any> {
    // Implement if your backend supports token refresh
    return this.http.post(`${this.apiUrl}/auth/refresh`, {});
  }

  hasRole(role: string): boolean {
    const user = this.currentUserValue;
    return user?.role === role;
  }

  isAdmin(): boolean {
    return this.hasRole('Admin');
  }

  isMember(): boolean {
    return this.hasRole('Member');
  }
}
