// Developer: heaplyn
// Date: 2026-08-17
// Summary: Autonomous Interjection Service implementation.
//          Follows modularization rules and implements IAutonomousInterjectionService.

using System;
using System.Linq;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class AutonomousInterjectionManager : IAutonomousInterjectionService
    {
        private bool _isRunning = false;
        private DateTime _lastInterjection = DateTime.Now;
        private readonly Random _random = new Random();

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            EnvironmentalAudioAnalyzer.OnSoundDetected += (cat, conf) => {
                if (IsReady() && (cat == "Sigh" || cat == "Frustrated_Noise")) Trigger("You sound frustrated, Boss. Need a hand with the code?");
            };

            Task.Run(async () => {
                await Task.Delay(30000);
                while (_isRunning) {
                    try { await CheckProactiveAsync(); } catch { }
                    await AdaptiveSleeper.DelayAsync(_random.Next(180000, 300000));
                }
            });
        }

        public void Stop() => _isRunning = false;

        private async Task CheckProactiveAsync()
        {
            if (!IsReady()) return;
            if (NativeMethods.GetIdleTime() > 600000) return; // Silent if idle > 10m

            var recent = ActionJournalManager.GetRecentActions(5);
            if (recent.Count(a => a.ActionType == "BUILD_ERROR") >= 3) {
                await Trigger("Third build error in a row. Maybe we should check the references?");
            }
        }

        private bool IsReady() => CoreRegistry.Settings.Current.IS_AUTONOMOUS_MODE_ENABLED &&
                                 CoreRegistry.Settings.Current.IS_VOICE_MODE_ACTIVE &&
                                 (DateTime.Now - _lastInterjection).TotalMinutes >= 15 &&
                                 !CoreRegistry.Tts.IsSpeaking;

        private async Task Trigger(string fallback)
        {
            _lastInterjection = DateTime.Now;
            try {
                string prompt = $"Reason: {fallback}\nGenerate wity 1-sentence remark.";
                string res = await CoreRegistry.Llm.AskAsync(prompt);
                CoreRegistry.Tts.Speak(res);
                TextOverlay.Show("🤖 Heaplit: " + res, 5000);
            } catch { CoreRegistry.Tts.Speak(fallback); }
        }

        public static void StartGlobal() => CoreRegistry.Autonomous.Start();
    }
}
