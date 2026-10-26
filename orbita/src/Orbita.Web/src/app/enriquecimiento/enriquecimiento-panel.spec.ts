import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import type { Snapshot } from '../fichas/ficha-api';
import { EnriquecimientoPanel } from './enriquecimiento-panel';

const snapshot: Snapshot = {
  fuente: 'tmdb',
  idExterno: '438631',
  portada: 'https://image.tmdb.org/t/p/w500/dune.jpg',
  anio: 2021,
  autoria: 'Denis Villeneuve',
  notaExterna: 7.8,
  generos: ['Ciencia ficción', 'Aventura'],
};

describe('EnriquecimientoPanel', () => {
  let fixture: ComponentFixture<EnriquecimientoPanel>;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [EnriquecimientoPanel],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    fixture = TestBed.createComponent(EnriquecimientoPanel);
    fixture.componentRef.setInput('tipo', 'Película');
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('queries the resultados endpoint for the tipo and shows empty state', async () => {
    await buscar('Dune');
    vaciarSeries();

    const request = http.expectOne('/api/enriquecimiento/resultados?q=Dune&tipo=Pel%C3%ADcula');
    expect(request.request.method).toBe('GET');
    request.flush([]);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Sin resultados');
  });

  it('renders the resultados with portada, titulo, anio and nota externa', async () => {
    await buscar('Dune');
    vaciarSeries();
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

    const portadas = fixture.nativeElement.querySelectorAll('.resultado img');
    expect(portadas.length).toBe(1);
    expect(portadas[0].getAttribute('src')).toBe('https://image.tmdb.org/t/p/w154/dune.jpg');
    expect(fixture.nativeElement.textContent).toContain('Dune');
    expect(fixture.nativeElement.textContent).toContain('2021');
    expect(fixture.nativeElement.textContent).toContain('7.8');
  });

  it('freezes the snapshot of the chosen resultado and emits it with its title', async () => {
    const elegido = vi.fn();
    fixture.componentInstance.elegido.subscribe(elegido);

    await buscar('Dune');
    vaciarSeries();
    http.expectOne('/api/enriquecimiento/resultados?q=Dune&tipo=Pel%C3%ADcula').flush([
      {
        idExterno: '438631',
        titulo: 'Dune',
        anio: 2021,
        portada: null,
        notaExterna: null,
      },
    ]);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.resultado') as HTMLButtonElement).click();

    const request = http.expectOne(
      '/api/enriquecimiento/snapshot?tipo=Pel%C3%ADcula&idExterno=438631',
    );
    expect(request.request.method).toBe('GET');
    request.flush(snapshot);

    expect(elegido).toHaveBeenCalledWith({ snapshot, titulo: 'Dune', tipo: 'Película' });
  });

  it('shows an error and keeps the manual escape when the API is down', async () => {
    await buscar('Dune');
    http
      .expectOne('/api/enriquecimiento/resultados?q=Dune&tipo=Serie')
      .flush(null, { status: 502, statusText: 'Bad Gateway' });
    http
      .expectOne('/api/enriquecimiento/resultados?q=Dune&tipo=Pel%C3%ADcula')
      .flush(null, { status: 502, statusText: 'Bad Gateway' });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No se pudo consultar la API externa');
    expect(fixture.nativeElement.textContent).toContain('Añadir manualmente');
  });

  it('searches Película and Serie together and tags each resultado with its tipo', async () => {
    await buscar('Dune');
    http
      .expectOne('/api/enriquecimiento/resultados?q=Dune&tipo=Pel%C3%ADcula')
      .flush([{ idExterno: '1', titulo: 'Dune', anio: 2021, portada: null, notaExterna: null }]);
    http.expectOne('/api/enriquecimiento/resultados?q=Dune&tipo=Serie').flush([
      {
        idExterno: '1',
        titulo: 'Dune: La hermandad',
        anio: 2024,
        portada: null,
        notaExterna: null,
      },
    ]);
    fixture.detectChanges();

    const etiquetas = Array.from(
      fixture.nativeElement.querySelectorAll('.resultado-tipo') as NodeListOf<HTMLElement>,
    ).map((etiqueta) => etiqueta.textContent?.trim());
    expect(etiquetas).toEqual(['Película', 'Serie']);
  });

  it('emits the tipo of the chosen Serie and freezes its snapshot with that tipo', async () => {
    const elegido = vi.fn();
    fixture.componentInstance.elegido.subscribe(elegido);

    await buscar('Dune');
    http.expectOne('/api/enriquecimiento/resultados?q=Dune&tipo=Pel%C3%ADcula').flush([]);
    http.expectOne('/api/enriquecimiento/resultados?q=Dune&tipo=Serie').flush([
      {
        idExterno: '99',
        titulo: 'Dune: La hermandad',
        anio: 2024,
        portada: null,
        notaExterna: null,
      },
    ]);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.resultado') as HTMLButtonElement).click();
    http
      .expectOne('/api/enriquecimiento/snapshot?tipo=Serie&idExterno=99')
      .flush({ ...snapshot, idExterno: '99' });

    expect(elegido).toHaveBeenCalledWith({
      snapshot: { ...snapshot, idExterno: '99' },
      titulo: 'Dune: La hermandad',
      tipo: 'Serie',
    });
  });

  it('keeps the resultados of one tipo when the other source fails', async () => {
    await buscar('Dune');
    http
      .expectOne('/api/enriquecimiento/resultados?q=Dune&tipo=Pel%C3%ADcula')
      .flush(null, { status: 502, statusText: 'Bad Gateway' });
    http.expectOne('/api/enriquecimiento/resultados?q=Dune&tipo=Serie').flush([
      {
        idExterno: '99',
        titulo: 'Dune: La hermandad',
        anio: 2024,
        portada: null,
        notaExterna: null,
      },
    ]);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('.resultado').length).toBe(1);
    expect(fixture.nativeElement.textContent).not.toContain('No se pudo consultar');
  });

  it('emits the manual escape when requested', () => {
    const manual = vi.fn();
    fixture.componentInstance.manual.subscribe(manual);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('.manual-link') as HTMLButtonElement).click();

    expect(manual).toHaveBeenCalledTimes(1);
  });

  it('discards the resultados when the tipo changes', async () => {
    await buscar('Dune');
    vaciarSeries();
    http.expectOne('/api/enriquecimiento/resultados?q=Dune&tipo=Pel%C3%ADcula').flush([
      {
        idExterno: '438631',
        titulo: 'Dune',
        anio: 2021,
        portada: null,
        notaExterna: null,
      },
    ]);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.resultado').length).toBe(1);

    fixture.componentRef.setInput('tipo', 'Libro');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('.resultado').length).toBe(0);
  });

  it('ignores resultados that arrive after the tipo changes', async () => {
    await buscar('Dune');
    vaciarSeries();
    const request = http.expectOne('/api/enriquecimiento/resultados?q=Dune&tipo=Pel%C3%ADcula');

    fixture.componentRef.setInput('tipo', 'Libro');
    fixture.detectChanges();
    request.flush([
      {
        idExterno: '438631',
        titulo: 'Dune',
        anio: 2021,
        portada: null,
        notaExterna: null,
      },
    ]);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('.resultado').length).toBe(0);
  });

  it('does not emit a snapshot that arrives after the tipo changes', async () => {
    const elegido = vi.fn();
    fixture.componentInstance.elegido.subscribe(elegido);

    await buscar('Dune');
    vaciarSeries();
    http.expectOne('/api/enriquecimiento/resultados?q=Dune&tipo=Pel%C3%ADcula').flush([
      {
        idExterno: '438631',
        titulo: 'Dune',
        anio: 2021,
        portada: null,
        notaExterna: null,
      },
    ]);
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('.resultado') as HTMLButtonElement).click();
    const request = http.expectOne(
      '/api/enriquecimiento/snapshot?tipo=Pel%C3%ADcula&idExterno=438631',
    );

    fixture.componentRef.setInput('tipo', 'Libro');
    fixture.detectChanges();
    request.flush(snapshot);

    expect(elegido).not.toHaveBeenCalled();
  });

  function vaciarSeries(): void {
    http.expectOne('/api/enriquecimiento/resultados?q=Dune&tipo=Serie').flush([]);
  }

  async function buscar(texto: string): Promise<void> {
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('#consulta') as HTMLInputElement;
    input.value = texto;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const botonBuscar = fixture.nativeElement.querySelector(
      'button[type="submit"]',
    ) as HTMLButtonElement;
    expect(botonBuscar.disabled).toBe(false);
    botonBuscar.click();
    fixture.detectChanges();
  }
});
