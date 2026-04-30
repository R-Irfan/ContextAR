# 🎮 ContextAR – Muse

> *An adaptive XR museum companion that intelligently adjusts content depth using real-world context.*

---

## 📌 Overview

**ContextAR – Muse** is an AI-powered XR application designed to enhance museum visits through context-aware interaction. Instead of static descriptions, the system dynamically adapts how information is presented—ranging from minimal glanceable insights to full conversational AI—based on user attention and environmental conditions.

The experience is designed to behave like **AI-powered smart glasses**, understanding:

* What the user is looking at
* How long they are engaged
* The surrounding environment (noise, crowd)

---

## 🚀 Core Concept

ContextAR replaces traditional museum guides with an adaptive system:

* Walking by → No interruption
* Brief attention → Short contextual info
* Deep engagement → Full AI-driven experience

---

## 🧠 Key Features

### 🎯 Context-Aware UI Adaptation

* Driven by:

  * Gaze duration
  * Crowd density
  * Ambient noise
* Dynamically selects the most appropriate UI mode

---

### 🎤 Voice Interaction (Hands-Free)

* Speech-to-Text (STT) for user queries
* Text-to-Speech (TTS) for responses
* Supports seamless conversational flow

---

### 👁️ On-Device Perception

* Exhibit detection (on-device vision)
* Crowd detection
* Noise level estimation
* Gaze tracking

---

### 🤖 AI-Powered Knowledge System

* Retrieval-Augmented Generation (RAG)
* FAISS vector search for museum knowledge base
* GPT-4o for response generation
* Ensures accurate, context-aligned answers

---

### 🎨 Adaptive XR UI System

* UI scales to match real-world painting size
* Handles temporary loss of object tracking
* Optimized for passthrough XR environments

---

## 🏗️ System Architecture

### End-to-End Flow

```
Unity (Meta Quest)
 ├── Object Detection (on-device)
 ├── STT (user question)
 ├── Crowd / Noise / Gaze tracking
        ↓
POST /ask → Python AI Server
        ↓
Context Router
        ↓
RAG (Knowledge Base)
        ↓
GPT-4o (Answer Generation)
        ↓
{ mode, answer }
        ↓
Unity
 ├── TTS plays response
 └── UI adapts based on mode
```

---

## 🔄 Context Routing Logic

| Gaze Duration | Crowd   | Noise | Mode                |
| ------------- | ------- | ----- | ------------------- |
| < 5s          | Any     | Any   | NO_RESPONSE         |
| 5–15s         | Low     | Quiet | BRIEF_TEXT          |
| 5–15s         | Low     | Noisy | BRIEF_TEXT          |
| 5–15s         | Crowded | Any   | GLANCE_CARD         |
| > 15s         | Low     | Quiet | FULL_VOICE          |
| > 15s         | Low     | Noisy | FULL_VOICE          |
| > 15s         | Crowded | Any   | BRIEF_TEXT + Prompt |

---

## 🎨 UI Behavior Design

| Gaze Time | Environment   | UI Mode        | Description               |
| --------- | ------------- | -------------- | ------------------------- |
| < 5s      | Any           | No UI          | Clean passthrough         |
| 5–15s     | Quiet         | Brief Text     | Title + short description |
| 5–15s     | Noisy/Crowded | Glance Card    | Minimal info              |
| > 15s     | Quiet         | Full Menu      | Full AI interaction       |
| > 15s     | Noisy/Crowded | Brief + Prompt | Suggest quieter spot      |

---

## 🌐 Server Connection (Unity ↔ AI Backend)

Before using AI features, the app must connect to the local AI server.

### 🔌 Initial Setup

* User enters server IP address on first launch
* Example:

```
http://192.168.1.10:8000
```

---

### 💾 Persistence

* Stored using Unity **PlayerPrefs**
* Automatically reused on next launch
* Can be updated via settings UI

---

### 🔄 Connection Flow

1. User enters server IP
2. Unity stores it locally
3. Requests sent to:

```
{SERVER_IP}/ask
```

4. Server returns:

```
{ mode, answer }
```

5. Unity:

   * Plays response via TTS
   * Updates UI based on mode

---

### ⚠️ Validation Notes

* Must include `http://`
* Ensure correct port (default: 8000)
* Device and server must be on same network

---

### 🧪 Testing

* Test endpoint:

```
http://<IP>:8000/health
```

* Ensure server is running before launching app

---

## 🛠️ Tech Stack

### 🎮 Frontend (XR)

* Unity 6 (6000.3.13f or higher)
* Meta Quest XR SDK
* Meta AI Building Blocks
* Meta STT / TTS

---

### 🧠 Backend (AI Server)

* Python (FastAPI)
* GPT-4o / GPT-4o Vision
* OpenAI Embeddings
* FAISS

---

### 🎨 Tools

* Figma (UI/UX design)
* On-device object detection

---

## 📂 Project Structure

```
Assets/
|── ContextAR
│   |── Scripts/
│   ├── Prefabs/
│   ├── UI/
│   ├── Scenes/
|   |── Textures 
│
│── 
│── 
│── XR/
```

---

## ⚙️ Setup Instructions

### 1. Clone Repository

```bash
git clone https://github.com/your-username/contextar-muse.git
```

---

### 2. Open in Unity

* Open Unity Hub
* Add project folder
* Use Unity ≥ 6000.3.13f

---

### 3. Configure XR

* Enable XR Plugin Management
* Setup Meta Quest

---

### 4. Setup AI Server

Follow:
https://github.com/jeannineshiu/ContextAR-AI

---

### 5. Run Project

1. Start AI server
2. Launch Unity scene
3. Enter server IP
4. Begin interaction

---

## ⚠️ Known Limitations

* Requires stable local network
* Detection depends on lighting/visibility
* Crowd/noise estimation is heuristic-based

---

## 🔮 Future Improvements

* Fully on-device AI
* Multi-language support
* Personalized museum tours
* Cloud-based knowledge sync

---

## 🤝 Contribution

1. Fork the repository
2. Create a feature branch
3. Submit a pull request

---

## 📄 License

Specify your license (e.g., MIT)

---

## 📬 Contact

* Project: **ContextAR – Muse**
* Team: *Add team members*
* GitHub: *Add repository link*

---
