import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  FichaApi,
  TIPOS_FICHA,
  type Ficha,
  type FichaDraft,
  type Snapshot,
} from './fichas/ficha-api';
import { EnriquecimientoPanel } from './enriquecimiento/enriquecimiento-panel';

interface BackendHealth {
  status: string;
  databaseConfigured: boolean;
}

@Component({
  selector: 'app-dashboard',
  imports: [FormsModule, EnriquecimientoPanel],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  private readonly http = inject(HttpClient);
  private readonly fichasApi = inject(FichaApi);

  protected readonly backendStatus = signal<'loading' | 'ready' | 'offline'>('loading');
  protected readonly databaseConfigured = signal(false);
  protected readonly fichas = signal<Ficha[]>([]);
  protected readonly loadingFichas = signal(true);
  protected readonly savingFicha = signal(false);
  protected readonly showFichaForm = signal(false);
  protected readonly mostrarBusqueda = signal(false);
  protected readonly editingFichaId = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly tiposFicha = TIPOS_FICHA;
  protected draft: FichaDraft = this.emptyDraft();

  constructor() {
    this.http.get<BackendHealth>('/api/health').subscribe({
      next: (health) => {
        this.databaseConfigured.set(health.databaseConfigured);
        this.backendStatus.set(health.status === 'ok' ? 'ready' : 'offline');
      },
      error: () => this.backendStatus.set('offline'),
    });

    this.loadFichas();
  }

  protected startCreate(): void {
    this.draft = this.emptyDraft();
    this.editingFichaId.set(null);
    this.showFichaForm.set(true);
    this.mostrarBusqueda.set(true);
    this.errorMessage.set(null);
  }

  protected editFicha(ficha: Ficha): void {
    this.draft = {
      titulo: ficha.titulo,
      tipo: ficha.tipo,
      fecha: ficha.fecha,
      terminado: ficha.terminado,
      valoracion: ficha.valoracion,
      comentarios: ficha.comentarios,
      compania: ficha.compania,
      snapshot: ficha.snapshot,
    };
    this.editingFichaId.set(ficha.id);
    this.showFichaForm.set(true);
    this.mostrarBusqueda.set(false);
    this.errorMessage.set(null);
  }

  protected cancelFichaForm(): void {
    this.showFichaForm.set(false);
    this.editingFichaId.set(null);
    this.mostrarBusqueda.set(false);
    this.errorMessage.set(null);
  }

  protected elegirEnriquecimiento(elegido: { snapshot: Snapshot; titulo: string }): void {
    this.draft.snapshot = elegido.snapshot;
    this.draft.titulo = elegido.titulo;
    this.mostrarBusqueda.set(false);
  }

  protected anadirManualmente(): void {
    this.mostrarBusqueda.set(false);
  }

  protected quitarSnapshot(): void {
    this.draft.snapshot = null;
    if (this.editingFichaId() === null) {
      this.mostrarBusqueda.set(true);
    }
  }

  protected alCambiarTipo(): void {
    if (this.editingFichaId() === null && this.draft.snapshot !== null) {
      this.draft.snapshot = null;
      this.mostrarBusqueda.set(true);
    }
  }

  protected saveFicha(): void {
    const id = this.editingFichaId();
    const ficha = this.normalizeDraft(this.draft);
    this.savingFicha.set(true);
    this.errorMessage.set(null);

    const request = id === null ? this.fichasApi.create(ficha) : this.fichasApi.update(id, ficha);
    request.subscribe({
      next: (savedFicha) => {
        this.fichas.update((current) => {
          const updated =
            id === null
              ? [...current, savedFicha]
              : current.map((ficha) => (ficha.id === savedFicha.id ? savedFicha : ficha));
          return this.sortFichas(updated);
        });
        this.savingFicha.set(false);
        this.cancelFichaForm();
      },
      error: () => {
        this.savingFicha.set(false);
        this.errorMessage.set(
          'No se pudo guardar la Ficha. Revisa los datos e inténtalo de nuevo.',
        );
      },
    });
  }

  protected deleteFicha(ficha: Ficha): void {
    const confirmed = window.confirm(
      `¿Borrar la Ficha «${ficha.titulo}»? Esta acción no se puede deshacer.`,
    );
    if (!confirmed) {
      return;
    }

    this.errorMessage.set(null);
    this.fichasApi.delete(ficha.id).subscribe({
      next: () =>
        this.fichas.update((current) => current.filter((savedFicha) => savedFicha.id !== ficha.id)),
      error: () => this.errorMessage.set('No se pudo borrar la Ficha. Inténtalo de nuevo.'),
    });
  }

  private loadFichas(): void {
    this.fichasApi.list().subscribe({
      next: (fichas) => {
        this.fichas.set(this.sortFichas(fichas));
        this.loadingFichas.set(false);
      },
      error: () => {
        this.errorMessage.set(
          'No se pudieron cargar las Fichas. Comprueba la conexión e inténtalo de nuevo.',
        );
        this.loadingFichas.set(false);
      },
    });
  }

  private emptyDraft(): FichaDraft {
    return {
      titulo: '',
      tipo: 'Película',
      fecha: null,
      terminado: false,
      valoracion: null,
      comentarios: null,
      compania: null,
      snapshot: null,
    };
  }

  private normalizeDraft(draft: FichaDraft): FichaDraft {
    return {
      ...draft,
      titulo: draft.titulo.trim(),
      fecha: draft.fecha || null,
      comentarios: draft.comentarios?.trim() || null,
      compania: draft.compania?.trim() || null,
    };
  }

  private sortFichas(fichas: Ficha[]): Ficha[] {
    return [...fichas].sort((first, second) => first.titulo.localeCompare(second.titulo));
  }
}
