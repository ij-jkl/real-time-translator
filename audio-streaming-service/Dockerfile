﻿FROM python:3.11-slim
WORKDIR /app

COPY . .
RUN pip install --no-cache-dir fastapi uvicorn httpx

EXPOSE 9000
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "9000"]
