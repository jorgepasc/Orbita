import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { CopiaSeguridadApi } from './copia-seguridad-api';

@Component({
  selector: 'app-copia-seguridad-page',
  templateUrl: './copia-seguridad-page.html',
  styleUrl: './copia-seguridad-page.scss',
})
export class CopiaSeguridadPage {
  private readonly api = inject(CopiaSeguridadApi);

  protected readonly ocupado = signal(false);
  protected readonly resultado = signal<string | null>(null);
  protected readonly errores = signal<string[]>([]);

  protected descargar(): void {
    this.iniciar();
    this.api.exportar().subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const enlace = document.createElement('a');
        enlace.href = url;
        enlace.download = `orbita-${new Date().toISOString().slice(0, 10)}.json`;
        enlace.click();
        URL.revokeObjectURL(url);
        this.ocupado.set(false);
        this.resultado.set('Copia descargada.');
      },
      error: () => this.fallar(['No se pudo descargar la copia. Inténtalo de nuevo.']),
    });
  }

  protected async restaurar(evento: Event): Promise<void> {
    const input = evento.target as HTMLInputElement;
    const fichero = input.files?.[0];
    input.value = '';
    if (!fichero) {
      return;
    }

    this.iniciar();

    let documento: unknown;
    try {
      documento = JSON.parse(await fichero.text());
    } catch {
      this.fallar(['El fichero no es un JSON válido.']);
      return;
    }

    this.api.importar(documento).subscribe({
      next: (resumen) => {
        this.ocupado.set(false);
        this.resultado.set(
          `Copia restaurada: ${resumen.creadas} creadas, ${resumen.actualizadas} actualizadas.`,
        );
      },
      error: (error: unknown) => this.fallar(mensajesDeError(error)),
    });
  }

  private iniciar(): void {
    this.ocupado.set(true);
    this.resultado.set(null);
    this.errores.set([]);
  }

  private fallar(mensajes: string[]): void {
    this.ocupado.set(false);
    this.errores.set(mensajes);
  }
}

function mensajesDeError(error: unknown): string[] {
  if (error instanceof HttpErrorResponse && error.status === 400) {
    const detalle = (error.error as { errors?: Record<string, string[]> } | null)?.errors ?? {};
    const mensajes = Object.entries(detalle).flatMap(([campo, textos]) =>
      textos.map((texto) => `${campo}: ${texto}`),
    );
    if (mensajes.length > 0) {
      return ['No se restauró nada. Corrige el fichero:', ...mensajes];
    }
  }
  return ['No se pudo restaurar la copia. Inténtalo de nuevo.'];
}
