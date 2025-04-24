# Story Creator

## What is this?
A Creative - AI-powered - Adventure Creator. A NET-based application designed to create adventure stories using the power of AI.

## How to run as Docker container
- if you have Groq api key then Docker image:
   ```bash
   docker run -it -e AI_API_KEY="gsk-**yourkey**" ghcr.io/bajahaw/story-creator:latest
    ```
- else you can run the app without the AI API key:
   ```bash
   docker run -it ghcr.io/bajahaw/story-creator:latest
   ```