import { Component, inject, effect } from '@angular/core';
import { WebSocketService } from '../../core/services/web-socket.service';
import { AsyncPipe, NgIf } from '@angular/common';

@Component({
  selector: 'app-transcribe',
  standalone: true,
  imports: [NgIf, AsyncPipe],
  templateUrl: './transcribe.html',
  styleUrls: ['./transcribe.scss']
})
export class TranscribeComponent {
  private ws = inject(WebSocketService);

  connect = this.ws.connect;
  disconnect = this.ws.disconnect;
  clear = this.ws.clear;

  status = this.ws.status;
  transcription = this.ws.transcription;
  totalWords = this.ws.totalWords;
  sessionTime = this.ws.sessionTime;
  chunksProcessed = this.ws.chunksProcessed;

  constructor() {
    effect(() => {
      console.log('Status:', this.status());
    });
  }
}
