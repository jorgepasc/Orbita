import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';

interface BackendHealth {
  status: string;
  databaseConfigured: boolean;
}

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly http = inject(HttpClient);
  protected readonly backendStatus = signal<'loading' | 'ready' | 'offline'>('loading');
  protected readonly databaseConfigured = signal(false);

  constructor() {
    this.http.get<BackendHealth>('/api/health').subscribe({
      next: (health) => {
        this.databaseConfigured.set(health.databaseConfigured);
        this.backendStatus.set(health.status === 'ok' ? 'ready' : 'offline');
      },
      error: () => this.backendStatus.set('offline'),
    });
  }
}
