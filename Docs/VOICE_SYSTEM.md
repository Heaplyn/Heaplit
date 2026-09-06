# Voice Activation & Speech Systems

Heaplit features a multi-tiered voice activation pipeline designed for 100% privacy and local-first execution.

## 1. Activation Pipeline
The "Wake Word" engine runs in three stages to balance battery life and accuracy:
1.  **Acoustic Pulse (Fast)**: A low-level algorithm monitors microphone RMS and frequency. It looks for the specific syllable cadence of "Heaplit" (Sibilant 's' at the end).
2.  **Vosk Neural Pass (Local)**: If the pulse matches, the **Vosk Neural Engine** transcribes the last 3 seconds of audio. This is done entirely on your CPU.
3.  **Gemini Triage (Cloud - Optional)**: If Vosk is unsure, a 1-second audio clip is sent to Gemini for a "YES/NO" confirmation. This only happens as a fallback to prevent false triggers from the TV or background noise.

## 2. Echo Rejection
To prevent Heaplit from "talking to himself," the system uses **Semantic Echo Rejection**:
- Every time Heaplit speaks, the text is stored in `LastAiSpokenText`.
- When the mic hears a command, it compares the transcription to the last spoken text using `SearchUtil.GetSimilarity`.
- If the similarity is > 80%, the command is discarded as an echo.

## 3. One-Shot Commands
Heaplit supports "One-Shot" intent parsing. You don't have to wait for him to say "Yes?":
- *"Hey Heaplit, what's the weather?"*
- The system automatically strips the wake word and routes the remaining query directly to the command parser.

## 4. Voice Training
The **Voice Studio** (`/voice`) allows you to:
- Enroll your specific voiceprint for **Speaker Biometrics**.
- Teach Heaplit new environmental sounds (like a knock or a clap).
- Adjust the **Mic Noise Gate** to filter out fan noise or keyboard clicking.
