import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { LibreriaLayout } from './libreria-layout';

describe('LibreriaLayout', () => {
  it('muestra la Copia de seguridad como icono con nombre accesible y tooltip', () => {
    TestBed.configureTestingModule({
      imports: [LibreriaLayout],
      providers: [provideRouter([])],
    });
    const fixture = TestBed.createComponent(LibreriaLayout);
    fixture.detectChanges();

    const enlace = fixture.nativeElement.querySelector('.nav-icono') as HTMLAnchorElement;
    expect(enlace.getAttribute('href')).toBe('/copia-seguridad');
    expect(enlace.getAttribute('aria-label')).toBe('Copia de seguridad');
    expect(enlace.dataset['tooltip']).toBe('Copia de seguridad');
    expect(enlace.textContent?.trim()).toBe('');
  });
});
