# Story Creator

## What is this?
Story Creator is a .NET-based application designed to help users create and manage stories. It is packaged as a Docker container for easy deployment and usage.

## How to run as Docker container
- if you have Groq api key then Docker image:
   ```bash
   docker run -it -e AI_API_KEY="gsk-**yourkey**" ghcr.io/bajahaw/story-creator:latest
    ```
- else you can run the app without the AI API key:
   ```bash
   docker run -it ghcr.io/bajahaw/story-creator:latest
   ```