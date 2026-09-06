// Developer: heaplyn
// Date: 2026-08-18
// Summary: Command Handler for Google Cloud Platform integrations.
//          Handles "gcloud", "translate", "vision", and "bucket" commands.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HeaplitLauncher.Modules.Layer3.Handlers
{
    public class GCloudCommandHandler : ICommandHandler
    {
        public bool CanHandle(string query)
        {
            return SearchUtil.MatchesAny(query, "gcloud", "translate", "vision", "bucket", "assist");
        }

        public List<CommandResult> GetSuggestions(string query)
        {
            var l = new List<CommandResult>();
            string q = query.ToLower();

            if (q.StartsWith("assist"))
            {
                l.Add(new CommandResult { TITLE = "🤖 Gemini Cloud Assist", DESCRIPTION = "Query and manage your GCP environment with AI", EXECUTE = () => GoogleCloudOverlay.ShowOverlay() });
            }
            else if (q.StartsWith("gcloud"))
            {
                l.Add(new CommandResult { TITLE = "📊 Google Cloud Dashboard", DESCRIPTION = "View API traffic, errors, and project health", EXECUTE = () => GoogleCloudOverlay.ShowOverlay() });
                l.Add(new CommandResult { TITLE = "🛠️ List Enabled APIs", DESCRIPTION = "Check currently active cloud services", EXECUTE = () => GoogleCloudOverlay.ShowOverlay() });
                l.Add(new CommandResult { TITLE = "🗄️ Cloud Storage Browser", DESCRIPTION = "Manage GCS buckets and files", EXECUTE = () => GoogleCloudOverlay.ShowOverlay() });
            }
            else if (q.StartsWith("translate"))
            {
                l.Add(new CommandResult { TITLE = "🌐 Cloud Translation", DESCRIPTION = "Translate text using Google Cloud", EXECUTE = () => GoogleCloudOverlay.ShowOverlay() });
            }
            else if (q.StartsWith("vision"))
            {
                l.Add(new CommandResult { TITLE = "👁️ Cloud Vision AI", DESCRIPTION = "Analyze images using Vertex AI Vision", EXECUTE = () => GoogleCloudOverlay.ShowOverlay() });
            }
            else if (q.StartsWith("bucket"))
            {
                l.Add(new CommandResult { TITLE = "🗄️ Cloud Storage", DESCRIPTION = "Browse and upload to GCS buckets", EXECUTE = () => GoogleCloudOverlay.ShowOverlay() });
            }

            return l;
        }

        public List<CommandDesc> GetCommandDescriptions()
        {
            return new List<CommandDesc>
            {
                new CommandDesc("gcloud", "Open Google Cloud Management Dashboard", "gcloud"),
                new CommandDesc("translate", "Translate text using high-performance cloud models", "translate Hello"),
                new CommandDesc("vision", "Analyze images using advanced Cloud Vision AI", "vision")
            };
        }
    }
}
