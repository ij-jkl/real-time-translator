import {Component, inject, ViewChild, ElementRef, AfterViewInit, OnDestroy} from '@angular/core';

import { CommonModule } from '@angular/common';
import { WebSocketService } from '../../core/services/web-socket.service';

@Component({
  selector: 'app-transcribe',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './transcribe.html'
})
export class TranscribeComponent implements AfterViewInit, OnDestroy {

  private readonly ws = inject(WebSocketService);

  @ViewChild('transcriptionContent') transcriptionContentRef!: ElementRef<HTMLDivElement>;

  readonly connect = this.ws.connect;
  readonly disconnect = this.ws.disconnect;
  readonly clear = this.ws.clear;
  readonly status = this.ws.status;
  readonly transcription = this.ws.transcription;
  readonly totalWords = this.ws.totalWords;
  readonly sessionTime = this.ws.sessionTime;
  readonly chunksProcessed = this.ws.chunksProcessed;

  private mutationObserver?: MutationObserver;

  get statusList() {
    const s = this.status();
    return [
      {
        type: 'websocket',
        connected: s.websocket === 'connected',
        ariaLabel: s.websocket === 'connected' ? 'WebSocket conectado' : 'WebSocket desconectado',
        text: s.websocket === 'connected' ? 'WebSocket Conectado' : 'WebSocket Desconectado',
      },
      {
        type: 'recording',
        connected: s.recording === 'recording',
        ariaLabel: s.recording === 'recording' ? 'Grabando' : 'Grabación detenida',
        text: s.recording === 'recording' ? 'Grabando' : 'Grabación Detenida',
      },
      {
        type: 'whisper',
        connected: s.whisper === 'processing',
        ariaLabel: s.whisper === 'processing' ? 'Whisper procesando' : 'Whisper inactivo',
        text: s.whisper === 'processing' ? 'Whisper Procesando' : 'Whisper Inactivo',
      },
    ];
  }

  getStatusDotColor(status: { type: string; connected: boolean }): string {
    if (!status.connected) {
      return 'bg-red-500';
    }

    return 'bg-green-500';
  }

  ngAfterViewInit(): void {
    const container = this.transcriptionContentRef?.nativeElement;
    if (!container) return;

    setTimeout(() => {
      container.scrollTop = container.scrollHeight;
    }, 0);

    this.mutationObserver = new MutationObserver(() => {
      const { scrollHeight, clientHeight, scrollTop } = container;
      const nearBottom = scrollTop + clientHeight >= scrollHeight - clientHeight * 0.2;

      if (scrollHeight > clientHeight * 1.1 && nearBottom) {
        container.scrollTo({ top: scrollHeight, behavior: 'smooth' });
      }
    });

    this.mutationObserver.observe(container, {
      childList: true,
      subtree: true,
      characterData: true
    });
  }

  ngOnDestroy(): void {
    this.mutationObserver?.disconnect();
  }
}
