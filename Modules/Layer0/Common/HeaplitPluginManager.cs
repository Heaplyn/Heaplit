// Developer: heaplyn
// Date: 2026-08-16
// Summary: High-performance plugin loader.
//          Dynamically loads .dll files from the /Plugins folder and registers their capabilities.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace HeaplitLauncher
{
    public static class HeaplitPluginManager
    {
        private static readonly List<IHeaplitPlugin> _loadedPlugins = new List<IHeaplitPlugin>();
        public static IReadOnlyList<IHeaplitPlugin> LoadedPlugins => _loadedPlugins;

        public static void Initialize()
        {
            string pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            if (!Directory.Exists(pluginDir)) Directory.CreateDirectory(pluginDir);

            DebugConsoleOverlay.Log("Plugin-System", "Scanning for external modules in /Plugins...");

            foreach (string dll in Directory.GetFiles(pluginDir, "*.dll"))
            {
                try
                {
                    Assembly assembly = Assembly.LoadFrom(dll);
                    var pluginTypes = assembly.GetTypes()
                        .Where(t => typeof(IHeaplitPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var type in pluginTypes)
                    {
                        if (Activator.CreateInstance(type) is IHeaplitPlugin plugin)
                        {
                            plugin.OnInitialize();
                            _loadedPlugins.Add(plugin);
                            DebugConsoleOverlay.Log("Plugin-System", $"Successfully loaded: {plugin.PluginName} v{plugin.Version}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugConsoleOverlay.Log("Plugin-System", $"Failed to load {Path.GetFileName(dll)}: {ex.Message}");
                }
            }
        }

        public static void Shutdown()
        {
            foreach (var plugin in _loadedPlugins)
            {
                try { plugin.OnShutdown(); } catch { }
            }
            _loadedPlugins.Clear();
        }

        public static List<CommandDesc> GetAllPluginCommands()
        {
            var all = new List<CommandDesc>();
            foreach (var plugin in _loadedPlugins)
            {
                var cmds = plugin.GetPluginCommands();
                if (cmds != null) all.AddRange(cmds);
            }
            return all;
        }
    }
}
