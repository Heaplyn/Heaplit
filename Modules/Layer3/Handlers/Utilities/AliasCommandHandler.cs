// Developer: heaplyn
// Date: 2026-08-09
// Summary: Handles CLI commands to manage custom command shortcuts/aliases persistent in settings.

using System;
using System.Collections.Generic;

namespace HeaplitLauncher.Modules.Layer3.Handlers
{
    public class AliasCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "alias", "unalias");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            query = query.Trim();

            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return suggestions;

            string cmd = parts[0].ToLower();
            double similarity = SearchUtil.BestSimilarity(query, "alias", "unalias"); // High priority match

            if (cmd == "alias")
            {
                if (parts.Length > 2)
                {
                    string shortcut = parts[1].ToLower();
                    // Extract expansion (everything after the shortcut keyword)
                    string expansion = query[(query.IndexOf(parts[1]) + parts[1].Length)..].Trim();

                    suggestions.Add(new CommandResult
                    {
                        TITLE = $"Create Alias: {shortcut} -> \"{expansion}\"",
                        DESCRIPTION = "Save this custom command shortcut",
                        EXECUTE = () => SetAlias(shortcut, expansion),
                        SIMILARITY = similarity
                    });
                }
                else if (parts.Length > 1)
                {
                    string shortcut = parts[1].ToLower();
                    suggestions.Add(new CommandResult
                    {
                        TITLE = $"Create Alias for '{shortcut}'...",
                        DESCRIPTION = $"Type the target command (e.g. 'alias {shortcut} empty')",
                        EXECUTE = null,
                        SIMILARITY = similarity
                    });
                }
                else
                {
                    // Show all configured aliases
                    var currentAliases = SettingsManager.Current.ALIASES;
                    if (currentAliases.Count > 0)
                    {
                        foreach (var alias in currentAliases)
                        {
                            suggestions.Add(new CommandResult
                            {
                                TITLE = $"Alias: {alias.Key} -> \"{alias.Value}\"",
                                DESCRIPTION = $"Type 'unalias {alias.Key}' to remove this shortcut",
                                EXECUTE = null,
                                SIMILARITY = similarity - 0.1
                            });
                        }
                    }

                    suggestions.Add(new CommandResult
                    {
                        TITLE = "Create Alias...",
                        DESCRIPTION = "Type 'alias <shortcut> <command>' (e.g. 'alias clean empty')",
                        EXECUTE = null,
                        SIMILARITY = similarity
                    });
                }
            }
            else if (cmd == "unalias")
            {
                if (parts.Length > 1)
                {
                    string shortcut = parts[1].ToLower();
                    if (SettingsManager.Current.ALIASES.TryGetValue(shortcut, out string? expansion))
                    {
                        suggestions.Add(new CommandResult
                        {
                            TITLE = $"Remove Alias: '{shortcut}'",
                            DESCRIPTION = $"Delete the shortcut mapping to: \"{expansion}\"",
                            EXECUTE = () => RemoveAlias(shortcut),
                            SIMILARITY = similarity
                        });
                    }
                    else
                    {
                        suggestions.Add(new CommandResult
                        {
                            TITLE = $"Remove Alias: '{shortcut}' (Not Found)",
                            DESCRIPTION = "No such alias is currently configured",
                            EXECUTE = null,
                            SIMILARITY = similarity
                        });
                    }
                }
                else
                {
                    suggestions.Add(new CommandResult
                    {
                        TITLE = "Remove Alias...",
                        DESCRIPTION = "Type the alias shortcut to remove (e.g. 'unalias clean')",
                        EXECUTE = null,
                        SIMILARITY = similarity
                    });
                }
            }

            return suggestions;
        }

        private static void SetAlias(string shortcut, string expansion)
        {
            try
            {
                // Prevent circular aliases
                if (shortcut == expansion || expansion.StartsWith(shortcut + " "))
                {
                    TextOverlay.Show("⚠️ Cannot map an alias to itself or to a command starting with itself!", 3000);
                    return;
                }

                SettingsManager.Current.ALIASES[shortcut] = expansion;
                SettingsManager.Save();
                TextOverlay.Show($"🏷️ Configured alias '{shortcut}' successfully!", 2500);
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Failed to save alias: {ex.Message}", 3000);
            }
        }

        private static void RemoveAlias(string shortcut)
        {
            try
            {
                if (SettingsManager.Current.ALIASES.Remove(shortcut))
                {
                    SettingsManager.Save();
                    TextOverlay.Show($"🏷️ Removed alias '{shortcut}' successfully!", 2500);
                }
            }
            catch (Exception ex)
            {
                TextOverlay.Show($"⚠️ Failed to remove alias: {ex.Message}", 3000);
            }
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("alias <n> <cmd>", "Create persistent command alias", "alias gp push"),
                new CommandDesc("alias list", "List registered custom aliases", "alias list"),
                new CommandDesc("alias remove <n>", "Delete a custom command alias", "alias remove gp")
            };
        }
    }
}
