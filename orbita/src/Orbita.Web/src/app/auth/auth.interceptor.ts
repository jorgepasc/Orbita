import { HttpErrorResponse, type HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const isApiRequest = request.url.startsWith('/api/');
  const token = isApiRequest ? auth.getValidToken() : null;
  const authenticatedRequest = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(authenticatedRequest).pipe(
    catchError((error: unknown) => {
      if (isApiRequest && error instanceof HttpErrorResponse && error.status === 401) {
        if (request.url.endsWith('/api/auth/login') || request.url.endsWith('/api/auth/me')) {
          auth.clearSession();
        } else {
          void router.navigateByUrl(auth.redirectToLogin());
        }
      }

      return throwError(() => error);
    }),
  );
};
