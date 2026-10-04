import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Dashboard } from './dashboard';
import type { Ficha, Snapshot } from './fichas/ficha-api';

const snapshot: Snapshot = {
  fuente: 'tmdb',
  idExterno: '438631',
  portada: 'https://image.tmdb.org/t/p/w500/dune.jpg',
  anio: 2021,
  autoria: 'Denis Villeneuve',
  notaExterna: 7.8,
  generos: ['Ciencia ficción', 'Aventura'],
};

describe('Dashboard enriquecimiento', () => {
  let fixture: ComponentFixture<Dashboard>;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Dashboard],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    fixture = TestBed.createComponent(Dashboard);
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/health').flush({ status: 'ok', databaseConfigured: true });
    http.expectOne('/api/fichas').flush([]);
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('prefills the title and sends the chosen snapshot when creating a ficha', async () => {
    (
      fixture.nativeElement.querySelector('.page-heading .primary-button') as HTMLButtonElement
    ).click();
    fixture.detectChanges();

    const consulta = fixture.nativeElement.querySelector('#consulta') as HTMLInputElement;
    consulta.value = 'Dune';
    consulta.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
    (
      fixture.nativeElement.querySelector(
        'app-enriquecimiento-panel button[type="submit"]',
      ) as HTMLButtonElement
    ).click();
    fixture.detectChanges();

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

    (fixture.nativeElement.querySelector('.form-panel form') as HTMLFormElement).dispatchEvent(
      new Event('submit', { bubbles: true, cancelable: true }),
    );
    const createRequest = http.expectOne('/api/fichas');
    expect(createRequest.request.method).toBe('POST');
    expect(createRequest.request.body.titulo).toBe('Dune');
    expect(createRequest.request.body.snapshot).toEqual(snapshot);
    createRequest.flush({
      id: 'ficha-dune',
      titulo: 'Dune',
      tipo: 'Película',
      fecha: null,
      terminado: false,
      valoracion: null,
      comentarios: null,
      compania: null,
      snapshot,
    });
  });

  it('shows the portada, Nota Externa and genre chips on a ficha card', () => {
    const ficha: Ficha = {
      id: 'ficha-dune',
      titulo: 'Dune',
      tipo: 'Película',
      fecha: null,
      terminado: false,
      valoracion: null,
      comentarios: null,
      compania: null,
      snapshot,
    };

    fixture.destroy();
    fixture = TestBed.createComponent(Dashboard);
    fixture.detectChanges();
    http.expectOne('/api/health').flush({ status: 'ok', databaseConfigured: true });
    http.expectOne('/api/fichas').flush([
      ficha,
      {
        ...ficha,
        id: 'ficha-sin-portada',
        titulo: 'Ficha sin portada',
        snapshot: { ...snapshot, portada: null },
      },
    ]);
    fixture.detectChanges();

    const cards = fixture.nativeElement.querySelectorAll('.ficha-card') as NodeListOf<HTMLElement>;
    const card = cards[0];
    expect(card.querySelector('.ficha-portada')?.getAttribute('src')).toBe(snapshot.portada);
    expect(card.textContent).toContain('Nota Externa 7.8');
    expect(card.querySelectorAll('.genero-chip').length).toBe(2);
    expect(card.textContent).toContain('Ciencia ficción');
    expect(card.textContent).toContain('Aventura');
    expect(cards[1].querySelector('.ficha-portada-placeholder')?.textContent).toContain(
      'Portada no disponible',
    );
  });
});
