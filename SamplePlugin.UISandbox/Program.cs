using System;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Microsoft.Extensions.DependencyInjection;
using SamplePlugin.Core;
using SamplePlugin.Core.Application;
using SamplePlugin.Core.Chat;
using SamplePlugin.Tests.Chat;

namespace SamplePlugin.UISandbox;

internal static class Program
{
    private static void Main()
    {
        // Create window
        WindowCreateInfo windowCi = new()
        {
            X = 100,
            Y = 100,
            WindowWidth = 800,
            WindowHeight = 600,
            WindowTitle = "SamplePlugin UI Sandbox"
        };
        
        var window = VeldridStartup.CreateWindow(windowCi);
        
        // Create graphics device
        GraphicsDeviceOptions options = new()
        {
            PreferStandardClipSpaceYDirection = true,
            PreferDepthRangeZeroToOne = true
        };
        
        var gd = VeldridStartup.CreateGraphicsDevice(window, options);
        
        // Create ImGui renderer
        ImGuiRenderer imguiRenderer = new(gd, gd.MainSwapchain.Framebuffer.OutputDescription, 
            window.Width, window.Height);
        
        // Set up DI
        var services = new ServiceCollection();
        services.AddSingleton<IChatMessageProvider, MockChatMessageProvider>();
        ServiceConfiguration.ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();
        
        // Get services
        var viewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
        var chatProvider = (MockChatMessageProvider)serviceProvider.GetRequiredService<IChatMessageProvider>();
        
        // Wire up events
        chatProvider.OnChatMessageReceived += (_, e) =>
        {
            viewModel.OnChatMessageReceived(new ChatMessageData
            {
                Message = e.Message,
                Sender = e.Sender,
                Timestamp = e.Timestamp
            });
        };
        
        // Main loop
        var cl = gd.ResourceFactory.CreateCommandList();
        var showControlPanel = true;
        var showMainWindow = true;
        
        while (window.Exists)
        {
            var snapshot = window.PumpEvents();
            
            if (!window.Exists) break;
            
            imguiRenderer.Update(1f / 60f, snapshot);
            
            // Draw control panel
            if (showControlPanel)
            {
                ImGuiNET.ImGui.Begin("Control Panel", ref showControlPanel);
                
                if (ImGuiNET.ImGui.Button("Simulate Chat Message"))
                {
                    chatProvider.SimulateChatMessage(
                        $"Test message {DateTime.Now:HH:mm:ss}",
                        "TestUser");
                }
                
                ImGuiNET.ImGui.Separator();
                ImGuiNET.ImGui.Checkbox("Show Main Window", ref showMainWindow);
                
                ImGuiNET.ImGui.End();
            }
            
            // Draw main window
            if (showMainWindow)
            {
                DrawMainWindow(viewModel);
            }
            
            // Render
            cl.Begin();
            cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
            cl.ClearColorTarget(0, RgbaFloat.Black);
            imguiRenderer.Render(gd, cl);
            cl.End();
            gd.SubmitCommands(cl);
            gd.SwapBuffers(gd.MainSwapchain);
        }
        
        // Cleanup
        imguiRenderer.Dispose();
        cl.Dispose();
        gd.Dispose();
    }

    private static void DrawMainWindow(MainWindowViewModel viewModel)
    {
        ImGuiNET.ImGui.Begin("SamplePlugin");
        
        var state = viewModel.State;
        
        ImGuiNET.ImGui.Text($"Message Count: {state.MessageCount}");
        
        if (ImGuiNET.ImGui.Button("Increment Counter"))
        {
            viewModel.OnIncrementClick();
        }
        
        ImGuiNET.ImGui.Separator();
        
        if (!string.IsNullOrEmpty(state.LastMessage))
        {
            ImGuiNET.ImGui.Text("Last Message:");
            ImGuiNET.ImGui.Text($"From: {state.LastSender}");
            ImGuiNET.ImGui.TextWrapped(state.LastMessage);
        }
        else
        {
            ImGuiNET.ImGui.Text("No messages received yet");
        }
        
        ImGuiNET.ImGui.End();
    }
}
