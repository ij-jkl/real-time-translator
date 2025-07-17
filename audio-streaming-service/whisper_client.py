import httpx
import os
import asyncio

async def send_to_whisper(audio_path):
    url = "http://localhost:8000/transcribe_from_audio"
    ext = os.path.splitext(audio_path)[1].replace(".", "")

    try:
        with open(audio_path, "rb") as f:
            files = {'audioFile': (f"audio.{ext}", f, f"audio/{ext}")}
            data = {
                'language': 'es',
                'initial_prompt': 'Esto es una conversación en español.'
            }
            async with httpx.AsyncClient() as client:
                response = await client.post(url, files=files, data=data)

        await asyncio.sleep(0.1)
        return response.json()

    finally:
        try:
            os.remove(audio_path)
        except PermissionError:
            print(f"⚠️ Could not delete temp file (still in use): {audio_path}")
