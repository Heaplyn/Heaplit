# AUDIO & PERCEPTION SUBSYSTEM (`Modules/Layer0/`)

## OVERVIEW
Heaplit incorporates an audio processing and perception engine that enables hands-free voice wake-up, acoustic environment classification, speech tone sentiment analysis, and text-to-speech synthesis.

---

## CORE MODULE DETAILS

### 1. `LocalWakeWordDetector.cs`
- Continuously listens to default audio input (microphone stream).
- Performs lightweight pattern matching against target wake phrases (e.g. "Hey Heaplit").
- Fires `OnWakeWordDetected` and `OnVoiceCommandRecognized` events on the dispatcher to wake up Heaplit overlays.

### 2. `AudioFeatureExtractor.cs` & `AcousticMlClassifier.cs`
- **`AudioFeatureExtractor`**: Transforms raw PCM audio samples into frequency spectrum representations using Fast Fourier Transform (FFT). Extracts MFCC (Mel-Frequency Cepstral Coefficients) and spectral centroid features.
- **`AcousticMlClassifier`**: Classifies environment sounds into categories (keyboard typing, speech, ambient noise, silence) to dynamically adjust audio ducking and voice recognition thresholds.

### 3. `EmotionalContextManager.cs` & `EnvironmentalAudioAnalyzer.cs`
- **`EmotionalContextManager`**: Analyzes pitch variations, cadence, and vocal energy to estimate developer sentiment (e.g. frustration, focus, calm), storing context scores for LLM prompt personalization.
- **`EnvironmentalAudioAnalyzer`**: Monitors ambient noise levels and triggers quiet-mode adjustments when background noise spikes.

### 4. `VoiceStudioManager.cs` & `TtsVoiceLibraryManager.cs`
- Controls local Text-to-Speech (TTS) engines, custom voice model libraries, pitch/rate controls, and audio output rendering.
