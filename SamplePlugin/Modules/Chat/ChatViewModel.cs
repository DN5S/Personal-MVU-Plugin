using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using SamplePlugin.Modules.Chat.Models;
using Dalamud.Game.Text;

namespace SamplePlugin.Modules.Chat;

public class ChatViewModel : IDisposable
{
    private readonly BehaviorSubject<string> filterSubject = new(string.Empty);
    private BehaviorSubject<HashSet<XivChatType>>? enabledChannelsSubject;
    private readonly Subject<ChatMessage> newMessageSubject = new();
    private readonly ObservableCollection<ChatMessage> allMessages = [];
    private IDisposable? filterSubscription;
    private ChatModuleConfiguration? configuration;
    
    public ObservableCollection<ChatMessage> Messages { get; } = [];

    public IObservable<string> Filter => filterSubject.AsObservable();
    public IObservable<ChatMessage> NewMessage => newMessageSubject.AsObservable();
    
    public int MaxMessages => configuration?.MaxMessages ?? 1000;
    public HashSet<XivChatType> EnabledChannels => enabledChannelsSubject?.Value ?? [];

    public void Initialize(ChatModuleConfiguration config)
    {
        configuration = config;
        
        // Initialize with configuration values
        enabledChannelsSubject = new BehaviorSubject<HashSet<XivChatType>>(
            config.EnabledChannels);
        
        // Setup filter subscription
        filterSubscription = filterSubject.Throttle(TimeSpan.FromMilliseconds(300))
                                          .CombineLatest(enabledChannelsSubject, (filter, channels) 
                                                             => (filter, channels))
                                          .Subscribe(tuple => ApplyFilter(tuple.filter, tuple.channels));
    }
    
    public void UpdateConfiguration(int maxMessages, bool autoScroll, bool showTimestamps)
    {
        if (configuration != null)
        {
            configuration.MaxMessages = maxMessages;
            configuration.AutoScroll = autoScroll;
            configuration.ShowTimestamps = showTimestamps;
            
            // Trim messages if max was reduced
            while (allMessages.Count > maxMessages)
            {
                var oldMessage = allMessages[0];
                allMessages.RemoveAt(0);
                Messages.Remove(oldMessage);
            }
        }
    }
    
    public void AddMessage(ChatMessage message)
    {
        allMessages.Add(message);
        
        while (allMessages.Count > MaxMessages)
        {
            var oldMessage = allMessages[0];
            allMessages.RemoveAt(0);
            Messages.Remove(oldMessage);
        }
        
        // Check if a message should be shown
        if (enabledChannelsSubject != null && ShouldShowMessage(message, filterSubject.Value, enabledChannelsSubject.Value))
        {
            Messages.Add(message);
        }
        
        newMessageSubject.OnNext(message);
    }
    
    public void SetFilter(string filter)
    {
        filterSubject.OnNext(filter);
    }
    
    public void ToggleChannel(XivChatType channel)
    {
        if (enabledChannelsSubject == null) return;
        
        var channels = new HashSet<XivChatType>(enabledChannelsSubject.Value);
        if (!channels.Add(channel))
            channels.Remove(channel);

        enabledChannelsSubject.OnNext(channels);
        
        // Update configuration
        if (configuration != null)
        {
            configuration.EnabledChannels = channels;
        }
    }
    
    public void ClearMessages()
    {
        allMessages.Clear();
        Messages.Clear();
    }
    
    private void ApplyFilter(string filter, HashSet<XivChatType> channels)
    {
        Messages.Clear();
        
        foreach (var message in allMessages.Where(m => ShouldShowMessage(m, filter, channels)))
        {
            Messages.Add(message);
        }
    }
    
    private static bool ShouldShowMessage(ChatMessage message, string filter, HashSet<XivChatType> channels)
    {
        if (!channels.Contains(message.Type))
            return false;
        
        if (string.IsNullOrEmpty(filter))
            return true;
        
        return message.Message.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
               message.Sender.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }
    
    public void Dispose()
    {
        filterSubscription?.Dispose();
        filterSubject.Dispose();
        enabledChannelsSubject?.Dispose();
        newMessageSubject.Dispose();
        GC.SuppressFinalize(this);
    }
}
