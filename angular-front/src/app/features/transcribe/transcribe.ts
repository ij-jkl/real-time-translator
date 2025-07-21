import { Component, inject, effect, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { WebSocketService } from '../../core/services/web-socket.service';
import { AsyncPipe, NgIf, CommonModule } from '@angular/common';

@Component({
  selector: 'app-transcribe',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './transcribe.html',
  styleUrls: ['./transcribe.scss']
})

export class TranscribeComponent implements AfterViewInit {
  private ws = inject(WebSocketService);

  @ViewChild('transcriptionContent') transcriptionContentRef!: ElementRef<HTMLDivElement>;

  connect = this.ws.connect;
  disconnect = this.ws.disconnect;
  clear = this.ws.clear;

  status = this.ws.status;
  transcription = this.ws.transcription;
  totalWords = this.ws.totalWords;
  sessionTime = this.ws.sessionTime;
  chunksProcessed = this.ws.chunksProcessed;

  get statusList() {
    const s = this.status();
    return [
      {
        connected: s.websocket === 'connected',
        ariaLabel: s.websocket === 'connected' ? 'WebSocket conectado' : 'WebSocket desconectado',
        text: s.websocket === 'connected' ? 'WebSocket Conectado' : 'WebSocket Desconectado',
      },
      {
        connected: s.recording === 'recording',
        ariaLabel: s.recording === 'recording' ? 'Grabando' : 'Grabación detenida',
        text: s.recording === 'recording' ? 'Grabando' : 'Grabación Detenida',
      },
      {
        connected: s.whisper === 'processing',
        ariaLabel: s.whisper === 'processing' ? 'Whisper procesando' : 'Whisper inactivo',
        text: s.whisper === 'processing' ? 'Whisper Procesando' : 'Whisper Inactivo',
      },
    ];
  }

  private mutationObserver?: MutationObserver;

  constructor() {
    // No effect here for scroll, will use MutationObserver in ngAfterViewInit
  }

  ngAfterViewInit() {
    if (this.transcriptionContentRef && this.transcriptionContentRef.nativeElement) {
      setTimeout(() => {
        const el = this.transcriptionContentRef.nativeElement;
        el.scrollTop = el.scrollHeight;
      }, 0);

      this.mutationObserver = new MutationObserver(() => {
        const el = this.transcriptionContentRef.nativeElement;
        const { scrollHeight, clientHeight, scrollTop } = el;
        if (scrollHeight > clientHeight * 1.1) {
          if (scrollTop + clientHeight >= scrollHeight - clientHeight * 0.2) {
            el.scrollTo({ top: scrollHeight, behavior: 'smooth' });
          }
        }
      });
      this.mutationObserver.observe(this.transcriptionContentRef.nativeElement, {
        childList: true,
        subtree: true,
        characterData: true
      });
    }
  }

  ngOnDestroy() {
    if (this.mutationObserver) {
      this.mutationObserver.disconnect();
    }
  }
}
