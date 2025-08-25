namespace SamplePlugin.Core.Application;

public static class StateUpdater
{
    public static ApplicationState Update(ApplicationState state, Message message)
    {
        return message switch
        {
            IncrementCounter => state with { MessageCount = state.MessageCount + 1 },
            
            ChatMessageReceived msg => state with 
            { 
                MessageCount = state.MessageCount + 1,
                LastMessage = msg.Data.Message,
                LastSender = msg.Data.Sender
            },
            
            _ => state
        };
    }
}