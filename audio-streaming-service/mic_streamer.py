import asyncio
import sounddevice as sd
import websockets
import queue
import numpy as np

SAMPLE_RATE = 16000
CHUNK_DURATION = 0.5  # seconds
CHUNK_SIZE = int(SAMPLE_RATE * CHUNK_DURATION)

audio_queue = queue.Queue()

def callback(indata, frames, time, status):
    audio_queue.put(indata.copy())  # Save audio chunk in queue

async def send_audio():
    uri = "ws://localhost:9000/ws/audio"
    print(f"Connecting to WebSocket at {uri}...")

    async with websockets.connect(uri) as ws:
        print("Connected to WebSocket!")
        with sd.InputStream(samplerate=SAMPLE_RATE, channels=1, dtype="int16", callback=callback):
            print("🎧 Recording... Press Ctrl+C to stop.")
            while True:
                chunk = audio_queue.get()  # Block until a chunk is ready
                await ws.send(chunk.tobytes())  # Send over WebSocket

asyncio.run(send_audio())
