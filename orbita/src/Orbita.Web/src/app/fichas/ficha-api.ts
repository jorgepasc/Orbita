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
  snapshot: Snapshot | null;
}

@Injectable({ providedIn: 'root' })
export class FichaApi {
  private readonly http = inject(HttpClient);

  list(): Observable<Ficha[]> {
    return this.http.get<Ficha[]>('/api/fichas');
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
