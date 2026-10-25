import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';
import type { Snapshot, TipoFicha } from '../fichas/ficha-api';

export interface Resultado {
  idExterno: string;
  titulo: string;
  anio: number | null;
  portada: string | null;
  notaExterna: number | null;
}

export interface ResultadoDeTipo extends Resultado {
  tipo: TipoFicha;
}

@Injectable({ providedIn: 'root' })
export class EnriquecimientoApi {
  private readonly http = inject(HttpClient);

  resultados(q: string, tipo: TipoFicha): Observable<Resultado[]> {
    return this.http.get<Resultado[]>('/api/enriquecimiento/resultados', { params: { q, tipo } });
  }

  snapshot(tipo: TipoFicha, idExterno: string): Observable<Snapshot> {
    return this.http.get<Snapshot>('/api/enriquecimiento/snapshot', {
      params: { tipo, idExterno },
    });
  }
}
