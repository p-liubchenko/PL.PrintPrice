import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, firstValueFrom, of, tap } from 'rxjs';

export interface AuthResponse {
  token: string;
  expiresAt: string;
  username: string;
  mustChangePassword: boolean;
  isAdmin: boolean;
}

export interface ChangePasswordRequest {
  newPassword: string;
  currentPassword?: string;
}

const TOKEN_KEY = 'pricer_token';
const USERNAME_KEY = 'pricer_username';
const EXPIRES_KEY = 'pricer_expires';

@Injectable({ providedIn: 'root' })
export class AuthService {
  /** Populated by APP_INITIALIZER before routing starts. */
  setupRequired = false;

  constructor(private http: HttpClient) {}

  // ── Initializer ───────────────────────────────────────────────────────

  checkSetupStatus(): Promise<void> {
    return firstValueFrom(
      this.http.get<{ required: boolean }>('/api/auth/setup-required').pipe(
        tap(res => { this.setupRequired = res.required; }),
        catchError(() => of({ required: false }))
      )
    ).then(() => {});
  }

  // ── Auth flows ────────────────────────────────────────────────────────

  setup(username: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/setup', { username, password }).pipe(
      tap(res => { this.storeToken(res); this.setupRequired = false; })
    );
  }

  login(username: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/login', { username, password }).pipe(
      tap(res => this.storeToken(res))
    );
  }

  changePassword(newPassword: string, currentPassword?: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/change-password', { newPassword, currentPassword }).pipe(
      tap(res => this.storeToken(res))
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USERNAME_KEY);
    localStorage.removeItem(EXPIRES_KEY);
  }

  // ── Token accessors ───────────────────────────────────────────────────

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  getUsername(): string | null {
    return localStorage.getItem(USERNAME_KEY);
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    const expires = localStorage.getItem(EXPIRES_KEY);
    if (!token || !expires) return false;
    return new Date(expires) > new Date();
  }

  mustChangePassword(): boolean {
    return this.getPayloadClaim('must_change_password') === 'true';
  }

  isAdmin(): boolean {
    const role = this.getPayloadClaim('role');
    if (!role) return false;
    return role === 'Administrator' ||
      (Array.isArray(role) && role.includes('Administrator'));
  }

  // ── Internals ─────────────────────────────────────────────────────────

  private storeToken(res: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, res.token);
    localStorage.setItem(USERNAME_KEY, res.username);
    localStorage.setItem(EXPIRES_KEY, res.expiresAt);
  }

  private getPayloadClaim(claim: string): any {
    const token = this.getToken();
    if (!token) return null;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload[claim] ?? null;
    } catch {
      return null;
    }
  }
}
