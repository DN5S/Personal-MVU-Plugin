using Xunit;
using FluentAssertions;
using SamplePlugin.Core.Application;
using SamplePlugin.Core.Chat;

namespace SamplePlugin.Tests.Application;

public class StateUpdaterTests
{
    [Fact]
    public void Update_WithIncrementCounter_IncrementsCount()
    {
        // Arrange
        var initialState = new ApplicationState { MessageCount = 5 };
        var message = new IncrementCounter();
        
        // Act
        var newState = StateUpdater.Update(initialState, message);
        
        // Assert
        newState.MessageCount.Should().Be(6);
    }
    
    [Fact]
    public void Update_WithChatMessage_UpdatesStateCorrectly()
    {
        // Arrange
        var initialState = new ApplicationState { MessageCount = 0 };
        var chatData = new ChatMessageData
        {
            Message = "Hello World",
            Sender = "TestUser",
            Timestamp = System.DateTime.Now
        };
        var message = new ChatMessageReceived(chatData);
        
        // Act
        var newState = StateUpdater.Update(initialState, message);
        
        // Assert
        newState.MessageCount.Should().Be(1);
        newState.LastMessage.Should().Be("Hello World");
        newState.LastSender.Should().Be("TestUser");
    }
    
    [Fact]
    public void Update_DoesNotMutateOriginalState()
    {
        // Arrange
        var initialState = new ApplicationState { MessageCount = 5 };
        var message = new IncrementCounter();
        
        // Act
        var newState = StateUpdater.Update(initialState, message);
        
        // Assert
        initialState.MessageCount.Should().Be(5);
        newState.Should().NotBeSameAs(initialState);
    }
}