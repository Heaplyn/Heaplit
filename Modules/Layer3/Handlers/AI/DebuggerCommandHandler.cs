// Developer: heaplyn
// Date: 2026-09-03
// Summary: Handles CLI commands for system debugging, diagnostics, PC optimization, and process inspection.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HeaplitLauncher
{
    public class DebuggerCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "debug", "inspect", "diagnostics", "perf", "monitor", "console", "taskmgr", "task manager", "processes", "optimizer", "pc optimizer", "system monitor");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            string q = query.Trim().ToLower();

            if (q.Contains("console") || q.Contains("logs"))
            {
                suggestions.Add(new CommandResult
                {
                    TITLE = "📝 View AI Execution Logs (Debug Console)",
                    DESCRIPTION = "Show the raw internal monologue and tool execution steps of the agent",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "debug", "inspect", "diagnostics", "perf", "monitor", "console") + 9.5 * 0.01),
                    EXECUTE = () => DebugConsoleOverlay.ShowOverlay()
                });
            }

            suggestions.Add(new CommandResult
            {
                TITLE = "🛠️ Open Heaplit Live System Debugger & PC Optimizer",
                DESCRIPTION = "Real-time telemetry, RAM compactor, junk purger, zombie task killer & process manager",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "debug", "inspect", "diagnostics", "perf", "monitor", "optimizer", "taskmgr", "system monitor") + 9.8 * 0.01),
                EXECUTE = () => SystemMonitorOverlay.ShowOverlay()
            });

            suggestions.Add(new CommandResult
            {
                TITLE = "🚀 Run Max PC Optimization",
                DESCRIPTION = "Execute algorithmic deep RAM compaction, junk purge, DNS flush, and kill dead tasks",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "optimizer", "optimize", "perf", "clean", "speedup", "ram") + 9.2 * 0.01),
                EXECUTE = () => SystemMonitorOverlay.ShowOverlay()
            });

            suggestions.Add(new CommandResult
            {
                TITLE = "📊 Open Performance Monitor",
                DESCRIPTION = "Track CPU, RAM, and Disk IO in a floating HUD panel",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "debug", "inspect", "diagnostics", "perf", "monitor", "console") + 8.0 * 0.01),
                EXECUTE = () => SystemSpecsOverlay.ShowOverlay()
            });

            suggestions.Add(new CommandResult
            {
                TITLE = "🔍 Inspect Active Processes",
                DESCRIPTION = "View and manage all running background tasks",
                SIMILARITY = (SearchUtil.BestSimilarity(query, "debug", "inspect", "diagnostics", "perf", "monitor", "console", "taskmgr") + 7.5 * 0.01),
                EXECUTE = () => SystemMonitorOverlay.ShowOverlay()
            });

            return suggestions;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("debug", "Open system debugging & PC optimizer suite", "debug"),
                new CommandDesc("optimizer", "Run autonomic PC optimizer", "optimizer"),
                new CommandDesc("debug console", "View AI internal execution logs", "debug console"),
                new CommandDesc("monitor", "Monitor system performance and processes", "monitor"),
                new CommandDesc("inspect", "Inspect active processes", "inspect"),
                new CommandDesc("diagnostics", "Run system diagnostics", "diagnostics"),
                new CommandDesc("perf", "Show performance statistics", "perf")
            };
        }
    }
}
