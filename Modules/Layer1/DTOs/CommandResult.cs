// Developer: heaplyn
// Date: 2026-08-08
// Summary: Model defining suggestion outcomes with visual metadata (Title, Description), similarity score, and executable action.

using System;

namespace HeaplitLauncher
{
    public class CommandResult
    {
        public string TITLE { get; set; } = string.Empty;
        public string DESCRIPTION { get; set; } = string.Empty;
        public Action? EXECUTE { get; set; }
        public double SIMILARITY { get; set; } = 0.0;
    }
}
