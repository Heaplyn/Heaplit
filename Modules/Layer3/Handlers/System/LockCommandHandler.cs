// Developer: heaplyn
// Date: 2026-08-08
// Summary: Handles security commands to lock the Windows session workstation using User32 DLL calls.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher
{
    public class LockCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.IsClose(query.Trim(), "lock");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            double similarity = SearchUtil.GetSimilarity(query.Trim(), "lock");
            suggestions.Add(new CommandResult
            {
                TITLE = "Lock Workstation",
                DESCRIPTION = "Secure the Windows session immediately",
                EXECUTE = () => NativeMethods.LockWorkStation(),
                SIMILARITY = similarity
            });
            return suggestions;
        }
    }
}
