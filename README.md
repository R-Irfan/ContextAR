# ContextAR – Muse

> **An AI-powered XR museum companion that adapts to user attention and environment in real time.**

---

## 🎥 Demo (Watch First)

[![Watch Demo](https://img.youtube.com/vi/uVUPJRyVCUM/0.jpg)](https://www.youtube.com/watch?v=uVUPJRyVCUM)

---

## 🏷️ Badges

![Unity](https://img.shields.io/badge/Engine-Unity-black?logo=unity)
![XR](https://img.shields.io/badge/Platform-XR%20%7C%20Meta%20Quest-blueviolet)
![AI](https://img.shields.io/badge/AI-GPT--4o%20%7C%20RAG-green)
![Status](https://img.shields.io/badge/Status-Prototype-yellow)

---

## 🚨 The Problem

Museum experiences today are:

* Static and one-size-fits-all
* Overwhelming or under-informative
* Not adaptive to user attention or environment

Visitors either **miss context** or **get overloaded**.

---

## 💡 Our Solution

**ContextAR transforms museum visits into adaptive, intelligent experiences.**

It behaves like **AI-powered smart glasses** that:

* Understand what you're looking at
* Measure how engaged you are
* Sense your surroundings

And then **adjust the experience automatically**.

---

## ⚡ Key Innovation

> **Context-aware content delivery in XR**

The system decides:

* When to stay silent
* When to show minimal info
* When to enable full AI conversation

---

## 🧠 How It Works

```plaintext
User (XR Headset)
   ↓
On-device perception
(exhibit + gaze + crowd + noise)
   ↓
POST /ask → AI Server
   ↓
Context Router
   ↓
RAG + GPT-4o
   ↓
{ mode, answer }
   ↓
Adaptive UI + Voice Output
```

---

## 🎯 Smart Adaptation

| Situation        | System Behavior              |
| ---------------- | ---------------------------- |
| Passing by (<5s) | No interruption              |
| Brief glance     | Short contextual info        |
| Deep engagement  | Full AI conversation         |
| Crowded/noisy    | Minimal or guided experience |

---

## 🎨 Experience Modes

* **No UI** → Clean real-world view
* **Glance Card** → 1-line info
* **Brief Text** → Short description
* **Full Experience** → AI + audio + exploration

---

## 🌐 Server Connection (Unity ↔ AI)

* User enters **local AI server IP** on first launch
* Stored using PlayerPrefs
* Example:

```bash
http://192.168.1.10:8000
```

---

## 🛠️ Tech Stack

**XR Layer**

* Unity 6
* Meta Quest SDK
* Meta STT / TTS

**AI Layer**

* FastAPI (Python)
* GPT-4o + Vision
* RAG + FAISS
* OpenAI Embeddings

---

## ⚙️ Setup Instructions

### 1. Clone Repository

```bash
git clone https://github.com/R-Irfan/ContextAR.git
```

---

### 2. Unity Setup

* Open project in Unity Hub
* Use Unity version ≥ **6000.3.13f**
* Enable XR Plugin Management
* Configure Meta Quest

---

### 3. AI Server Setup

The AI backend powers context-aware responses using RAG + GPT.

👉 Repository: https://github.com/jeannineshiu/ContextAR-AI

Follow instructions to setup the server

#### ▶️ Run Server

```bash
uvicorn server:app --host 0.0.0.0 --port 8000
```

---

#### ✅ Test Server

Open in browser:

```bash
http://localhost:8000/health
```

---

* Ensure:

  * Same Wi-Fi network
  * Firewall allows connection

---

### 5. Run Full System

1. Start AI server
2. Wear headset
3. Launch the app
4. Enter Server IP address
5. Look at exhibit and interact
6. Ask questions and listen the context aware AI responses

---

## 🚀 Why This Matters

* Makes museums **interactive without friction**
* Reduces cognitive overload
* Enables **personalized learning at scale**
* Bridges physical + digital storytelling

---

## 🧪 What Makes It Unique

* Real-time **context routing engine**
* Combines **perception + AI + XR UI adaptation**
* Designed for **AI glasses future**

---

## 🔮 Future Scope

* Fully on-device AI
* Multi-language support
* Personalized tours
* Cloud museum integration

---

## 👥 Team

| Name            | Role                | Responsibilities              
| ---------       | ------------        | ----------------------------- 
| Adrian Leon     | PHD Student XR, BCI | Unity, XR, AI, UI, Crowd, noise detection, Project Management     
| Jia-Yin Shiu    | AI Engineer         | RAG, FastAPI, GPT integration
| Yuxuan Tu       | Designer            | UI/UX, Figma
| Irfan R         | XR Developer        | Unity, XR, Voice Services, UI 

---

## 📄 License

MIT License

---
