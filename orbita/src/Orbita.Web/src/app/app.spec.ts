import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { Dashboard } from './dashboard';

describe('Dashboard', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Dashboard],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(Dashboard);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should show the backend status returned by the API', () => {
    const fixture = TestBed.createComponent(Dashboard);
    const http = TestBed.inject(HttpTestingController);

    fixture.detectChanges();

    const request = http.expectOne('/api/health');
    request.flush({ status: 'ok', databaseConfigured: true });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.status')?.textContent).toContain('Operativo');
    expect(compiled.querySelector('.database-state strong')?.textContent).toContain(
      'Configuración recibida por Aspire',
    );

    http.verify();
  });
});
