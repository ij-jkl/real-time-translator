import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent : () => import('./features/transcribe/transcribe').then(m => m.TranscribeComponent),
  },
];
