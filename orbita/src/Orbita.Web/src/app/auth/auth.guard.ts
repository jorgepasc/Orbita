import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { type CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const http = inject(HttpClient);

  if (!auth.hasValidSession()) {
    return router.createUrlTree(['/login']);
  }

  return http.get('/api/auth/me').pipe(
    map(() => true),
    catchError(() => of(auth.redirectToLogin())),
  );
};
