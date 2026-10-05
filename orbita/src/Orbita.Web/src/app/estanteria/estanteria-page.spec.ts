import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import type { Ficha, Snapshot } from '../fichas/ficha-api';
import { EstanteriaPage } from './estanteria-page';

const snapshot: Snapshot = {
  fuente: 'tmdb',
  idExterno: '438631',
  portada: 'https://image.tmdb.org/t/p/w500/dune.jpg',
  anio: 2021,
  autoria: 'Denis Villeneuve',
  notaExterna: 7.8,
  generos: ['Ciencia ficción', 'Aventura'],
};

function ficha(parcial: Partial<Ficha> & { id: string; titulo: string }): Ficha {
  return {
    tipo: 'Película',
    fecha: '2026-03-14',
    terminado: true,
    valoracion: null,
    comentarios: null,
    compania: null,
    plataforma: null,
    idioma: null,
    snapshot: null,
    ...parcial,
  };
}

describe('EstanteriaPage', () => {
  let http: HttpTestingController;

  function crear(estanteria: string) {
    TestBed.configureTestingModule({
      imports: [EstanteriaPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { data: { estanteria } } } },
      ],
    });
    const fixture = TestBed.createComponent(EstanteriaPage);
    http = TestBed.inject(HttpTestingController);
    return fixture;
  }

  function titulos(fixture: ReturnType<typeof crear>): string[] {
    return Array.from(
      fixture.nativeElement.querySelectorAll('.tarjeta h3') as NodeListOf<HTMLElement>,
    ).map((elemento) => elemento.textContent?.trim() ?? '');
  }

  afterEach(() => http.verify());

  it('solo muestra los Tipos de su Estantería', () => {
    const fixture = crear('pantalla');
    http
      .expectOne('/api/fichas')
      .flush([
        ficha({ id: 'peli', titulo: 'Peli' }),
        ficha({ id: 'serie', titulo: 'Serie', tipo: 'Serie' }),
        ficha({ id: 'libro', titulo: 'Libro', tipo: 'Libro' }),
        ficha({ id: 'juego', titulo: 'Juego', tipo: 'Juego' }),
      ]);
    fixture.detectChanges();

    expect(titulos(fixture).sort()).toEqual(['Peli', 'Serie']);
  });

  it('el segmented filtra por Tipo en Estanterías dobles', () => {
    const fixture = crear('pantalla');
    http
      .expectOne('/api/fichas')
      .flush([
        ficha({ id: 'peli', titulo: 'Peli' }),
        ficha({ id: 'serie', titulo: 'Serie', tipo: 'Serie' }),
      ]);
    fixture.detectChanges();

    const botonSerie = Array.from(
      fixture.nativeElement.querySelectorAll('.segmented button') as NodeListOf<HTMLButtonElement>,
    ).find((boton) => boton.textContent?.trim() === 'Serie');
    botonSerie?.click();
    fixture.detectChanges();

    expect(titulos(fixture)).toEqual(['Serie']);
  });

  it('agrupa por mes descendente con etiqueta en español', () => {
    const fixture = crear('pantalla');
    http
      .expectOne('/api/fichas')
      .flush([
        ficha({ id: 'marzo', titulo: 'Marzo', fecha: '2026-03-14' }),
        ficha({ id: 'enero', titulo: 'Enero', fecha: '2026-01-05' }),
        ficha({ id: 'marzo-2', titulo: 'Marzo dos', fecha: '2026-03-02' }),
      ]);
    fixture.detectChanges();

    const meses = Array.from(
      fixture.nativeElement.querySelectorAll('.mes-titulo') as NodeListOf<HTMLElement>,
    ).map((elemento) => elemento.textContent?.trim());
    expect(meses).toEqual(['marzo 2026', 'enero 2026']);
  });

  it('busca por título ignorando acentos', () => {
    const fixture = crear('pantalla');
    http
      .expectOne('/api/fichas')
      .flush([
        ficha({ id: 'aguilas', titulo: 'Ciudad de las águilas' }),
        ficha({ id: 'otra', titulo: 'Otra cosa' }),
      ]);
    fixture.detectChanges();

    const buscador = fixture.nativeElement.querySelector('#busqueda') as HTMLInputElement;
    buscador.value = 'aguilas';
    buscador.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();

    expect(titulos(fixture)).toEqual(['Ciudad de las águilas']);
  });

  it('pinta la tarjeta con y sin Snapshot', () => {
    const fixture = crear('pantalla');
    http.expectOne('/api/fichas').flush([
      ficha({
        id: 'con',
        titulo: 'Con Snapshot',
        fecha: '2026-03-10',
        terminado: false,
        valoracion: 8.5,
        snapshot,
      }),
      ficha({ id: 'sin', titulo: 'Sin Snapshot', fecha: '2026-03-09' }),
    ]);
    fixture.detectChanges();

    const tarjetas = fixture.nativeElement.querySelectorAll('.tarjeta') as NodeListOf<HTMLElement>;
    const con = tarjetas[0];
    expect(con.querySelector('.portada img')?.getAttribute('src')).toBe(snapshot.portada);
    expect(con.querySelector('.genero-chip')?.textContent?.trim()).toBe('Ciencia ficción');
    expect(con.querySelector('.valoracion')?.textContent).toContain('8.5');
    expect(con.querySelector('.nota-externa')?.textContent?.trim()).toBe('7.8');
    expect(con.querySelector('.marca-no-continuada')?.textContent?.trim()).toBe('No continuada');

    expect(tarjetas[1].querySelector('.portada-placeholder')).not.toBeNull();
  });

  it('añade el sufijo MC a la Nota Externa de RAWG', () => {
    const fixture = crear('juegos');
    http.expectOne('/api/fichas').flush([
      ficha({
        id: 'juego',
        titulo: 'God of War',
        tipo: 'Juego',
        snapshot: { ...snapshot, fuente: 'rawg', notaExterna: 84, generos: ['Acción'] },
      }),
    ]);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.nota-externa')?.textContent?.trim()).toBe('84 MC');
  });

  it('el + abre el formulario con los Tipos de la Estantería', () => {
    const fixture = crear('juegos');
    http.expectOne('/api/fichas').flush([]);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.primary-button') as HTMLButtonElement).click();
    fixture.detectChanges();

    const panel = fixture.nativeElement.querySelector('app-ficha-form-panel') as HTMLElement;
    expect(panel).not.toBeNull();
    const opciones = Array.from(
      panel.querySelectorAll('#tipo option') as NodeListOf<HTMLElement>,
    ).map((opcion) => opcion.textContent?.trim());
    expect(opciones).toEqual(['Juego']);
  });
});
