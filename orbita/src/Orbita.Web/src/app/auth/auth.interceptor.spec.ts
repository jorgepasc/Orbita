import {
  HttpClient,
  HttpErrorResponse,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpTestingController;
  let clearSession: () => void;
  let token: string | null;

  beforeEach(() => {
    token = 'signed-token';
    clearSession = vi.fn(() => (token = null));
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        {
          provide: AuthService,
          useValue: {
            getValidToken: () => token,
            clearSession,
            redirectToLogin: () => {
              clearSession();
              return TestBed.inject(Router).parseUrl('/login');
            },
          },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('adds a bearer token to API requests', () => {
    TestBed.inject(HttpClient).get('/api/auth/me').subscribe();

    const request = http.expectOne('/api/auth/me');
    expect(request.request.headers.get('Authorization')).toBe('Bearer signed-token');
    request.flush({ username: 'ana' });

    TestBed.inject(HttpClient).get('https://example.org/catalog').subscribe();
    const externalRequest = http.expectOne('https://example.org/catalog');
    expect(externalRequest.request.headers.has('Authorization')).toBe(false);
    externalRequest.flush({});
  });

  it('clears the session and redirects to login when the API rejects a token', () => {
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigateByUrl');
    TestBed.inject(HttpClient)
      .get('/api/fichas')
      .subscribe({
        error: (error: HttpErrorResponse) => expect(error.status).toBe(401),
      });

    http.expectOne('/api/fichas').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(clearSession).toHaveBeenCalledOnce();
    expect(navigate).toHaveBeenCalledWith(router.parseUrl('/login'));
  });
});
