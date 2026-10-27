import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';
import { CopiaSeguridadPage } from './copia-seguridad/copia-seguridad-page';
import { EstanteriaPage } from './estanteria/estanteria-page';
import { FichaDetallePage } from './fichas/ficha-detalle-page';
import { LibreriaLayout } from './libreria/libreria-layout';
import { LoginPage } from './login/login-page';

export const routes: Routes = [
  { path: 'login', component: LoginPage },
  {
    path: '',
    component: LibreriaLayout,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'pantalla' },
      { path: 'pantalla', component: EstanteriaPage, data: { estanteria: 'pantalla' } },
      { path: 'lecturas', component: EstanteriaPage, data: { estanteria: 'lecturas' } },
      { path: 'juegos', component: EstanteriaPage, data: { estanteria: 'juegos' } },
      { path: 'copia-seguridad', component: CopiaSeguridadPage },
      { path: 'fichas/:id', component: FichaDetallePage },
    ],
  },
  { path: '**', redirectTo: 'pantalla' },
];
