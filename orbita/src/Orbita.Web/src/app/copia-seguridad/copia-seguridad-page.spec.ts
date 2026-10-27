import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CopiaSeguridadPage } from './copia-seguridad-page';

function seleccionar(fixture: { nativeElement: HTMLElement }, contenido: string): Promise<void> {
  const input = fixture.nativeElement.querySelector('input[type="file"]') as HTMLInputElement;
  const fichero = new File([contenido], 'copia.json', { type: 'application/json' });
  Object.defineProperty(input, 'files', { value: [fichero], configurable: true });
  input.dispatchEvent(new Event('change'));
  return new Promise((resolve) => setTimeout(resolve));
}

describe('CopiaSeguridadPage', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CopiaSeguridadPage],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
  });

  it('downloads the backup as a blob', () => {
    const fixture = TestBed.createComponent(CopiaSeguridadPage);
    const http = TestBed.inject(HttpTestingController);
    URL.createObjectURL = vi.fn(() => 'blob:copia');
    URL.revokeObjectURL = vi.fn();
    const clic = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();
    const request = http.expectOne('/api/copia-seguridad');
    expect(request.request.method).toBe('GET');
    expect(request.request.responseType).toBe('blob');
    request.flush(new Blob(['{}']));
    fixture.detectChanges();

    expect(clic).toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).toContain('Copia descargada.');
    http.verify();
  });

  it('posts the selected file and shows the summary', async () => {
    const fixture = TestBed.createComponent(CopiaSeguridadPage);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    await seleccionar(fixture, '{"version":1,"fichas":[]}');
    const request = http.expectOne('/api/copia-seguridad');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ version: 1, fichas: [] });
    request.flush({ creadas: 2, actualizadas: 3 });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('2 creadas, 3 actualizadas');
    http.verify();
  });

  it('rejects a file that is not valid JSON without calling the API', async () => {
    const fixture = TestBed.createComponent(CopiaSeguridadPage);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    await seleccionar(fixture, 'no es json');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('El fichero no es un JSON válido.');
    http.verify();
  });

  it('lists the validation errors returned by the API', async () => {
    const fixture = TestBed.createComponent(CopiaSeguridadPage);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    await seleccionar(fixture, '{"version":1,"fichas":[{}]}');
    http
      .expectOne('/api/copia-seguridad')
      .flush(
        { errors: { 'fichas[0].Titulo': ['El título es obligatorio.'] } },
        { status: 400, statusText: 'Bad Request' },
      );
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(
      'fichas[0].Titulo: El título es obligatorio.',
    );
    http.verify();
  });
});
