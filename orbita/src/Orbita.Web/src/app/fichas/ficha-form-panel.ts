import { Component, computed, inject, input, type OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EnriquecimientoPanel } from '../enriquecimiento/enriquecimiento-panel';
import { FichaApi, type Ficha, type FichaDraft, type Snapshot, type TipoFicha } from './ficha-api';

function crearDraftVacio(): FichaDraft {
  return {
    titulo: '',
    tipo: 'Película',
    fecha: null,
    terminado: true,
    valoracion: null,
    comentarios: null,
    compania: null,
    plataforma: null,
    idioma: null,
    autor: null,
    snapshot: null,
  };
}

@Component({
  selector: 'app-ficha-form-panel',
  imports: [FormsModule, EnriquecimientoPanel],
  templateUrl: './ficha-form-panel.html',
  styleUrl: './ficha-form-panel.scss',
})
export class FichaFormPanel implements OnInit {
  private readonly api = inject(FichaApi);

  readonly tiposPermitidos = input.required<readonly TipoFicha[]>();
  readonly ficha = input<Ficha | null>(null);
  readonly guardada = output<Ficha>();
  readonly cancelada = output<void>();

  protected readonly esEdicion = computed(() => this.ficha() !== null);
  protected readonly guardando = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly mostrarBusqueda = signal(false);
  protected draft: FichaDraft = crearDraftVacio();
  private readonly versionFormulario = signal(0);

  protected readonly formularioValido = computed(() => {
    this.versionFormulario();
    return this.draft.titulo.trim().length > 0 && this.draft.fecha !== null;
  });

  ngOnInit(): void {
    const ficha = this.ficha();
    if (ficha !== null) {
      Object.assign(this.draft, ficha, { snapshot: ficha.snapshot });
    } else {
      this.draft.tipo = this.tiposPermitidos()[0];
    }
    this.mostrarBusqueda.set(!this.esEdicion());
    this.versionFormulario.set(1);
  }

  protected actualizarTitulo(titulo: string): void {
    this.draft.titulo = titulo;
    this.versionFormulario.update((valor) => valor + 1);
  }

  protected actualizarFecha(fecha: string): void {
    this.draft.fecha = fecha === '' ? null : fecha;
    this.versionFormulario.update((valor) => valor + 1);
  }

  protected actualizarValoracion(valor: string): void {
    this.draft.valoracion = valor === '' ? null : Number(valor);
  }

  protected actualizarCompania(compania: string): void {
    this.draft.compania = compania;
  }

  protected actualizarPlataforma(plataforma: string): void {
    this.draft.plataforma = plataforma;
  }

  protected actualizarIdioma(idioma: string): void {
    this.draft.idioma = idioma;
  }

  protected actualizarAutor(autor: string): void {
    this.draft.autor = autor;
  }

  protected actualizarComentarios(comentarios: string): void {
    this.draft.comentarios = comentarios;
  }

  protected actualizarTerminado(terminado: boolean): void {
    this.draft.terminado = terminado;
  }

  protected elegirEnriquecimiento(elegido: {
    snapshot: Snapshot;
    titulo: string;
    tipo: TipoFicha;
  }): void {
    this.draft.tipo = elegido.tipo;
    this.draft.snapshot = elegido.snapshot;
    this.draft.titulo = elegido.titulo;
    if (elegido.tipo === 'Libro' && !this.draft.autor?.trim()) {
      this.draft.autor = elegido.snapshot.autoria;
    }
    this.mostrarBusqueda.set(false);
  }

  protected anadirManualmente(): void {
    this.mostrarBusqueda.set(false);
  }

  protected quitarSnapshot(): void {
    this.draft.snapshot = null;
    if (!this.esEdicion()) {
      this.mostrarBusqueda.set(true);
    }
  }

  protected alCambiarTipo(tipo: TipoFicha): void {
    this.draft.tipo = tipo;
    if (tipo !== 'Juego') {
      this.draft.plataforma = null;
    }
    if (tipo !== 'Libro' && tipo !== 'Cómic') {
      this.draft.idioma = null;
    }
    if (tipo !== 'Libro') {
      this.draft.autor = null;
    } else {
      this.draft.compania = null;
    }
    if (!this.esEdicion() && this.draft.snapshot !== null) {
      this.draft.snapshot = null;
      this.mostrarBusqueda.set(true);
    }
  }

  protected guardar(): void {
    if (this.guardando()) {
      return;
    }

    const draft = this.normalizarDraft(this.draft);
    const editando = this.ficha();
    this.guardando.set(true);
    this.errorMessage.set(null);

    const peticion =
      editando === null ? this.api.create(draft) : this.api.update(editando.id, draft);
    peticion.subscribe({
      next: (ficha) => {
        this.guardando.set(false);
        this.guardada.emit(ficha);
      },
      error: () => {
        this.guardando.set(false);
        this.errorMessage.set(
          'No se pudo guardar la Ficha. Revisa los datos e inténtalo de nuevo.',
        );
      },
    });
  }

  protected cancelar(): void {
    this.cancelada.emit();
  }

  private normalizarDraft(draft: FichaDraft): FichaDraft {
    const esLibro = draft.tipo === 'Libro';
    return {
      ...draft,
      titulo: draft.titulo.trim(),
      fecha: draft.fecha || null,
      comentarios: draft.comentarios?.trim() || null,
      compania: esLibro ? null : draft.compania?.trim() || null,
      autor: esLibro ? draft.autor?.trim() || null : null,
      plataforma: draft.plataforma?.trim() || null,
      idioma: draft.idioma?.trim() || null,
    };
  }
}
