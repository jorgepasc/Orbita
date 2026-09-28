import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';
import { Dashboard } from './dashboard';
import { LoginPage } from './login/login-page';

export const routes: Routes = [
  { path: 'login', component: LoginPage },
  { path: '', component: Dashboard, canActivate: [authGuard] },
  { path: '**', redirectTo: '' },
];
