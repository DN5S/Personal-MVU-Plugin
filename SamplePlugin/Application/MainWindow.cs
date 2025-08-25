using System;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using SamplePlugin.Core.Application;

namespace SamplePlugin.Application;

public class MainWindow : Window, IMainWindow, IDisposable
{
    private readonly MainWindowViewModel viewModel;
    
    public MainWindow(MainWindowViewModel viewModel) : base("SamplePlugin##MainWindow")
    {
        this.viewModel = viewModel;
        
        Size = new System.Numerics.Vector2(400, 300);
        SizeCondition = ImGuiCond.FirstUseEver;
    }
    
    public override void Draw()
    {
        var state = viewModel.State;
        
        ImGui.Text($"Message Count: {state.MessageCount}");
        
        if (ImGui.Button("Increment Counter"))
        {
            viewModel.OnIncrementClick();
        }
        
        ImGui.Separator();
        
        if (!string.IsNullOrEmpty(state.LastMessage))
        {
            ImGui.Text("Last Message:");
            ImGui.Text($"From: {state.LastSender}");
            ImGui.TextWrapped(state.LastMessage);
        }
        else
        {
            ImGui.Text("No messages received yet");
        }
    }
    
    public void Dispose()
    {
    }
}