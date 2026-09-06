// Developer: heaplyn
// Date: 2026-08-18
// Summary: Command Metadata Descriptor for Heaplit Command System.
//          Added default constructor to support object initializer syntax.

using System;

namespace HeaplitLauncher
{
    public class CommandDesc
    {
        public string COMMAND_NAME { get; set; } = string.Empty;
        public string COMMAND_DESCRIPTION { get; set; } = string.Empty;
        public string COMMAND_EXAMPLE { get; set; } = string.Empty;
        public bool SHOW { get; set; } = true;

        public CommandDesc() { }

        public CommandDesc(string Name, string Description, string Example)
        {
            COMMAND_NAME = Name;
            COMMAND_DESCRIPTION = Description;
            COMMAND_EXAMPLE = Example;
        }

        public CommandDesc(string Name, string Description, string Example, bool ShowParam)
        {
            COMMAND_NAME = Name;
            COMMAND_DESCRIPTION = Description;
            COMMAND_EXAMPLE = Example;
            SHOW = ShowParam;
        }
    }
}
