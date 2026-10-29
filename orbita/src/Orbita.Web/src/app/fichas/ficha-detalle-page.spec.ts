import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, Router } from '@angular/router';
import type { Ficha, Snapshot } from './ficha-api';
import { FichaDetallePage } from './ficha-detalle-page';

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
    autor: null,
    snapshot: null,
    ...parcial,
  };
}

describe('FichaDetallePage', () => {
  let http: HttpTestingController;

  function crear(id = 'ficha-1') {
    TestBed.configureTestingModule({
      imports: [FichaDetallePage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => id } } } },
      ],
    });
    const fixture = TestBed.createComponent(FichaDetallePage);
    http = TestBed.inject(HttpTestingController);
    return fixture;
  }

  afterEach(() => http.verify());

  it('etiqueta la autoría como Director en Película', () => {
    const fixture = crear();
    http.expectOne('/api/fichas/ficha-1').flush(ficha({ id: 'ficha-1', titulo: 'Dune', snapshot }));
    fixture.detectChanges();

    const autoria = fixture.nativeElement.querySelector('.autoria') as HTMLElement;
    expect(autoria.textContent).toContain('Director:');
    expect(autoria.textContent).toContain('Denis Villeneuve');
  });

  it('muestra reparto, países y duración de los detalles de Película', () => {
    const fixture = crear();
    http.expectOne('/api/fichas/ficha-1').flush(
      ficha({
        id: 'ficha-1',
        titulo: 'Dune',
        snapshot: {
          ...snapshot,
          detalles: {
            tipo: 'pelicula',
            actores: ['Timothée Chalamet', 'Zendaya'],
            paises: ['US', 'CA'],
            duracionMinutos: 155,
          },
        },
      }),
    );
    fixture.detectChanges();

    const texto = (fixture.nativeElement as HTMLElement).textContent;
    expect(texto).toContain('Reparto: Timothée Chalamet, Zendaya');
    expect(texto).toContain('País: Estados Unidos, Canadá');
    expect(texto).toContain('Duración: 155 min');
  });

  it('muestra las páginas de los detalles de Libro', () => {
    const fixture = crear();
    http.expectOne('/api/fichas/ficha-1').flush(
      ficha({
        id: 'ficha-1',
        titulo: 'El nombre del viento',
        tipo: 'Libro',
        snapshot: {
          ...snapshot,
          fuente: 'openlibrary',
          detalles: { tipo: 'libro', paginas: 662 },
        },
      }),
    );
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Páginas: 662');
  });

  it('muestra plataformas y horas medias de los detalles de Juego', () => {
    const fixture = crear();
    http.expectOne('/api/fichas/ficha-1').flush(
      ficha({
        id: 'ficha-1',
        titulo: 'God of War',
        tipo: 'Juego',
        snapshot: {
          ...snapshot,
          fuente: 'rawg',
          detalles: { tipo: 'juego', plataformas: ['PlayStation 5', 'PC'], horasMedias: 14 },
        },
      }),
    );
    fixture.detectChanges();

    const texto = (fixture.nativeElement as HTMLElement).textContent;
    expect(texto).toContain('Plataformas: PlayStation 5, PC');
    expect(texto).toContain('Duración media: 14 h');
  });

  it('no muestra datos de detalles si el Snapshot no los trae', () => {
    const fixture = crear();
    http.expectOne('/api/fichas/ficha-1').flush(ficha({ id: 'ficha-1', titulo: 'Dune', snapshot }));
    fixture.detectChanges();

    const texto = (fixture.nativeElement as HTMLElement).textContent;
    expect(texto).not.toContain('Reparto:');
    expect(texto).not.toContain('Duración:');
  });

  it('etiqueta la autoría como Estudio en Juego', () => {
    const fixture = crear();
    http.expectOne('/api/fichas/ficha-1').flush(
      ficha({
        id: 'ficha-1',
        titulo: 'God of War',
        tipo: 'Juego',
        snapshot: { ...snapshot, fuente: 'rawg', autoria: 'Santa Monica Studio', notaExterna: 94 },
      }),
    );
    fixture.detectChanges();

    expect((fixture.nativeElement.querySelector('.autoria') as HTMLElement).textContent).toContain(
      'Estudio:',
    );
    expect(
      (fixture.nativeElement.querySelector('.nota-externa') as HTMLElement).textContent,
    ).toContain('94 MC');
  });

  it('no pinta Nota Externa en Libro aunque el Snapshot la traiga', () => {
    const fixture = crear();
    http.expectOne('/api/fichas/ficha-1').flush(
      ficha({
        id: 'ficha-1',
        titulo: 'El nombre del viento',
        tipo: 'Libro',
        snapshot: { ...snapshot, fuente: 'openlibrary', notaExterna: 5 },
      }),
    );
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.nota-externa')).toBeNull();
  });

  it('una Ficha manual muestra solo el título', () => {
    const fixture = crear();
    http
      .expectOne('/api/fichas/ficha-1')
      .flush(ficha({ id: 'ficha-1', titulo: 'Ficha manual', snapshot: null }));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('h1')?.textContent).toContain('Ficha manual');
    expect(fixture.nativeElement.querySelector('.portada')).toBeNull();
    expect(fixture.nativeElement.querySelector('.generos')).toBeNull();
  });

  it('Editar abre el formulario reutilizado', async () => {
    const fixture = crear();
    http.expectOne('/api/fichas/ficha-1').flush(ficha({ id: 'ficha-1', titulo: 'Dune', snapshot }));
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.secondary-button') as HTMLButtonElement).click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const panel = fixture.nativeElement.querySelector('app-ficha-form-panel') as HTMLElement;
    expect(panel).not.toBeNull();
    expect((panel.querySelector('#titulo') as HTMLInputElement).value).toBe('Dune');
  });

  it('al editar una Ficha con Snapshot solo ofrece Tipos compatibles con su fuente', async () => {
    const fixture = crear();
    http.expectOne('/api/fichas/ficha-1').flush(ficha({ id: 'ficha-1', titulo: 'Dune', snapshot }));
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.secondary-button') as HTMLButtonElement).click();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const panel = fixture.nativeElement.querySelector('app-ficha-form-panel') as HTMLElement;
    const opciones = Array.from(
      panel.querySelectorAll('#tipo option') as NodeListOf<HTMLOptionElement>,
    ).map((opcion) => opcion.textContent?.trim());
    expect(opciones).toEqual(['Película', 'Serie']);
  });

  it('Borrar pide confirmación y vuelve a su Estantería', () => {
    const fixture = crear();
    const router = TestBed.inject(Router);
    const navegacion = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    http
      .expectOne('/api/fichas/ficha-1')
      .flush(ficha({ id: 'ficha-1', titulo: 'God of War', tipo: 'Juego' }));
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.delete-button') as HTMLButtonElement).click();

    http.expectOne('/api/fichas/ficha-1').flush(null, { status: 204, statusText: 'No Content' });
    expect(navegacion).toHaveBeenCalledWith('/juegos');
  });
});
