import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router, type UrlTree } from '@angular/router';
import { map, tap, type Observable } from 'rxjs';

interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
}

const ACCESS_TOKEN_KEY = 'orbita.accessToken';
const TOKEN_EXPIRY_KEY = 'orbita.tokenExpiresAtUtc';
const MAX_TIMEOUT_MS = 2_147_483_647;

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private expiryTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    this.getValidToken();
  }

  login(username: string, password: string): Observable<void> {
    return this.http.post<LoginResponse>('/api/auth/login', { username, password }).pipe(
      tap((response) => {
        localStorage.setItem(ACCESS_TOKEN_KEY, response.accessToken);
        localStorage.setItem(TOKEN_EXPIRY_KEY, response.expiresAtUtc);
        this.getValidToken();
      }),
      map(() => undefined),
    );
  }

  hasValidSession(): boolean {
    return this.getValidToken() !== null;
  }

  getValidToken(): string | null {
    const token = localStorage.getItem(ACCESS_TOKEN_KEY);
    const expiresAtUtc = localStorage.getItem(TOKEN_EXPIRY_KEY);
    const expiresAt = expiresAtUtc === null ? Number.NaN : Date.parse(expiresAtUtc);

    if (!token || !Number.isFinite(expiresAt) || expiresAt <= Date.now()) {
      this.clearSession();
      return null;
    }

    this.scheduleExpiry(expiresAt);
    return token;
  }

  redirectToLogin(): UrlTree {
    this.clearSession();
    return this.router.createUrlTree(['/login']);
  }

  clearSession(): void {
    if (this.expiryTimer !== null) {
      clearTimeout(this.expiryTimer);
      this.expiryTimer = null;
    }

    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(TOKEN_EXPIRY_KEY);
  }

  private scheduleExpiry(expiresAt: number): void {
    if (this.expiryTimer !== null) {
      clearTimeout(this.expiryTimer);
    }

    const delay = Math.min(expiresAt - Date.now(), MAX_TIMEOUT_MS);
    this.expiryTimer = setTimeout(() => {
      if (!this.getValidToken() && this.router.url !== '/login') {
        void this.router.navigateByUrl(this.redirectToLogin());
      }
    }, delay);
  }
}
