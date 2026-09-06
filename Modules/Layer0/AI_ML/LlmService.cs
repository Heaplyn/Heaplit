// Developer: heaplyn
// Date: 2026-08-18
// Summary: Core implementation of ILlmService.
//          Unified wrapper that redirects all traffic to the exhaustive LlmRouter.
//          Ensures consistent failover logic across the entire application.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HeaplitLauncher
{
    public class LlmService : ILlmService
    {
        public LlmService() { }

        public async Task<bool> IsLocalAvailableAsync() => await LlmRouter.IsOllamaAvailableAsync();

        public async Task<List<string>> GetLocalModelsAsync()
        {
            try { return await LlmRouter.GetOllamaModelsAsync(); }
            catch { return new List<string>(); }
        }

        public async Task<string> DiscoverAiServersAsync()
        {
            await LlmRouter.IsOllamaAvailableAsync();
            return "Discovery via Router complete.";
        }

        public async Task<string> AskAsync(string prompt, List<ChatTurn>? history = null, CancellationToken ct = default)
        {
            // The Router now contains the exhaustive failover and global model cycling logic
            return await LlmRouter.AskAsync(prompt, history, ct);
        }

        public async Task<string> AskOllamaStreamAsync(string prompt, List<ChatTurn>? history, Action<string> onToken, CancellationToken ct = default)
        {
            // Redirect streaming to Router's implementation
            return await LlmRouter.AskOllamaStreamAsync(prompt, history, onToken, ct);
        }
    }
}
