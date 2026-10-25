import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { catchError, forkJoin, map, of } from 'rxjs';
import type { Snapshot, TipoFicha } from '../fichas/ficha-api';
import { EnriquecimientoApi, type ResultadoDeTipo } from './enriquecimiento-api';

// Pantalla agrupa Película y Serie, ambas de TMDB: una sola búsqueda cubre las dos.
const TIPOS_PANTALLA: readonly TipoFicha[] = ['Película', 'Serie'];

@Component({
  selector: 'app-enriquecimiento-panel',
  imports: [],
  templateUrl: './enriquecimiento-panel.html',
  styleUrl: './enriquecimiento-panel.scss',
})
export class EnriquecimientoPanel {
  private readonly api = inject(EnriquecimientoApi);

  readonly tipo = input.required<TipoFicha>();
  readonly elegido = output<{ snapshot: Snapshot; titulo: string; tipo: TipoFicha }>();
  readonly manual = output<void>();

  protected consulta = '';
  protected readonly buscando = signal(false);
  protected readonly tiposBusqueda = computed<readonly TipoFicha[]>(() =>
    TIPOS_PANTALLA.includes(this.tipo()) ? TIPOS_PANTALLA : [this.tipo()],
  );
  protected readonly resultados = signal<ResultadoDeTipo[]>([]);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly busquedaRealizada = signal(false);
  private versionTipo = 0;
  private numeroBusqueda = 0;
  private numeroSnapshot = 0;

  constructor() {
    effect(() => {
      this.tipo();
      this.versionTipo += 1;
      this.numeroBusqueda += 1;
      this.numeroSnapshot += 1;
      this.resultados.set([]);
      this.busquedaRealizada.set(false);
      this.buscando.set(false);
      this.errorMessage.set(null);
    });
  }

  protected actualizarConsulta(event: Event): void {
    this.consulta = (event.target as HTMLInputElement).value;
  }

  protected buscar(event: Event): void {
    event.preventDefault();
    const texto = this.consulta.trim();
    if (!texto) {
      return;
    }

    const tipo = this.tipo();
    const versionTipo = this.versionTipo;
    const numeroBusqueda = ++this.numeroBusqueda;
    this.buscando.set(true);
    this.errorMessage.set(null);
    const tipos = this.tiposBusqueda();
    let fallos = 0;
    forkJoin(
      tipos.map((tipoBusqueda) =>
        this.api.resultados(texto, tipoBusqueda).pipe(
          map((resultados) =>
            resultados.map((resultado) => ({ ...resultado, tipo: tipoBusqueda })),
          ),
          catchError(() => {
            fallos += 1;
            return of<ResultadoDeTipo[]>([]);
          }),
        ),
      ),
    ).subscribe({
      next: (porTipo) => {
        if (fallos === tipos.length) {
          this.buscando.set(false);
          this.errorMessage.set(
            'No se pudo consultar la API externa. Prueba de nuevo o añade la Ficha manualmente.',
          );
          return;
        }

        if (!this.busquedaSigueVigente(tipo, versionTipo, numeroBusqueda)) {
          return;
        }

        this.resultados.set(porTipo.flat());
        this.busquedaRealizada.set(true);
        this.buscando.set(false);
      },
      error: () => {
        if (!this.busquedaSigueVigente(tipo, versionTipo, numeroBusqueda)) {
          return;
        }

        this.buscando.set(false);
        this.errorMessage.set(
          'No se pudo consultar la API externa. Prueba de nuevo o añade la Ficha manualmente.',
        );
      },
    });
  }

  protected elegir(resultado: ResultadoDeTipo): void {
    const tipo = this.tipo();
    const versionTipo = this.versionTipo;
    const numeroSnapshot = ++this.numeroSnapshot;
    this.buscando.set(true);
    this.api.snapshot(resultado.tipo, resultado.idExterno).subscribe({
      next: (snapshot) => {
        if (!this.tipoSigueVigente(tipo, versionTipo) || numeroSnapshot !== this.numeroSnapshot) {
          return;
        }

        this.buscando.set(false);
        this.elegido.emit({ snapshot, titulo: resultado.titulo, tipo: resultado.tipo });
      },
      error: () => {
        if (!this.tipoSigueVigente(tipo, versionTipo) || numeroSnapshot !== this.numeroSnapshot) {
          return;
        }

        this.buscando.set(false);
        this.errorMessage.set(
          'No se pudo congelar el Snapshot. Prueba de nuevo o añade la Ficha manualmente.',
        );
      },
    });
  }

  private busquedaSigueVigente(
    tipo: TipoFicha,
    versionTipo: number,
    numeroBusqueda: number,
  ): boolean {
    return this.tipoSigueVigente(tipo, versionTipo) && numeroBusqueda === this.numeroBusqueda;
  }

  private tipoSigueVigente(tipo: TipoFicha, versionTipo: number): boolean {
    return tipo === this.tipo() && versionTipo === this.versionTipo;
  }
}
