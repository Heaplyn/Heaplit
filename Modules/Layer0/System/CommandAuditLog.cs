// Developer: heaplyn
// Date: 2026-09-02
// Summary: Append-only audit log of every command / tool the AI executes. Written to
//          Data/CommandAudit.log with a UTC timestamp so there is a durable record of what
//          Heaplit did on the machine (shell commands, file writes/edits, downloads, tool creation,
//          settings changes). Best-effort and thread-safe; never throws into callers.

using System;
using System.IO;

namespace HeaplitLauncher
{
    public static class CommandAuditLog
    {
        private static readonly object _lock = new();
        private static string LogPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "CommandAudit.log");

        /// <summary>Record one executed action. <paramref name="kind"/> e.g. "PS", "EDIT", "DL".</summary>
        public static void Log(string kind, string detail)
        {
            try
            {
                string line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z] {kind}: {Trim(detail)}";
                lock (_lock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                    File.AppendAllText(LogPath, line + Environment.NewLine);
                }
                try { DebugConsoleOverlay.Log("CMD-AUDIT", $"{kind}: {Trim(detail, 200)}"); } catch { }
            }
            catch { }
        }

        private static string Trim(string s, int max = 2000)
        {
            s = (s ?? "").Replace("\r", " ").Replace("\n", " \\n ");
            return s.Length > max ? s.Substring(0, max) + "…" : s;
        }
    }
}
