import { Component, effect, inject, input, output, signal } from '@angular/core';
import type { Snapshot, TipoFicha } from '../fichas/ficha-api';
import { EnriquecimientoApi, type Resultado } from './enriquecimiento-api';

@Component({
  selector: 'app-enriquecimiento-panel',
  imports: [],
  templateUrl: './enriquecimiento-panel.html',
  styleUrl: './enriquecimiento-panel.scss',
})
export class EnriquecimientoPanel {
  private readonly api = inject(EnriquecimientoApi);

  readonly tipo = input.required<TipoFicha>();
  readonly elegido = output<{ snapshot: Snapshot; titulo: string }>();
  readonly manual = output<void>();

  protected consulta = '';
  protected readonly buscando = signal(false);
  protected readonly resultados = signal<Resultado[]>([]);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly busco = signal(false);

  constructor() {
    effect(() => {
      this.tipo();
      this.resultados.set([]);
      this.busco.set(false);
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

    this.buscando.set(true);
    this.errorMessage.set(null);
    this.api.resultados(texto, this.tipo()).subscribe({
      next: (resultados) => {
        this.resultados.set(resultados);
        this.busco.set(true);
        this.buscando.set(false);
      },
      error: () => {
        this.buscando.set(false);
        this.errorMessage.set(
          'No se pudo consultar la API externa. Prueba de nuevo o añade la Ficha manualmente.',
        );
      },
    });
  }

  protected elegir(resultado: Resultado): void {
    this.buscando.set(true);
    this.api.snapshot(this.tipo(), resultado.idExterno).subscribe({
      next: (snapshot) => {
        this.buscando.set(false);
        this.elegido.emit({ snapshot, titulo: resultado.titulo });
      },
      error: () => {
        this.buscando.set(false);
        this.errorMessage.set(
          'No se pudo congelar el Snapshot. Prueba de nuevo o añade la Ficha manualmente.',
        );
      },
    });
  }
}
