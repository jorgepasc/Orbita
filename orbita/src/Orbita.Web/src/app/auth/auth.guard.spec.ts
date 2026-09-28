import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import {
  provideRouter,
  Router,
  type ActivatedRouteSnapshot,
  type RouterStateSnapshot,
} from '@angular/router';
import { isObservable } from 'rxjs';
import { AuthService } from './auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  const auth = {
    hasValidSession: vi.fn(() => false),
    clearSession: vi.fn(),
    redirectToLogin: vi.fn(() => TestBed.inject(Router).parseUrl('/login')),
  };

  beforeEach(() => {
    auth.hasValidSession.mockReset();
    auth.hasValidSession.mockReturnValue(false);
    auth.clearSession.mockReset();
    auth.redirectToLogin.mockClear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
      ],
    });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('redirects an unauthenticated visitor to login', () => {
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );

    expect(result).toEqual(TestBed.inject(Router).parseUrl('/login'));
    expect(TestBed.inject(HttpTestingController).match('/api/auth/me')).toHaveLength(0);
  });

  it('allows a visitor only after the API confirms the session', () => {
    auth.hasValidSession.mockReturnValue(true);

    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );
    let allowed: unknown;
    if (isObservable(result)) {
      result.subscribe((value) => (allowed = value));
    }

    TestBed.inject(HttpTestingController).expectOne('/api/auth/me').flush({ username: 'ana' });

    expect(allowed).toBe(true);
  });

  it('redirects and clears the session when the API rejects the token', () => {
    auth.hasValidSession.mockReturnValue(true);

    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );
    let redirect: unknown;
    if (isObservable(result)) {
      result.subscribe((value) => (redirect = value));
    }

    TestBed.inject(HttpTestingController)
      .expectOne('/api/auth/me')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(auth.redirectToLogin).toHaveBeenCalledOnce();
    expect(redirect).toEqual(TestBed.inject(Router).parseUrl('/login'));
  });
});
