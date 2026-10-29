import { Component, input } from '@angular/core';
import type { EstanteriaId } from './estanterias';

// Ilustraciones de línea de cada Estantería: trazo en currentColor y acentos en terracota.
@Component({
  selector: 'app-estanteria-ilustracion',
  templateUrl: './estanteria-ilustracion.html',
  styleUrl: './estanteria-ilustracion.scss',
})
export class EstanteriaIlustracion {
  readonly estanteria = input.required<EstanteriaId>();
}
