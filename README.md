# 🎮 ContextAR – Muse

> **An AI-powered XR museum companion that adapts to user attention and environment in real time.**

---

## 🏷️ Badges

![Unity](https://img.shields.io/badge/Engine-Unity-black?logo=unity)
![XR](https://img.shields.io/badge/Platform-XR%20%7C%20Meta%20Quest-blueviolet)
![AI](https://img.shields.io/badge/AI-GPT--4o%20%7C%20RAG-green)
![Hackathon](https://img.shields.io/badge/Built%20For-Hackathon-orange)
![Status](https://img.shields.io/badge/Status-Prototype-yellow)

---

## 🎥 Demo (Watch First)

[![Watch Demo](https://img.youtube.com/vi/uVUPJRyVCUM/0.jpg)](https://www.youtube.com/watch?v=uVUPJRyVCUM)

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
git clone https://github.com/your-username/contextar-muse.git
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

#### Steps:

```bash
# Clone AI server
git clone https://github.com/jeannineshiu/ContextAR-AI.git
cd ContextAR-AI
```

```bash
# Create virtual environment
python -m venv venv
```

```bash
# Activate environment
# Windows:
venv\Scripts\activate

# Mac/Linux:
source venv/bin/activate
```

```bash
# Install dependencies
pip install -r requirements.txt
```

---

#### 🔑 Configure Environment Variables

Create a `.env` file in the root:

```env
OPENAI_API_KEY=your_api_key_here
```

---

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

### 4. Connect Unity to Server

* Launch the Unity app
* Enter server IP:

```bash
http://<YOUR_PC_IP>:8000
```

* Ensure:

  * Same Wi-Fi network
  * Firewall allows connection

---

### 5. Run Full System

1. Start AI server
2. Launch Unity scene
3. Wear headset
4. Look at exhibit and interact

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

| Name      | Role         | Responsibilities              | Contact           |
| --------- | ------------ | ----------------------------- | ----------------- |
| Your Name | XR Developer | Unity, XR UI, Integration     | GitHub / LinkedIn |
| Member 2  | AI Engineer  | RAG, FastAPI, GPT integration | GitHub / LinkedIn |
| Member 3  | Designer     | UI/UX, Figma                  | Portfolio         |
| Member 4  | Product      | Concept, testing              | LinkedIn          |

---

## 📬 Contact

* GitHub: https://github.com/your-username
* Email: [your@email.com](mailto:your@email.com)

---

## 📄 License

MIT License

---
