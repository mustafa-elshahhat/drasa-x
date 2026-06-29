# School AI RAG Platform

An AI-powered educational platform that allows students to ask questions
from their official school textbooks using Retrieval-Augmented Generation (RAG).

## Tech Stack
- Backend: FastAPI
- LLM: Groq (LLaMA 3.1)
- Vector DB: Chroma
- Embeddings: sentence-transformers
- Frontend: React

## Features
- Grade & subject aware question answering
- Context-only answers (no hallucination)
- Quiz generation from study material


Place trained models inside:
app/behavior/models/

Expected files:
- student_cluster_model.pkl
- student_scaler.pkl