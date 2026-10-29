import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { estanteriaDeTipo } from '../estanteria/estanterias';
import {
  FichaApi,
  notaExternaConSufijo,
  tiposCompatiblesConSnapshot,
  type Ficha,
  type TipoFicha,
} from './ficha-api';
import { FichaFormPanel } from './ficha-form-panel';

const ETIQUETA_AUTORIA: Record<TipoFicha, string> = {
  Película: 'Director',
  Serie: 'Creador',
  Libro: 'Autor',
  Juego: 'Estudio',
  Cómic: 'Autor',
};

@Component({
  selector: 'app-ficha-detalle-page',
  imports: [FichaFormPanel],
  templateUrl: './ficha-detalle-page.html',
  styleUrl: './ficha-detalle-page.scss',
})
export class FichaDetallePage {
  private readonly api = inject(FichaApi);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly ficha = signal<Ficha | null>(null);
  protected readonly cargando = signal(true);
  protected readonly editando = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly estanteriaNombre = computed(() => {
    const ficha = this.ficha();
    return ficha ? estanteriaDeTipo(ficha.tipo).nombre : 'la librería';
  });

  protected readonly tiposEditables = computed(() =>
    tiposCompatiblesConSnapshot(this.ficha()?.snapshot ?? null),
  );

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id === null) {
      this.cargando.set(false);
      return;
    }

    this.api.get(id).subscribe({
      next: (ficha) => {
        this.ficha.set(ficha);
        this.cargando.set(false);
      },
      error: () => {
        this.cargando.set(false);
        this.errorMessage.set('No se pudo cargar la Ficha.');
      },
    });
  }

  protected etiquetaAutoria(tipo: TipoFicha): string {
    return ETIQUETA_AUTORIA[tipo];
  }

  protected paises(codigos: readonly string[]): string {
    const nombres = new Intl.DisplayNames(['es'], { type: 'region' });
    return codigos.map((codigo) => nombres.of(codigo) ?? codigo).join(', ');
  }

  protected mostrarNotaExterna(tipo: TipoFicha): boolean {
    return tipo !== 'Libro' && tipo !== 'Cómic';
  }

  protected notaExterna(ficha: Ficha): string | null {
    return notaExternaConSufijo(ficha.snapshot);
  }

  protected volver(): void {
    const ficha = this.ficha();
    const estanteria = ficha ? estanteriaDeTipo(ficha.tipo).id : 'pantalla';
    void this.router.navigateByUrl(`/${estanteria}`);
  }

  protected abrirEdicion(): void {
    this.editando.set(true);
  }

  protected cerrarEdicion(): void {
    this.editando.set(false);
  }

  protected alGuardar(ficha: Ficha): void {
    this.ficha.set(ficha);
    this.editando.set(false);
  }

  protected borrar(): void {
    const ficha = this.ficha();
    if (ficha === null) {
      return;
    }

    const confirmado = window.confirm(
      `¿Borrar la Ficha «${ficha.titulo}»? Esta acción no se puede deshacer.`,
    );
    if (!confirmado) {
      return;
    }

    this.api.delete(ficha.id).subscribe({
      next: () => this.volver(),
      error: () => this.errorMessage.set('No se pudo borrar la Ficha. Inténtalo de nuevo.'),
    });
  }
}
