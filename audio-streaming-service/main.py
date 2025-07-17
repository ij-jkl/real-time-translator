from fastapi import FastAPI, WebSocket, WebSocketDisconnect
import tempfile
from whisper_client import send_to_whisper
import wave

app = FastAPI()

MAX_SECONDS = 8  # accumulate 8 seconds before transcribing
SAMPLE_RATE = 16000
BYTES_PER_SECOND = SAMPLE_RATE * 2  # 2 bytes per sample (int16)

@app.websocket("/ws/audio")
async def websocket_audio_handler(websocket: WebSocket):
    await websocket.accept()
    print("WebSocket connection established.")

    buffer = bytearray()

    try:
        while True:
            data = await websocket.receive_bytes()
            buffer.extend(data)

            # Transcribe every ~8 seconds
            if len(buffer) >= BYTES_PER_SECOND * MAX_SECONDS:
                with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as temp_audio_file:
                    with wave.open(temp_audio_file, 'wb') as wf:
                        wf.setnchannels(1)
                        wf.setsampwidth(2)
                        wf.setframerate(SAMPLE_RATE)
                        wf.writeframes(buffer)

                # Transcribe once full segment is ready
                audioChunk = await send_to_whisper(temp_audio_file.name)
                print("📝 Transcription:", audioChunk.get("Transcription"))

                buffer.clear()

    except WebSocketDisconnect:
        print("WebSocket connection closed.")




""" uvicorn main:app --host 0.0.0.0 --port 9000 """
""" pip install -r requirements.txt """
