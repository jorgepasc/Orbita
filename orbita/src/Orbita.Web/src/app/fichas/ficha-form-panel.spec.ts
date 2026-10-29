import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import type { Snapshot, TipoFicha } from './ficha-api';
import { FichaFormPanel } from './ficha-form-panel';

const snapshot: Snapshot = {
  fuente: 'tmdb',
  idExterno: '438631',
  portada: 'https://image.tmdb.org/t/p/w500/dune.jpg',
  anio: 2021,
  autoria: 'Denis Villeneuve',
  notaExterna: 7.8,
  generos: ['Ciencia ficción', 'Aventura'],
};

function escribir(
  fixture: ComponentFixture<FichaFormPanel>,
  selector: string,
  valor: string,
): void {
  const input = fixture.nativeElement.querySelector(selector) as HTMLInputElement;
  input.value = valor;
  input.dispatchEvent(new Event('input', { bubbles: true }));
  fixture.detectChanges();
}

describe('FichaFormPanel', () => {
  let http: HttpTestingController;

  function crear(tipos: readonly TipoFicha[]) {
    TestBed.configureTestingModule({
      imports: [FichaFormPanel],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const fixture = TestBed.createComponent(FichaFormPanel);
    fixture.componentRef.setInput('tiposPermitidos', tipos);
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    return fixture;
  }

  function deshabilitado(fixture: ComponentFixture<FichaFormPanel>): boolean {
    return (
      fixture.nativeElement.querySelector('.ficha-form button[type="submit"]') as HTMLButtonElement
    ).disabled;
  }

  afterEach(() => http.verify());

  it('prerrellena el título y envía el Snapshot elegido al crear', async () => {
    const fixture = crear(['Película']);

    escribir(fixture, '#consulta', 'Dune');
    (
      fixture.nativeElement.querySelector(
        'app-enriquecimiento-panel button[type="submit"]',
      ) as HTMLButtonElement
    ).click();
    fixture.detectChanges();

    http.expectOne('/api/enriquecimiento/resultados?q=Dune&tipo=Serie').flush([]);
    http.expectOne('/api/enriquecimiento/resultados?q=Dune&tipo=Pel%C3%ADcula').flush([
      {
        idExterno: '438631',
        titulo: 'Dune',
        anio: 2021,
        portada: 'https://image.tmdb.org/t/p/w154/dune.jpg',
        notaExterna: 7.8,
      },
    ]);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.resultado') as HTMLButtonElement).click();
    http
      .expectOne('/api/enriquecimiento/snapshot?tipo=Pel%C3%ADcula&idExterno=438631')
      .flush(snapshot);
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement.querySelector('#titulo') as HTMLInputElement).value).toBe('Dune');
    expect(fixture.nativeElement.querySelector('.snapshot-preview')).not.toBeNull();

    escribir(fixture, '#fecha', '2025-03-01');
    (fixture.nativeElement.querySelector('.ficha-form') as HTMLFormElement).dispatchEvent(
      new Event('submit', { bubbles: true, cancelable: true }),
    );

    const peticion = http.expectOne('/api/fichas');
    expect(peticion.request.method).toBe('POST');
    expect(peticion.request.body.titulo).toBe('Dune');
    expect(peticion.request.body.snapshot).toEqual(snapshot);
    peticion.flush({
      id: 'ficha-dune',
      titulo: 'Dune',
      tipo: 'Película',
      fecha: '2025-03-01',
      terminado: false,
      valoracion: null,
      comentarios: null,
      compania: null,
      plataforma: null,
      idioma: null,
      autor: null,
      snapshot,
    });
  });

  it('exige la Fecha siempre para poder guardar', () => {
    const fixture = crear(['Película']);
    expect(deshabilitado(fixture)).toBe(true);

    escribir(fixture, '#titulo', 'Dune');
    expect(deshabilitado(fixture)).toBe(true);

    escribir(fixture, '#fecha', '2025-03-01');
    expect(deshabilitado(fixture)).toBe(false);
  });

  it('muestra Plataforma con Juego y la envía', () => {
    const fixture = crear(['Juego']);
    expect(fixture.nativeElement.querySelector('#plataforma')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('#idioma')).toBeNull();

    escribir(fixture, '#titulo', 'God of War');
    escribir(fixture, '#fecha', '2025-03-03');
    escribir(fixture, '#plataforma', 'PS5');
    (fixture.nativeElement.querySelector('.ficha-form') as HTMLFormElement).dispatchEvent(
      new Event('submit', { bubbles: true, cancelable: true }),
    );

    const peticion = http.expectOne('/api/fichas');
    expect(peticion.request.body.plataforma).toBe('PS5');
    peticion.flush({});
  });

  it('muestra Idioma con Libro o Cómic y no Plataforma', () => {
    const fixture = crear(['Libro', 'Cómic']);
    expect(fixture.nativeElement.querySelector('#idioma')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('#plataforma')).toBeNull();
  });

  it('muestra Autor y oculta Compañía con Libro', () => {
    const fixture = crear(['Libro']);
    expect(fixture.nativeElement.querySelector('#autor')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('#compania')).toBeNull();

    escribir(fixture, '#titulo', 'Dune');
    escribir(fixture, '#fecha', '2025-03-04');
    escribir(fixture, '#autor', 'Frank Herbert');
    (fixture.nativeElement.querySelector('.ficha-form') as HTMLFormElement).dispatchEvent(
      new Event('submit', { bubbles: true, cancelable: true }),
    );

    const peticion = http.expectOne('/api/fichas');
    expect(peticion.request.body.autor).toBe('Frank Herbert');
    expect(peticion.request.body.compania).toBeNull();
    peticion.flush({});
  });

  async function enriquecerLibro(fixture: ComponentFixture<FichaFormPanel>): Promise<void> {
    escribir(fixture, '#consulta', 'Dune');
    (
      fixture.nativeElement.querySelector(
        'app-enriquecimiento-panel button[type="submit"]',
      ) as HTMLButtonElement
    ).click();
    fixture.detectChanges();

    http.expectOne('/api/enriquecimiento/resultados?q=Dune&tipo=Libro').flush([
      {
        idExterno: '/works/OL893415W',
        titulo: 'Dune',
        anio: 1965,
        portada: null,
        notaExterna: null,
      },
    ]);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.resultado') as HTMLButtonElement).click();
    http
      .expectOne((peticion) => peticion.url === '/api/enriquecimiento/snapshot')
      .flush({
        ...snapshot,
        fuente: 'openlibrary',
        idExterno: '/works/OL893415W',
        autoria: 'Frank Herbert',
        notaExterna: null,
      });
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('autorrellena el Autor del Libro con la autoría del Snapshot', async () => {
    const fixture = crear(['Libro']);

    await enriquecerLibro(fixture);

    expect((fixture.nativeElement.querySelector('#autor') as HTMLInputElement).value).toBe(
      'Frank Herbert',
    );
  });

  it('no sobrescribe el Autor que ya escribió el usuario', async () => {
    const fixture = crear(['Libro']);
    escribir(fixture, '#autor', 'F. Herbert');

    await enriquecerLibro(fixture);

    expect((fixture.nativeElement.querySelector('#autor') as HTMLInputElement).value).toBe(
      'F. Herbert',
    );
  });

  it('muestra Compañía con su placeholder y no Autor fuera de Libro', () => {
    const fixture = crear(['Película']);
    const compania = fixture.nativeElement.querySelector('#compania') as HTMLInputElement;

    expect(compania.placeholder).toBe('Family, solo...');
    expect(fixture.nativeElement.querySelector('#autor')).toBeNull();
  });

  it('marca Terminado por defecto', async () => {
    const fixture = crear(['Película']);
    await fixture.whenStable();
    const terminado = fixture.nativeElement.querySelector('#terminado') as HTMLInputElement;

    expect(terminado.checked).toBe(true);
  });

  it('limita el selector de Tipo a los permitidos', () => {
    const fixture = crear(['Libro', 'Cómic']);
    const opciones = Array.from(
      fixture.nativeElement.querySelectorAll('#tipo option') as NodeListOf<HTMLOptionElement>,
    ).map((opcion) => opcion.textContent?.trim());

    expect(opciones).toEqual(['Libro', 'Cómic']);
  });
});
