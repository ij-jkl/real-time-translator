import { Injectable, signal, computed } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class WebSocketService {
  private socket: WebSocket | null = null;
  private audioContext: AudioContext | null = null;
  private mediaStream: MediaStream | null = null;
  private processor: ScriptProcessorNode | null = null;

  private readonly _status = signal({
    websocket: 'disconnected',
    recording: 'idle',
    whisper: 'inactive'
  });

  private readonly _liveWords = signal<string[]>([]);
  private readonly _totalWords = signal(0);
  private readonly _chunksProcessed = signal(0);
  private readonly _sessionStart = signal<number | null>(null);
  private readonly _sessionTime = signal('00:00');

  readonly status = computed(() => this._status());
  readonly transcription = computed(() => this._liveWords().join(' '));
  readonly totalWords = computed(() => this._totalWords());
  readonly chunksProcessed = computed(() => this._chunksProcessed());
  readonly sessionTime = computed(() => this._sessionTime());

  private timer: any = null;
  private whisperTimeout: any = null;
  private wordTimer: any = null;

  connect = async () => {
    if (this.socket) return;

    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        audio: {
          sampleRate: 16000,
          channelCount: 1,
          echoCancellation: true,
          noiseSuppression: true
        }
      });

      this.socket = new WebSocket('ws://localhost:5224/ws/audio');
      this.socket.binaryType = 'arraybuffer';

      this.socket.onopen = () => {
        this._status.set({ websocket: 'connected', recording: 'recording', whisper: 'waiting' });
        this._sessionStart.set(Date.now());
        this.startSessionTimer();
        this.startAudioCapture(stream);
      };

      this.socket.onmessage = (event) => {
        const data = JSON.parse(event.data);
        const text = data.Transcription || data.text || event.data;

        console.log('[WebSocket] Received :', text);

        if (text.trim()) {
          this.setWhisperStatus('processing');

          const words = text.trim().split(/\s+/);
          this.enqueueWordsWithDelay(words, 275);

          this._totalWords.update(prev => prev + words.length);
          this._chunksProcessed.update(prev => prev + 1);

          if (this.whisperTimeout) clearTimeout(this.whisperTimeout);
          this.whisperTimeout = setTimeout(() => {
            this.setWhisperStatus('waiting');
          }, 9000);
        } else {
          console.warn('[WebSocket] Texto vac\u00edo recibido, estado no actualizado.');
        }
      };

      this.socket.onerror = () => {
        this._status.set({ websocket: 'error', recording: 'idle', whisper: 'inactive' });
      };

      this.socket.onclose = () => {
        this.cleanup();
      };
    } catch (err) {
      console.error('Mic access error :', err);
      alert('Permiso de micr\u00f3fono denegado o error al acceder.');
    }
  };

  disconnect = () => {
    this.socket?.close();
    this.cleanup();
  };

  clear = () => {
    this._liveWords.set([]);
    this._totalWords.set(0);
    this._chunksProcessed.set(0);
    this._sessionStart.set(Date.now());
  };

  private enqueueWordsWithDelay(words: string[], delay = 275) {
    if (this.wordTimer) clearInterval(this.wordTimer);
    let index = 0;

    this.wordTimer = setInterval(() => {
      if (index >= words.length) {
        clearInterval(this.wordTimer);
        this.wordTimer = null;
        return;
      }

      this._liveWords.update(prev => [...prev, words[index]]);
      index++;
    }, delay);
  }

  private startSessionTimer() {
    if (this.timer) clearInterval(this.timer);
    this.timer = setInterval(() => {
      const start = this._sessionStart();
      if (!start) return;

      const elapsed = Math.floor((Date.now() - start) / 1000);
      const min = Math.floor(elapsed / 60).toString().padStart(2, '0');
      const sec = (elapsed % 60).toString().padStart(2, '0');
      this._sessionTime.set(`${min}:${sec}`);
    }, 1000);
  }

  private setWhisperStatus(status: 'waiting' | 'processing' | 'inactive') {
    const current = this._status();
    this._status.set({ ...current, whisper: status });
  }

  private startAudioCapture(stream: MediaStream) {
    this.mediaStream = stream;
    this.audioContext = new AudioContext({ sampleRate: 16000 });

    const source = this.audioContext.createMediaStreamSource(stream);
    this.processor = this.audioContext.createScriptProcessor(8192, 1, 1);

    this.processor.onaudioprocess = (event) => {
      if (!this.socket || this.socket.readyState !== WebSocket.OPEN) return;

      const inputData = event.inputBuffer.getChannelData(0);
      const int16Array = new Int16Array(inputData.length);
      for (let i = 0; i < inputData.length; i++) {
        int16Array[i] = Math.max(-32768, Math.min(32767, inputData[i] * 32768));
      }

      this.socket.send(int16Array.buffer);
    };

    source.connect(this.processor);
    this.processor.connect(this.audioContext.destination);
  }

  private cleanup() {
    if (this.timer) clearInterval(this.timer);
    if (this.wordTimer) clearInterval(this.wordTimer);
    if (this.whisperTimeout) clearTimeout(this.whisperTimeout);

    this._status.set({ websocket: 'disconnected', recording: 'idle', whisper: 'inactive' });
    this.socket?.close();
    this.socket = null;

    this.processor?.disconnect();
    this.processor = null;

    this.audioContext?.close();
    this.audioContext = null;

    this.mediaStream?.getTracks().forEach(track => track.stop());
    this.mediaStream = null;
  }
}
