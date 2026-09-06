// Developer: heaplyn
// Date: 2026-08-17
// Summary: Interface for voice activation and wake word detection.

using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public interface IVoiceActivationService
    {
        void Start();
        void Stop();
        bool IsListening { get; }
        void SetSensitivity(double level);
        Task EnrollVoiceAsync(string name);
        Task LearnEnvironmentalSoundAsync(string category);
        Task SaveBackgroundAudioTokenAsync(string text);
        void LearnPhrase(string phrase);
    }
}
