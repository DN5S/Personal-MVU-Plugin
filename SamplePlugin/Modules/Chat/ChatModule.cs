using System;
using Microsoft.Extensions.DependencyInjection;
using SamplePlugin.Core.Module;
using SamplePlugin.Modules.Chat.Models;
using Dalamud.Plugin.Services;
using Dalamud.Game.Text;

namespace SamplePlugin.Modules.Chat;

[ModuleInfo("Chat", "1.0.0", Description = "Chat monitoring and filtering module", Author = "Sample Author")]
public class ChatModule : ModuleBase
{
    private ChatWindow? window;
    private ChatViewModel? viewModel;
    private IChatGui? chatGui;
    private ChatModuleConfiguration? moduleConfig;
    
    public override string Name => "Chat";
    public override string Version => "1.0.0";
    
    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<ChatViewModel>();
    }
    
    protected override void LoadConfiguration()
    {
        moduleConfig = GetModuleConfig<ChatModuleConfiguration>();
    }
    
    public override void Initialize()
    {
        chatGui = Services.GetRequiredService<IChatGui>();
        viewModel = Services.GetRequiredService<ChatViewModel>();
        
        // Initialize ViewModel with configuration
        viewModel.Initialize(moduleConfig!);
        
        window = new ChatWindow(viewModel, moduleConfig!, () => 
        {
            SetModuleConfig(moduleConfig!);
        });
        
        // Hook into Dalamud's chat
        chatGui.ChatMessage += OnChatMessage;
        
        Logger.Information("Chat module initialized");
    }
    
    private void OnChatMessage(XivChatType type, int timestamp, ref Dalamud.Game.Text.SeStringHandling.SeString sender, ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
    {
        var chatMessage = new ChatMessage
        {
            Type = type,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime,
            Sender = sender.TextValue,
            Message = message.TextValue
        };
        
        viewModel?.AddMessage(chatMessage);
        
        // Publish to EventBus for other modules
        EventBus.Publish(new ChatMessageReceived(chatMessage));
    }
    
    public override void DrawUI()
    {
        window?.Draw();
    }
    
    public override void DrawConfiguration()
    {
        window?.DrawConfiguration();
    }
    
    public override void Dispose()
    {
        if (chatGui != null)
        {
            chatGui.ChatMessage -= OnChatMessage;
        }
        
        window?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
