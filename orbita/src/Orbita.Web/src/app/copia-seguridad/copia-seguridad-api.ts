import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';

export interface ResumenImportacion {
  creadas: number;
  actualizadas: number;
}

@Injectable({ providedIn: 'root' })
export class CopiaSeguridadApi {
  private readonly http = inject(HttpClient);

  exportar(): Observable<Blob> {
    return this.http.get('/api/copia-seguridad', { responseType: 'blob' });
  }

  importar(documento: unknown): Observable<ResumenImportacion> {
    return this.http.post<ResumenImportacion>('/api/copia-seguridad', documento);
  }
}
