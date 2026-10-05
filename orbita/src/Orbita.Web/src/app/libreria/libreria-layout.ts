import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ESTANTERIAS } from '../estanteria/estanterias';

@Component({
  selector: 'app-libreria-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './libreria-layout.html',
  styleUrl: './libreria-layout.scss',
})
export class LibreriaLayout {
  protected readonly estanterias = ESTANTERIAS;
}
