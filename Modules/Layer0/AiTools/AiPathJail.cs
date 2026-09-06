// Developer: heaplyn
// Date: 2026-08-31
// Summary: SECURITY - confines model-driven file tools (@rf/@wf/@rf_b/@wf_b/@ls) to a
//          workspace root so the model cannot read or write arbitrary paths on disk
//          (e.g. C:\Windows, the Startup folder, browser cookies, SSH keys).

using System;
using System.IO;

namespace HeaplitLauncher.AiTools
{
    public static class AiPathJail
    {
        // Workspace root the model is allowed to touch. Defaults to the Heaplit install dir.
        public static string Root { get; } =
            Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);

        /// <summary>
        /// Resolves a model-supplied path against the workspace and rejects anything that
        /// escapes the root (via absolute paths, .. traversal, symlinks, etc.).
        /// </summary>
        public static bool TryResolve(string requested, out string fullPath, out string error)
        {
            fullPath = string.Empty;
            error = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(requested))
                { error = "[ERROR: empty path blocked]\n"; return false; }

                string combined = Path.IsPathRooted(requested)
                    ? requested
                    : Path.Combine(Root, requested);
                string full = Path.GetFullPath(combined);

                // Agent Mode (ENABLE_PC_CONTROL) grants full filesystem access. With it off, file
                // tools stay confined to the app workspace.
                if (CoreRegistry.Data.Settings.Current.ENABLE_PC_CONTROL)
                {
                    fullPath = full;
                    return true;
                }

                string rootWithSep = Root.EndsWith(Path.DirectorySeparatorChar)
                    ? Root : Root + Path.DirectorySeparatorChar;

                if (!string.Equals(full, Root, StringComparison.OrdinalIgnoreCase) &&
                    !full.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
                {
                    error = $"[ERROR: path '{requested}' is outside the workspace. Enable Agent Mode for full file access.]\n";
                    return false;
                }

                fullPath = full;
                return true;
            }
            catch (Exception ex)
            {
                error = $"[ERROR: invalid path '{requested}': {ex.Message}]\n";
                return false;
            }
        }
    }
}
