import type { TipoFicha } from '../fichas/ficha-api';

export type EstanteriaId = 'pantalla' | 'lecturas' | 'juegos';

export interface Estanteria {
  id: EstanteriaId;
  nombre: string;
  tipos: readonly TipoFicha[];
}

export const ESTANTERIAS: readonly Estanteria[] = [
  { id: 'pantalla', nombre: 'Pantalla', tipos: ['Película', 'Serie'] },
  { id: 'lecturas', nombre: 'Lecturas', tipos: ['Libro', 'Cómic'] },
  { id: 'juegos', nombre: 'Juegos', tipos: ['Juego'] },
];

export function getEstanteria(id: string | null | undefined): Estanteria {
  return ESTANTERIAS.find((estanteria) => estanteria.id === id) ?? ESTANTERIAS[0];
}

export function estanteriaDeTipo(tipo: TipoFicha): Estanteria {
  return ESTANTERIAS.find((estanteria) => estanteria.tipos.includes(tipo)) ?? ESTANTERIAS[0];
}
