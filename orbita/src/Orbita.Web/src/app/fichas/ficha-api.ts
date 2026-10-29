import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';

export const TIPOS_FICHA = ['Película', 'Serie', 'Libro', 'Juego', 'Cómic'] as const;

export type TipoFicha = (typeof TIPOS_FICHA)[number];

export type FuenteSnapshot = 'tmdb' | 'rawg' | 'openlibrary';

export interface Snapshot {
  fuente: FuenteSnapshot;
  idExterno: string;
  portada: string | null;
  anio: number | null;
  autoria: string | null;
  notaExterna: number | null;
  generos: string[];
  detalles?: DetallesSnapshot | null;
}

// Detalles propios de cada Tipo, discriminados por `tipo` (ADR 0004).
export interface DetallesPelicula {
  tipo: 'pelicula';
  actores: string[];
  paises: string[];
  duracionMinutos: number | null;
}

export type DetallesSnapshot = DetallesPelicula;

const TIPOS_POR_FUENTE: Record<FuenteSnapshot, readonly TipoFicha[]> = {
  tmdb: ['Película', 'Serie'],
  rawg: ['Juego'],
  openlibrary: ['Libro', 'Cómic'],
};

export function tiposCompatiblesConSnapshot(snapshot: Snapshot | null): readonly TipoFicha[] {
  return snapshot === null ? TIPOS_FICHA : TIPOS_POR_FUENTE[snapshot.fuente];
}

export function notaExternaConSufijo(snapshot: Snapshot | null): string | null {
  if (snapshot === null || snapshot.notaExterna === null) {
    return null;
  }
  return snapshot.fuente === 'rawg' ? `${snapshot.notaExterna} MC` : `${snapshot.notaExterna}`;
}

export interface Ficha {
  id: string;
  titulo: string;
  tipo: TipoFicha;
  fecha: string | null;
  terminado: boolean;
  valoracion: number | null;
  comentarios: string | null;
  compania: string | null;
  plataforma: string | null;
  idioma: string | null;
  autor: string | null;
  snapshot: Snapshot | null;
}

export interface FichaDraft {
  titulo: string;
  tipo: TipoFicha;
  fecha: string | null;
  terminado: boolean;
  valoracion: number | null;
  comentarios: string | null;
  compania: string | null;
  plataforma: string | null;
  idioma: string | null;
  autor: string | null;
  snapshot: Snapshot | null;
}

@Injectable({ providedIn: 'root' })
export class FichaApi {
  private readonly http = inject(HttpClient);

  list(): Observable<Ficha[]> {
    return this.http.get<Ficha[]>('/api/fichas');
  }

  get(id: string): Observable<Ficha> {
    return this.http.get<Ficha>(`/api/fichas/${id}`);
  }

  create(ficha: FichaDraft): Observable<Ficha> {
    return this.http.post<Ficha>('/api/fichas', ficha);
  }

  update(id: string, ficha: FichaDraft): Observable<Ficha> {
    return this.http.put<Ficha>(`/api/fichas/${id}`, ficha);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/fichas/${id}`);
  }
}
