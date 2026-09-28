import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { LoginPage } from './login-page';

describe('LoginPage', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  afterEach(() => localStorage.clear());

  it('sends the form credentials and saves the session after successful login', () => {
    const fixture = TestBed.createComponent(LoginPage);
    const http = TestBed.inject(HttpTestingController);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl');
    fixture.detectChanges();

    fixture.componentInstance.username = 'ana';
    fixture.componentInstance.password = 'secret';
    (fixture.nativeElement.querySelector('form') as HTMLFormElement).dispatchEvent(
      new Event('submit'),
    );

    const request = http.expectOne('/api/auth/login');
    expect(request.request.body).toEqual({ username: 'ana', password: 'secret' });
    request.flush({
      accessToken: 'signed-token',
      expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
    });

    expect(localStorage.getItem('orbita.accessToken')).toBe('signed-token');
    expect(navigate).toHaveBeenCalledWith('/');
    http.verify();
  });

  it('shows a credential error when login is rejected', () => {
    const fixture = TestBed.createComponent(LoginPage);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('form') as HTMLFormElement).dispatchEvent(
      new Event('submit'),
    );
    http.expectOne('/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Usuario o contraseña incorrectos.');
    http.verify();
  });
});
