// Developer: heaplyn
// Date: 2026-08-15
// Summary: Builds a comprehensive map of the active project structure and logic.
//          Indexes all source files, resource definitions, and directory hierarchies.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HeaplitLauncher
{
    public static class ProjectMapManager
    {
        public static string BuildProjectTree(string rootPath, int maxDepth = 4)
        {
            if (!Directory.Exists(rootPath)) return "Project root not found.";

            var sb = new StringBuilder();
            sb.AppendLine($"# PROJECT MAP: {Path.GetFileName(rootPath)}");
            TraverseDirectory(rootPath, sb, 0, maxDepth);
            return sb.ToString();
        }

        private static void TraverseDirectory(string path, StringBuilder sb, int depth, int maxDepth)
        {
            if (depth > maxDepth) return;

            string indent = new string(' ', depth * 2);
            string[] ignoredDirs = { "bin", "obj", ".git", ".vs", "node_modules", "publish" };
            string[] importantExts = { ".cs", ".xaml", ".ts", ".js", ".json", ".md", ".lua" };

            try
            {
                var entries = Directory.GetFileSystemEntries(path);
                foreach (var entry in entries)
                {
                    string name = Path.GetFileName(entry);
                    if (ignoredDirs.Any(d => name.Equals(d, StringComparison.OrdinalIgnoreCase))) continue;

                    if (Directory.Exists(entry))
                    {
                        sb.AppendLine($"{indent}📁 {name}/");
                        TraverseDirectory(entry, sb, depth + 1, maxDepth);
                    }
                    else
                    {
                        string ext = Path.GetExtension(entry).ToLower();
                        if (importantExts.Contains(ext))
                        {
                            sb.AppendLine($"{indent}📄 {name}");
                        }
                    }
                }
            }
            catch { }
        }
    }
}
