// Developer: heaplyn
// Date: 2026-08-14
// Summary: Main application entry point.

using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Linq;

namespace HeaplitLauncher
{
    public partial class App : Application
    {
        private NotifyIcon? _notifyIcon;
        private MainWindow? _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;
            SelfHealingManager.Initialize();
            KillPreviousHeaplitInstances();
            WpfScrollHelper.InitializeGlobalScrollFix();
            base.OnStartup(e);

            this.DispatcherUnhandledException += (s, ev) =>
            {
                // Do NOT swallow silently. Surface the exception (console + crash log) so failures
                // are diagnosable. Kept non-fatal (Handled = true) so a single UI fault doesn't kill
                // the HUD, but it is now logged loudly instead of hidden.
                try
                {
                    string details = $"{ev.Exception.GetType().Name}: {ev.Exception.Message}\n{ev.Exception.StackTrace}";
                    DebugConsoleOverlay.Log("UI-EXCEPTION", details);
                    System.Diagnostics.Debug.WriteLine("[DISPATCHER EXCEPTION] " + details);
                    try
                    {
                        System.IO.File.AppendAllText(
                            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "heaplit_debug.log"),
                            $"[{DateTime.Now:u}] DISPATCHER EXCEPTION: {details}\n");
                    }
                    catch { }
                }
                catch { }
                ev.Handled = true;
            };

            RunBootSequence();
        }

        private async void RunBootSequence()
        {
            LoadingWindow? loadingWindow = null;
            try
            {
                // 1. Show Loading Screen Immediately
                loadingWindow = new LoadingWindow();
                loadingWindow.Show();

                // Boot profiler: log each phase's wall-clock to heaplit_debug.log so slow startup
                // is diagnosable instead of guessed. (Search the log for "BOOT-PROFILE".)
                var bootSw = System.Diagnostics.Stopwatch.StartNew();
                long lastMs = 0;
                void BootPhase(string name)
                {
                    long now = bootSw.ElapsedMilliseconds;
                    try { System.IO.File.AppendAllText(
                        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "heaplit_debug.log"),
                        $"[{DateTime.Now:HH:mm:ss.fff}] BOOT-PROFILE {name}: +{now - lastMs}ms (total {now}ms)\n"); } catch { }
                    lastMs = now;
                }

                // 2. Fast Initialization
                loadingWindow.UpdateStatus("Initializing Core Services...", 10);
                CoreRegistry.InitializeAll();
                BootPhase("CoreRegistry.InitializeAll");

                loadingWindow.UpdateStatus("Applying visual interface...", 25);

                ThemeManager.ApplyTheme(CoreRegistry.Data.Settings.Current.THEME);
                BootPhase("ApplyTheme");

                // 3. Fire background service initializations in parallel
                loadingWindow.UpdateStatus("Starting engines...", 50);

                // Apply the saved Ring0 wait-scheduler coalescing floor, then warm the adaptive
                // throttle sampler + shared wait scheduler so background loops read live CPU/RAM
                // pressure and share one wait thread from their very first iteration.
                Ring0WaitScheduler.MinTimeoutMs = CoreRegistry.Data.Settings.Current.RING0_MIN_TIMEOUT_MS;
                AdaptiveSleeper.Start();

                var depTask = Task.Run(() => { try { EnsureDependenciesAsync().GetAwaiter().GetResult(); } catch { } });
                var cmdTask = Task.Run(() => { try { CommandParser.Initialize(); } catch { } });

                // Start opt-in network services if configured
                var s = CoreRegistry.Data.Settings.Current;
                if (s != null && s.P2P_SERVER_ENABLED)
                {
                    Task.Run(() => { try { MobileBridgeServer.Start(s.MOBILE_PORT); } catch { } });
                }

                // Await essential core setup (directories & command index) before showing UI
                loadingWindow.UpdateStatus("Calibrating core services...", 75);
                await Task.WhenAll(depTask, cmdTask);
                BootPhase("Await deps+CommandParser");
                loadingWindow.UpdateStatus("System Online.", 100);

                _mainWindow = new MainWindow();
                this.MainWindow = _mainWindow;

                _mainWindow.Show();
                _mainWindow.ShowHUD();
                BootPhase("MainWindow shown (HUD ready)");

                loadingWindow.Close();

                // Now that the HUD is visible, start the heavy initializers (app scan, voice, memory,
                // backup sync, AI discovery) in the background so they don't slow first paint.
                CoreRegistry.InitializeDeferred();

                // 5. System Tray
                try {
                    _notifyIcon = new NotifyIcon { Icon = SystemIcons.Application, Visible = true, Text = "Heaplit HUD" };
                    var contextMenu = new ContextMenuStrip();
                    contextMenu.Items.Add("Show Launcher", null, (s, ev) => _mainWindow?.Dispatcher.Invoke(() => _mainWindow.ShowHUD()));
                    contextMenu.Items.Add(new ToolStripSeparator());
                    contextMenu.Items.Add("Exit", null, (s, ev) => Application.Current.Shutdown());
                    _notifyIcon.ContextMenuStrip = contextMenu;
                    _notifyIcon.DoubleClick += (s, ev) => _mainWindow?.Dispatcher.Invoke(() => _mainWindow.ShowHUD());
                } catch { }

                // 6. Restore Overlays
                _ = Task.Delay(500).ContinueWith(_ => {
                    Application.Current.Dispatcher.Invoke(() => {
                        try { WindowPositionManager.RestoreOpenOverlays(); } catch { }
                    });
                });
            }
            catch (Exception ex)
            {
                loadingWindow?.Close();
                MessageBox.Show("Heaplit failed to boot: " + ex.Message);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Runtime dependency health-check
        //  SharpCompress is a NuGet reference so it's bundled into the EXE on
        //  every build. This is a safety net for corrupted or stripped deploys.
        // ─────────────────────────────────────────────────────────────────────
        private static async Task EnsureDependenciesAsync()
        {
            // Required assemblies: name → NuGet package id
            var required = new[] { ("SharpCompress", "SharpCompress") };

            bool allPresent = true;
            foreach (var (asmName, _) in required)
            {
                try { Assembly.Load(new AssemblyName(asmName)); }
                catch { allPresent = false; break; }
            }

            if (allPresent) return; // Fast-path: everything is fine

            // Slow-path: try to restore via dotnet CLI
            try
            {
                var exeDir  = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) ?? "";
                var csproj  = Directory.GetFiles(exeDir, "*.csproj", SearchOption.TopDirectoryOnly)
                               .FirstOrDefault() ?? "";

                if (string.IsNullOrEmpty(csproj)) return; // Can't find project – give up silently

                var psi = new ProcessStartInfo("dotnet", $"restore \"{csproj}\"")
                {
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                    await proc.WaitForExitAsync(new System.Threading.CancellationTokenSource(60_000).Token)
                              .ConfigureAwait(false);
            }
            catch { /* Swallow: app continues even without the restore */ }
        }

        private static void KillPreviousHeaplitInstances()
        {
            try
            {
                int currentId = System.Diagnostics.Process.GetCurrentProcess().Id;
                foreach (var proc in System.Diagnostics.Process.GetProcessesByName("HeaplitLauncher"))
                {
                    if (proc.Id != currentId) { try { proc.Kill(); } catch { } }
                }
            }
            catch { }
        }

        

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // Graceful shutdown of active background services
                VoiceActivationManager.Stop();
                MobileBridgeServer.Stop();
                MemoryManager.Stop();

                _notifyIcon?.Dispose();
            }
            catch { }
            base.OnExit(e);
        }
    }
}
