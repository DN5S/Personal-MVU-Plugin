using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility.Raii;
using SamplePlugin.Core.UI;
using Dalamud.Game.Text;
using Dalamud.Bindings.ImGui;

namespace SamplePlugin.Modules.Chat;

public class ChatWindow : Window, IDisposable
{
    private readonly ChatViewModel viewModel;
    private readonly ChatModuleConfiguration configuration;
    private readonly Action saveConfiguration;
    private string filterText = string.Empty;
    
    public ChatWindow(ChatViewModel viewModel, ChatModuleConfiguration configuration, Action saveConfiguration) 
        : base("Chat Monitor###ChatMonitor", ImGuiWindowFlags.None)
    {
        this.viewModel = viewModel;
        this.configuration = configuration;
        this.saveConfiguration = saveConfiguration;

        Size = new Vector2(600, 400);
        SizeCondition = ImGuiCond.FirstUseEver;
    }
    
    public override void Draw()
    {
        DrawToolbar();
        ImGui.Separator();
        DrawMessageList();
    }
    
    private void DrawToolbar()
    {
        // Filter input
        ImGui.SetNextItemWidth(200f);
        if (ImGui.InputTextWithHint("##ChatFilter", "Filter messages...", ref filterText, 256))
        {
            viewModel.SetFilter(filterText);
        }
        
        ImGui.SameLine();
        
        // Channel filter button
        if (ImGui.Button("Channels"))
        {
            ImGui.OpenPopup("ChannelFilterPopup");
        }
        
        using (var popup = ImRaii.Popup("ChannelFilterPopup"))
        {
            if (popup)
                DrawChannelFilters();
        }
        
        ImGui.SameLine();
        
        // Auto-scroll toggle
        var autoScroll = configuration.AutoScroll;
        if (ImGui.Checkbox("Auto-scroll", ref autoScroll))
        {
            configuration.AutoScroll = autoScroll;
            saveConfiguration();
        }
        
        ImGui.SameLine();
        
        // Clear button
        if (ImGui.Button("Clear"))
        {
            viewModel.ClearMessages();
        }
        
        ImGui.SameLine();
        ImGui.TextDisabled($"({viewModel.Messages.Count} messages)");
    }
    
    private void DrawChannelFilters()
    {
        var allChannels = Enum.GetValues<XivChatType>()
            .Where(c => c != XivChatType.None)
            .OrderBy(c => c.ToString())
            .ToList();
        
        foreach (var channel in allChannels)
        {
            var isEnabled = viewModel.EnabledChannels.Contains(channel);
            if (ImGui.Checkbox(channel.ToString(), ref isEnabled))
            {
                viewModel.ToggleChannel(channel);
            }
        }
    }
    
    private void DrawMessageList()
    {
        using var child = ImRaii.Child("MessageList", new Vector2(0, 0), true);
        if (!child) return;
        
        using var table = ImRaii.Table("ChatTable", 4, 
            ImGuiTableFlags.ScrollY | 
            ImGuiTableFlags.RowBg | 
            ImGuiTableFlags.BordersV | 
            ImGuiTableFlags.Resizable);
        
        if (!table) return;
        
        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableSetupColumn("Channel", ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Sender", ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();
        
        foreach (var message in viewModel.Messages)
        {
            ImGui.TableNextRow();
            
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(configuration.ShowTimestamps ? message.Timestamp.ToString("HH:mm:ss") : "");

            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, GetChannelColor(message.Type)))
            {
                ImGui.TextUnformatted(GetChannelShortName(message.Type));
            }
            
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(message.Sender);
            
            ImGui.TableNextColumn();
            ImGui.TextWrapped(message.Message);
        }
        
        if (configuration.AutoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
        {
            ImGui.SetScrollHereY(1.0f);
        }
    }
    
    private static uint GetChannelColor(XivChatType type)
    {
        return type switch
        {
            XivChatType.Say => 0xFFFFFFFF,
            XivChatType.Shout => 0xFFFF7F50,
            XivChatType.TellIncoming or XivChatType.TellOutgoing => 0xFFFFB6C1,
            XivChatType.Party => 0xFF66DDFF,
            XivChatType.Alliance => 0xFFFF7F00,
            XivChatType.FreeCompany => 0xFF7FFF7F,
            XivChatType.Ls1 or XivChatType.Ls2 or XivChatType.Ls3 or XivChatType.Ls4 or 
            XivChatType.Ls5 or XivChatType.Ls6 or XivChatType.Ls7 or XivChatType.Ls8 => 0xFFFFFF00,
            _ => 0xFFAAAAAA
        };
    }
    
    private static string GetChannelShortName(XivChatType type)
    {
        return type switch
        {
            XivChatType.Say => "Say",
            XivChatType.Shout => "Shout",
            XivChatType.TellIncoming => "Tell (In)",
            XivChatType.TellOutgoing => "Tell (Out)",
            XivChatType.Party => "Party",
            XivChatType.Alliance => "Alliance",
            XivChatType.FreeCompany => "FC",
            XivChatType.Ls1 => "LS1",
            XivChatType.Ls2 => "LS2",
            XivChatType.Ls3 => "LS3",
            XivChatType.Ls4 => "LS4",
            XivChatType.Ls5 => "LS5",
            XivChatType.Ls6 => "LS6",
            XivChatType.Ls7 => "LS7",
            XivChatType.Ls8 => "LS8",
            _ => type.ToString()
        };
    }
    
    public void DrawConfiguration()
    {
        ImGui.Text("Chat Monitor Configuration");
        ImGui.Separator();
        
        var maxMessages = configuration.MaxMessages;
        if (ImGui.InputInt("Max Messages", ref maxMessages, 100, 1000))
        {
            configuration.MaxMessages = Math.Clamp(maxMessages, 100, 10000);
            viewModel.UpdateConfiguration(configuration.MaxMessages, configuration.AutoScroll, configuration.ShowTimestamps);
            saveConfiguration();
        }
        LayoutHelpers.HelpTooltip("Maximum number of messages to keep in memory");
        
        ImGui.Spacing();
        
        var showTimestamps = configuration.ShowTimestamps;
        if (ImGui.Checkbox("Show Timestamps", ref showTimestamps))
        {
            configuration.ShowTimestamps = showTimestamps;
            viewModel.UpdateConfiguration(configuration.MaxMessages, configuration.AutoScroll, configuration.ShowTimestamps);
            saveConfiguration();
        }
        LayoutHelpers.HelpTooltip("Display timestamps for each message");
        
        var autoScrollConfig = configuration.AutoScroll;
        if (ImGui.Checkbox("Auto-scroll", ref autoScrollConfig))
        {
            configuration.AutoScroll = autoScrollConfig;
            viewModel.UpdateConfiguration(configuration.MaxMessages, configuration.AutoScroll, configuration.ShowTimestamps);
            saveConfiguration();
        }
        LayoutHelpers.HelpTooltip("Automatically scroll to new messages");
        
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        
        if (ImGui.Button("Reset Channel Filters"))
        {
            configuration.ResetChannels();
            viewModel.Initialize(configuration); // Re-initialize with default channels
            saveConfiguration();
        }
        LayoutHelpers.HelpTooltip("Reset channel filters to defaults");
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);   
    }
}
