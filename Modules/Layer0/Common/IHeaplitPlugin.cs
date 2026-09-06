// Developer: heaplyn
// Date: 2026-08-16
// Summary: Core Interface for Heaplit dynamic plugins.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher
{
    public interface IHeaplitPlugin
    {
        string PluginName { get; }
        string Description { get; }
        string Author { get; }
        Version Version { get; }

        /// <summary>
        /// Called when the plugin is first loaded.
        /// </summary>
        void OnInitialize();

        /// <summary>
        /// Called when Heaplit is shutting down.
        /// </summary>
        void OnShutdown();

        /// <summary>
        /// Allows the plugin to register custom command keywords.
        /// </summary>
        IEnumerable<CommandDesc> GetPluginCommands();
    }
}
