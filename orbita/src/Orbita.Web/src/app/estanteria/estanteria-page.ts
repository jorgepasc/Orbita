import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FichaApi, notaExternaConSufijo, type Ficha, type TipoFicha } from '../fichas/ficha-api';
import { FichaFormPanel } from '../fichas/ficha-form-panel';
import { agruparPorMes, normalizarTexto, ordenarPorValoracion } from './agrupacion-mensual';
import { EstanteriaIlustracion } from './estanteria-ilustracion';
import { getEstanteria } from './estanterias';

type Orden = 'fecha' | 'valoracion';

@Component({
  selector: 'app-estanteria-page',
  imports: [FormsModule, NgTemplateOutlet, RouterLink, FichaFormPanel, EstanteriaIlustracion],
  templateUrl: './estanteria-page.html',
  styleUrl: './estanteria-page.scss',
})
export class EstanteriaPage {
  private readonly api = inject(FichaApi);
  private readonly route = inject(ActivatedRoute);

  protected readonly estanteria = getEstanteria(this.route.snapshot.data['estanteria']);
  protected readonly agrupaDosTipos = this.estanteria.tipos.length > 1;
  protected readonly fichas = signal<Ficha[]>([]);
  protected readonly cargando = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly filtroTipo = signal<TipoFicha | null>(null);
  protected readonly busqueda = signal('');
  protected readonly orden = signal<Orden>('fecha');
  protected readonly mostrandoFormulario = signal(false);

  private readonly visibles = computed(() => {
    const tipos = this.estanteria.tipos;
    const filtro = this.filtroTipo();
    const consulta = normalizarTexto(this.busqueda());

    return this.fichas().filter((ficha) => {
      if (!tipos.includes(ficha.tipo)) {
        return false;
      }
      if (filtro !== null && ficha.tipo !== filtro) {
        return false;
      }
      return !consulta || normalizarTexto(ficha.titulo).includes(consulta);
    });
  });

  protected readonly grupos = computed(() => agruparPorMes(this.visibles()));
  protected readonly ranking = computed(() => ordenarPorValoracion(this.visibles()));

  constructor() {
    this.api.list().subscribe({
      next: (fichas) => {
        this.fichas.set(fichas);
        this.cargando.set(false);
      },
      error: () => {
        this.errorMessage.set(
          'No se pudieron cargar tus Fichas. Comprueba la conexión e inténtalo de nuevo.',
        );
        this.cargando.set(false);
      },
    });
  }

  protected abrirCreacion(): void {
    this.mostrandoFormulario.set(true);
  }

  protected cerrarFormulario(): void {
    this.mostrandoFormulario.set(false);
  }

  protected alGuardar(ficha: Ficha): void {
    this.fichas.update((actuales) => {
      const existe = actuales.some((existente) => existente.id === ficha.id);
      return existe
        ? actuales.map((existente) => (existente.id === ficha.id ? ficha : existente))
        : [...actuales, ficha];
    });
    this.cerrarFormulario();
  }

  protected primerGenero(ficha: Ficha): string | null {
    return ficha.snapshot?.generos[0] ?? null;
  }

  protected notaExterna(ficha: Ficha): string | null {
    return notaExternaConSufijo(ficha.snapshot);
  }
}
