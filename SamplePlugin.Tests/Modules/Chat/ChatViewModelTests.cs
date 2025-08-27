using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using SamplePlugin.Modules.Chat;
using SamplePlugin.Modules.Chat.Models;
using Xunit;
using Dalamud.Game.Text;

namespace SamplePlugin.Tests.Modules.Chat;

public class ChatViewModelTests : IDisposable
{
    private readonly ChatViewModel viewModel;
    private readonly ChatModuleConfiguration configuration;
    
    public ChatViewModelTests()
    {
        viewModel = new ChatViewModel();
        configuration = new ChatModuleConfiguration();
        viewModel.Initialize(configuration);
    }

    [Fact]
    public void AddMessage_ShouldAddMessageToCollection()
    {
        // Arrange
        var message = new ChatMessage
        {
            Type = XivChatType.Say,
            Sender = "Player",
            Message = "Hello World",
            Timestamp = DateTime.Now
        };
        
        // Act
        viewModel.AddMessage(message);
        
        // Assert
        viewModel.Messages.Should().ContainSingle()
            .Which.Message.Should().Be("Hello World");
    }
    
    [Fact]
    public void AddMessage_WithFilteredChannel_ShouldNotShowMessage()
    {
        // Arrange
        viewModel.ToggleChannel(XivChatType.Say); // Disable Say channel
        
        var message = new ChatMessage
        {
            Type = XivChatType.Say,
            Sender = "Player",
            Message = "Should not appear",
            Timestamp = DateTime.Now
        };
        
        // Act
        viewModel.AddMessage(message);
        
        // Assert
        viewModel.Messages.Should().BeEmpty();
    }
    
    [Fact]
    public void AddMessage_ExceedingMaxMessages_ShouldRemoveOldest()
    {
        // Arrange
        var config = new ChatModuleConfiguration { MaxMessages = 3 };
        viewModel.Initialize(config);
        
        // Act
        for (var i = 1; i <= 5; i++)
        {
            viewModel.AddMessage(new ChatMessage
            {
                Type = XivChatType.Say,
                Message = $"Message {i}",
                Sender = "Player",
                Timestamp = DateTime.Now
            });
        }
        
        // Assert
        viewModel.Messages.Should().HaveCount(3);
        viewModel.Messages.Select(m => m.Message)
            .Should().ContainInOrder("Message 3", "Message 4", "Message 5");
    }
    
    [Fact]
    public void SetFilter_ShouldFilterMessages()
    {
        // Arrange
        viewModel.AddMessage(new ChatMessage
        {
            Type = XivChatType.Say,
            Message = "Hello world",
            Sender = "Player1",
            Timestamp = DateTime.Now
        });
        
        viewModel.AddMessage(new ChatMessage
        {
            Type = XivChatType.Say,
            Message = "Goodbye",
            Sender = "Player2",
            Timestamp = DateTime.Now
        });
        
        // Act
        viewModel.SetFilter("Hello");
        Thread.Sleep(400); // Wait for throttle
        
        // Assert
        viewModel.Messages.Should().ContainSingle()
            .Which.Message.Should().Be("Hello world");
    }
    
    [Fact]
    public void SetFilter_BySender_ShouldFilterCorrectly()
    {
        // Arrange
        viewModel.AddMessage(new ChatMessage
        {
            Type = XivChatType.Say,
            Message = "Message 1",
            Sender = "Alice",
            Timestamp = DateTime.Now
        });
        
        viewModel.AddMessage(new ChatMessage
        {
            Type = XivChatType.Say,
            Message = "Message 2",
            Sender = "Bob",
            Timestamp = DateTime.Now
        });
        
        // Act
        viewModel.SetFilter("Alice");
        Thread.Sleep(400); // Wait for throttle
        
        // Assert
        viewModel.Messages.Should().ContainSingle()
            .Which.Sender.Should().Be("Alice");
    }
    
    [Fact]
    public void SetFilter_CaseInsensitive_ShouldWork()
    {
        // Arrange
        viewModel.AddMessage(new ChatMessage
        {
            Type = XivChatType.Say,
            Message = "HELLO WORLD",
            Sender = "Player",
            Timestamp = DateTime.Now
        });
        
        // Act
        viewModel.SetFilter("hello");
        Thread.Sleep(400); // Wait for throttle
        
        // Assert
        viewModel.Messages.Should().ContainSingle();
    }
    
    [Fact]
    public void ToggleChannel_ShouldEnableAndDisable()
    {
        // Arrange & Act
        var initialState = viewModel.EnabledChannels.Contains(XivChatType.Say);
        viewModel.ToggleChannel(XivChatType.Say);
        var afterFirstToggle = viewModel.EnabledChannels.Contains(XivChatType.Say);
        viewModel.ToggleChannel(XivChatType.Say);
        var afterSecondToggle = viewModel.EnabledChannels.Contains(XivChatType.Say);
        
        // Assert
        initialState.Should().BeTrue();
        afterFirstToggle.Should().BeFalse();
        afterSecondToggle.Should().BeTrue();
    }
    
    [Fact]
    public void ToggleChannel_ShouldFilterExistingMessages()
    {
        // Arrange
        viewModel.AddMessage(new ChatMessage
        {
            Type = XivChatType.Say,
            Message = "Say message",
            Sender = "Player",
            Timestamp = DateTime.Now
        });
        
        viewModel.AddMessage(new ChatMessage
        {
            Type = XivChatType.Shout,
            Message = "Shout message",
            Sender = "Player",
            Timestamp = DateTime.Now
        });
        
        // Act
        viewModel.ToggleChannel(XivChatType.Say);
        Thread.Sleep(400); // Wait for throttle (300 ms) to apply
        
        // Assert
        viewModel.Messages.Should().ContainSingle()
            .Which.Type.Should().Be(XivChatType.Shout);
    }
    
    [Fact]
    public void ClearMessages_ShouldRemoveAllMessages()
    {
        // Arrange
        for (var i = 0; i < 5; i++)
        {
            viewModel.AddMessage(new ChatMessage
            {
                Type = XivChatType.Say,
                Message = $"Message {i}",
                Sender = "Player",
                Timestamp = DateTime.Now
            });
        }
        
        // Act
        viewModel.ClearMessages();
        
        // Assert
        viewModel.Messages.Should().BeEmpty();
    }
    
    [Fact]
    public void NewMessage_Observable_ShouldEmitAddedMessages()
    {
        // Arrange
        var receivedMessages = new List<ChatMessage>();
        using var subscription = viewModel.NewMessage.Subscribe(msg => receivedMessages.Add(msg));
        
        var message = new ChatMessage
        {
            Type = XivChatType.Say,
            Message = "Test",
            Sender = "Player",
            Timestamp = DateTime.Now
        };
        
        // Act
        viewModel.AddMessage(message);
        
        // Assert
        receivedMessages.Should().ContainSingle()
            .Which.Message.Should().Be("Test");
    }
    
    [Fact]
    public void Filter_Observable_ShouldEmitFilterChanges()
    {
        // Arrange
        var receivedFilters = new List<string>();
        using var subscription = viewModel.Filter.Subscribe(f => receivedFilters.Add(f));
        
        // Act
        viewModel.SetFilter("test");
        
        // Assert
        receivedFilters.Should().Contain("test");
    }
    
    [Fact]
    public void DefaultEnabledChannels_ShouldIncludeCommonChannels()
    {
        // Assert
        viewModel.EnabledChannels.Should().Contain([
            XivChatType.Say,
            XivChatType.Shout,
            XivChatType.Party,
            XivChatType.FreeCompany,
            XivChatType.TellIncoming,
            XivChatType.TellOutgoing
        ]);
    }
    
    [Fact]
    public void MaxMessages_DefaultValue_ShouldBe1000()
    {
        // Assert
        viewModel.MaxMessages.Should().Be(1000);
    }
    
    [Fact]
    public void Dispose_ShouldCleanupResources()
    {
        // Arrange
        var subscription = viewModel.NewMessage.Subscribe(_ => { });
        
        // Act
        viewModel.Dispose();
        subscription.Dispose();
        
        // Assert - Should not throw when trying to add a message after dispose
        var action = () => viewModel.AddMessage(new ChatMessage
        {
            Type = XivChatType.Say,
            Message = "Test",
            Sender = "Player",
            Timestamp = DateTime.Now
        });
        
        action.Should().Throw<ObjectDisposedException>();
    }
    
    public void Dispose()
    {
        viewModel?.Dispose();
        GC.SuppressFinalize(this);
    }
}
