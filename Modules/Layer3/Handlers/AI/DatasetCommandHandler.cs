// Developer: heaplyn
// Date: 2026-08-19
// Summary: Command handler for the Dataset Harvester.
//          Allows manual triggering of autonomous dataset discovery and download.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeaplitLauncher.Modules.Layer3.Handlers
{
    public class DatasetCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "dataset", "harvest", "acoustic", "voice data");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            return new List<CommandResult>
            {
                new CommandResult
                {
                    TITLE = "🚀 Harvest Datasets",
                    DESCRIPTION = "Scrape GitHub for LLM datasets and use AI to auto-download high-quality data.",
                    SIMILARITY = query.ToLower().Contains("harvest") ? 1.0 : 0.6,
                    EXECUTE = () => Task.Run(async () => await DatasetHarvester.RunAutomaticHarvestAsync())
                },
                new CommandResult
                {
                    TITLE = "🔊 Expand Acoustic Databases",
                    DESCRIPTION = "Query AI and download voice datasets to calibrate wake words.",
                    SIMILARITY = query.ToLower().Contains("acoustic") ? 1.0 : 0.5,
                    EXECUTE = () => Task.Run(async () => await SystemKnowledgeManager.ExpandAcousticDatabasesAsync(force: true))
                },
                new CommandResult
                {
                    TITLE = "🔍 Search HF Datasets",
                    DESCRIPTION = "Discover new datasets on Hugging Face based on current trends.",
                    SIMILARITY = (SearchUtil.BestSimilarity(query, "dataset", "harvest", "acoustic", "voice data") + 0.8 * 0.01),
                    EXECUTE = () => {
                        Task.Run(async () => {
                            await DatasetHarvester.RunAutomaticHarvestAsync();
                        });
                    }
                }
            };
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc
                {
                    COMMAND_NAME = "Harvest Datasets",
                    COMMAND_DESCRIPTION = "Autonomous scraping of GitHub and Hugging Face for LLM datasets.",
                    COMMAND_EXAMPLE = "harvest datasets"
                }
            };
        }
    }
}
