from fastapi import FastAPI, WebSocket, WebSocketDisconnect
from fastapi.responses import JSONResponse
from whisper_client import send_to_whisper
import tempfile
import wave

app = FastAPI()

SAMPLE_RATE = 16000
BYTES_PER_SAMPLE = 2
MAX_SECONDS = 6
OVERLAP_SECONDS = 1
CHUNK_SIZE = SAMPLE_RATE * BYTES_PER_SAMPLE * MAX_SECONDS
OVERLAP_SIZE = SAMPLE_RATE * BYTES_PER_SAMPLE * OVERLAP_SECONDS

previous_text = ""

@app.websocket("/ws/audio")
async def websocket_audio_handler(websocket: WebSocket):
    await websocket.accept()
    print("WebSocket connection established.")

    buffer = bytearray()

    # We check so we dont have to reinitialize the previous_text variable every time, we check for repetead words when we end the chunk of audio
    global previous_text

    try:
        while True:
            data = await websocket.receive_bytes()
            buffer.extend(data)

            if len(buffer) >= CHUNK_SIZE:
                # Save chunk to temp WAV file
                with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as temp_audio_file:
                    with wave.open(temp_audio_file, 'wb') as wf:
                        wf.setnchannels(1)
                        wf.setsampwidth(BYTES_PER_SAMPLE)
                        wf.setframerate(SAMPLE_RATE)
                        wf.writeframes(buffer)

                # Send to Whisper microservice
                result = await send_to_whisper(temp_audio_file.name)
                full_text = result.get("Transcription", "").strip()

                # Only print the new portion that wasn't in the previous transcription (check repaeted words)
                if full_text.startswith(previous_text):
                    new_text = full_text[len(previous_text):].lstrip()
                else:
                    new_text = full_text  # fallback if no clear overlap

                if new_text:
                    print("Log : Transcription : ", new_text)
                    await websocket.send_json({"Transcription": new_text})

                previous_text = full_text
                buffer = buffer[-OVERLAP_SIZE:]

    except WebSocketDisconnect:
        print("WebSocket connection closed.")


@app.get("/healthz")
def healthz():
    return JSONResponse(content={
        "status": "ok",
        "service": "audio-streaming-service", 
        "sample_rate": SAMPLE_RATE,
        "max_seconds": MAX_SECONDS
    })

#Log info
@app.get("/status")
def status():
    return JSONResponse(content={
        "service": "audio-streaming-service",
        "version": "1.0.0",
        "sample_rate": SAMPLE_RATE,
        "max_seconds": MAX_SECONDS,
        "overlap_seconds": OVERLAP_SECONDS,
        "chunk_size": CHUNK_SIZE
    })


"""
Run with:
uvicorn main:app --host 0.0.0.0 --port 9000
"""
