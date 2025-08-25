using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using Microsoft.Extensions.DependencyInjection;
using SamplePlugin.Core;
using SamplePlugin.Core.Application;
using SamplePlugin.Core.Chat;
using SamplePlugin.Chat;
using SamplePlugin.Application;

namespace SamplePlugin;

public sealed class Plugin : IDalamudPlugin
{
    private readonly ServiceProvider serviceProvider;
    private readonly WindowSystem windowSystem;
    private readonly IChatMessageProvider chatProvider;
    
    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IChatGui chatGui)
    {
        var services = new ServiceCollection();
        
        // Register Dalamud services
        services.AddSingleton(pluginInterface);
        services.AddSingleton(commandManager);
        services.AddSingleton(chatGui);
        
        // Register window system
        windowSystem = new WindowSystem("SamplePlugin");
        services.AddSingleton(windowSystem);
        
        // Register our implementations
        services.AddSingleton<IChatMessageProvider, DalamudChatMessageProvider>();
        
        // Configure Core services
        ServiceConfiguration.ConfigureServices(services);
        
        // Build DI container
        serviceProvider = services.BuildServiceProvider();
        
        // Initialize chat provider
        chatProvider = serviceProvider.GetRequiredService<IChatMessageProvider>();
        chatProvider.Initialize();
        
        // Create and register main window
        var viewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
        var mainWindow = new MainWindow(viewModel);
        windowSystem.AddWindow(mainWindow);
        
        // Wire up chat events to view model
        chatProvider.OnChatMessageReceived += (_, e) =>
        {
            viewModel.OnChatMessageReceived(new ChatMessageData
            {
                Message = e.Message,
                Sender = e.Sender,
                Timestamp = e.Timestamp
            });
        };
        
        // Register UI events
        pluginInterface.UiBuilder.Draw += windowSystem.Draw;
        pluginInterface.UiBuilder.OpenMainUi += () => mainWindow.IsOpen = true;
        
        // Register commands
        commandManager.AddHandler("/SamplePlugin", new Dalamud.Game.Command.CommandInfo((_, _) => 
        {
            mainWindow.IsOpen = true;
        })
        {
            HelpMessage = "Open SamplePlugin window"
        });
    }
    
    public void Dispose()
    {
        windowSystem.RemoveAllWindows();
        
        var pluginInterface = serviceProvider.GetRequiredService<IDalamudPluginInterface>();
        pluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        
        var commandManager = serviceProvider.GetRequiredService<ICommandManager>();
        commandManager.RemoveHandler("/SamplePlugin");
        
        chatProvider?.Dispose();
        serviceProvider?.Dispose();
    }
}
