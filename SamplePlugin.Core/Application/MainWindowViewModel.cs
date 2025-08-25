using System;
using SamplePlugin.Core.Chat;

namespace SamplePlugin.Core.Application;

public class MainWindowViewModel
{
    private ApplicationState currentState = new();
    
    public ApplicationState State => currentState;
    
    public event EventHandler? OnStateChanged;
    
    public void ProcessMessage(Message message)
    {
        currentState = StateUpdater.Update(currentState, message);
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public void OnIncrementClick()
    {
        ProcessMessage(new IncrementCounter());
    }
    
    public void OnChatMessageReceived(ChatMessageData data)
    {
        ProcessMessage(new ChatMessageReceived(data));
    }
}
