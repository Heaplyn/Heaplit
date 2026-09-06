# Scaffolding & Implementation Guide

Use these templates to quickly implement new features in Heaplit while maintaining architectural consistency.

## 1. Creating a New Command Handler
Handlers live in `Modules/Layer3/Handlers`. They must implement `ICommandHandler`.

```csharp
namespace HeaplitLauncher
{
    public class MyNewFeatureHandler : ICommandHandler
    {
        public bool CanHandle(string query) => query.StartsWith("myfeature");

        public List<CommandResult> GetSuggestions(string query)
        {
            var suggestions = new List<CommandResult>();
            // Use SearchUtil for fuzzy matching
            double similarity = SearchUtil.GetSimilarity(query, "myfeature");

            suggestions.Add(new CommandResult
            {
                TITLE = "🚀 Run My Feature",
                DESCRIPTION = "Executes the new logic.",
                SIMILARITY = similarity + 5.0,
                EXECUTE = () => MyLogic()
            });
            return suggestions;
        }

        private void MyLogic() { /* implementation */ }
        public List<CommandDesc> GetCommandDescriptions() => new List<CommandDesc> { new CommandDesc("myfeature", "Description", "myfeature example") };
        public void OnStart() { }
    }
}
```
*Note: Remember to register your handler in `CommandParser.cs`.*

## 2. Creating a New UI Overlay
Overlays live in `Modules/Layer2`. They must inherit from `BaseOverlay`.

```csharp
namespace HeaplitLauncher
{
    public class MyOverlay : BaseOverlay
    {
        public MyOverlay() : base("MY OVERLAY TITLE", width: 400, height: 300)
        {
            var grid = new Grid();
            // Add UI elements here
            this.UserContent = grid;
        }

        public static void ShowOverlay()
        {
            Application.Current.Dispatcher.Invoke(() => {
                var win = new MyOverlay();
                win.Show();
            });
        }
    }
}
```

## 3. Creating a Mobile Plugin
Mobile plugins live in `Modules/Layer0/Plugins` of the Heaplit Mobile project.

```csharp
namespace Heaplit_Mobile.Modules.Layer0.Plugins
{
    public class MyMobilePlugin : IHeaplitPlugin
    {
        public string PluginId => "com.heaplit.myfeature";
        public string Name => "My Feature";
        public string Description => "Mobile implementation of X.";
        public string Version => "1.0.0";

        public Task Initialize(IServiceProvider sp) => Task.CompletedTask;
        public Task<bool> CanHandle(string cmd) => Task.FromResult(cmd == "myfeature");
        public async Task<string> ExecuteCommand(string cmd, object[] args) 
        {
             return "Executed!";
        }
        public Task OnShutdown() => Task.CompletedTask;
    }
}
```

---

## The Heaplit Way (Rules for AI)
1. **Glassmorphism**: Use the styles defined in `Styles.xaml`. Never use default WPF colors.
2. **Layering**: Logic belongs in Layer 0. UI belongs in Layer 2. Routing belongs in Layer 3.
3. **Async/Await**: Never block the UI thread. Use `Task.Run` for background work.
4. **Documentation**: After implementing a major feature, update `Docs/FEATURES_CATALOG.md` using the `[WRITE_FILE]` tag.
