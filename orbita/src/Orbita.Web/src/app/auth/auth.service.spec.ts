import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let auth: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    auth.clearSession();
    http.verify();
    localStorage.clear();
    vi.useRealTimers();
  });

  it('stores a token returned by the login endpoint', () => {
    auth.login('ana', 'secret').subscribe();

    const request = http.expectOne('/api/auth/login');
    request.flush({
      accessToken: 'signed-token',
      expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
    });

    expect(auth.hasValidSession()).toBe(true);
    expect(auth.getValidToken()).toBe('signed-token');
  });

  it('clears a token after its expiration time', () => {
    auth.login('ana', 'secret').subscribe();

    const request = http.expectOne('/api/auth/login');
    request.flush({
      accessToken: 'expired-token',
      expiresAtUtc: new Date(Date.now() - 1_000).toISOString(),
    });

    expect(auth.hasValidSession()).toBe(false);
    expect(auth.getValidToken()).toBeNull();
  });

  it('redirects to login when a saved token expires while the app is open', async () => {
    vi.useFakeTimers();
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    auth.login('ana', 'secret').subscribe();

    const request = http.expectOne('/api/auth/login');
    request.flush({
      accessToken: 'short-lived-token',
      expiresAtUtc: new Date(Date.now() + 1_000).toISOString(),
    });

    await vi.advanceTimersByTimeAsync(1_000);

    expect(auth.hasValidSession()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(router.parseUrl('/login'));
  });
});
