// Developer: heaplyn
// Date: 2026-08-17
// Summary: Interface for Text-to-Speech operations.

namespace HeaplitLauncher
{
    public interface ITtsService
    {
        void Speak(string text);
        void Stop();
        bool IsSpeaking { get; }
    }
}
