import os
import time

import torch
import whisper

from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.responses import JSONResponse

import webbrowser
import threading
import tempfile

def open_browser():
    webbrowser.open_new("http://127.0.0.1:8000/")

# Time to load the swagger UI in the browser, larger time if larger model this is to avoid reloading manually the browser. Else remove threading.Timer
threading.Timer(2, open_browser).start()

app = FastAPI(docs_url="/")

# Load the Whisper model using GPU as default, if not we use the CPU
device = "cuda" if torch.cuda.is_available() else "cpu"
print("Currently using " + device + " as device.")

# Add check later to see if we are translating from en audio or not
model = whisper.load_model("large", device = device)

@app.get("/healthz")
def healthz():
    return JSONResponse(content={"status": "ok"})

@app.post("/transcribe_from_audio")
async def transcribe_from_audio(audioFile : UploadFile = File(...)):

    # We get the extension of the file to check if it is allowed, and the temp path to read the binary later.
    ext = audioFile.filename.split(".")[-1].lower()

    # To avoid injection of .exe files or large files that can crash the service.
    ALLOWED_EXTENSIONS = {"mp3", "wav", "m4a", "webm", "flac", "ogg"}

    if ext not in ALLOWED_EXTENSIONS:
        raise HTTPException(
            status_code=400,
            detail=f"Unsupported/Not allowed file format '.{ext}'. Please upload one of the following extensions : {', '.join(ALLOWED_EXTENSIONS)}"
        )

    try:
        with tempfile.NamedTemporaryFile(suffix=f".{ext}", delete=False) as tmp:
            tmp.write(await audioFile.read())
            tmp.flush()
            temp_path = tmp.name

        # Transcribe the audio file using Whisper
        start_time = time.time()
        transcription = model.transcribe(temp_path, language="es") # Change "en" to the desired language code if needed
        end_time = time.time()

        total_time = end_time - start_time

        print(f"Transcription: {transcription['text']}")
        print(f"Took {round(total_time, 2)} seconds")

        return {
            "Transcription": transcription["text"],
            "Language": transcription["language"],
            "TranscriptionTimeSeconds": round(total_time, 2),
            "Segments": transcription["segments"]
        }

    finally:
        if os.path.exists(temp_path):
            os.remove(temp_path)


"""Instalation of the dependencies can be done using the following command : pip install fastapi uvicorn openai-whisper torch"""
""" uvicorn main:app --host 0.0.0.0 --port 8000 """
# Todo : Validate that the audio file is really of .mp3 extension, and not a .exe file with a .mp3 extension.
