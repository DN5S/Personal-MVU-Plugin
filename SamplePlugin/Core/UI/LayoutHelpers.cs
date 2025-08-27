using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace SamplePlugin.Core.UI;

public static class LayoutHelpers
{
    public static IDisposable BeginPanel(string id, Vector2? size = null, bool border = true)
    {
        var disposables = new CompositeDisposable();
        
        if (border)
            disposables.Add(ImRaii.PushStyle(ImGuiStyleVar.ChildBorderSize, 1f));
        
        disposables.Add(ImRaii.Child(
            id, 
            size ?? Vector2.Zero, 
            border, 
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse
        ));
        
        return disposables;
    }
    
    public static IDisposable BeginFormField(string label, float labelWidth = 150f)
    {
        ImGui.Text(label);
        ImGui.SameLine(labelWidth * ImGuiHelpers.GlobalScale);
        ImGui.SetNextItemWidth(-1);
        return new DummyDisposable();
    }
    
    public static IDisposable BeginTabView(string id)
    {
        return ImRaii.TabBar(id);
    }
    
    public static IDisposable BeginTab(string label)
    {
        return ImRaii.TabItem(label);
    }
    
    public static IDisposable BeginSection(string title, bool defaultOpen = true)
    {
        return ImRaii.TreeNode(title, defaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None);
    }
    
    public static IDisposable BeginCard()
    {
        var disposables = new CompositeDisposable();
        disposables.Add(ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(10, 10)));
        disposables.Add(ImRaii.PushStyle(ImGuiStyleVar.ChildBorderSize, 1f));
        disposables.Add(ImRaii.PushColor(ImGuiCol.ChildBg, ImGui.GetColorU32(ImGuiCol.FrameBg)));
        return disposables;
    }
    
    public static void HelpTooltip(string text)
    {
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            using var tooltip = ImRaii.Tooltip();
            if (tooltip)
            {
                using var wrap = ImRaii.TextWrapPos(ImGui.GetFontSize() * 35.0f);
                ImGui.Text(text);
            }
        }
    }
    
    public static void CenterNextWindow()
    {
        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
    }
    
    public static bool IconButton(FontAwesomeIcon icon, string id = "")
    {
        using var font = ImRaii.PushFont(UiBuilder.IconFont);
        return ImGui.Button($"{icon.ToIconString()}##{id}");
    }
    
    public static void Separator(string text = "")
    {
        if (string.IsNullOrEmpty(text))
        {
            ImGui.Separator();
            return;
        }
        
        var windowWidth = ImGui.GetWindowWidth();
        var textWidth = ImGui.CalcTextSize(text).X;
        
        ImGui.Separator();
        ImGui.SameLine(windowWidth / 2 - textWidth / 2);
        ImGui.Text(text);
        ImGui.Separator();
    }
    
    public static IDisposable BeginPopup(string id, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        return ImRaii.Popup(id, flags);
    }
    
    public static IDisposable BeginGroup()
    {
        return ImRaii.Group();
    }
    
    public static IDisposable WithTextWrap(float wrapWidth = 0)
    {
        return ImRaii.TextWrapPos(wrapWidth == 0 ? ImGui.GetContentRegionAvail().X : wrapWidth);
    }
    
    public static IDisposable WithDisabled(bool disabled = true)
    {
        return ImRaii.Disabled(disabled);
    }
    
    public static IDisposable WithIndent(float indent = 0)
    {
        if (indent > 0)
            return ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, indent);
        
        return new ImRaii.Indent();
    }
    
    public static IDisposable WithFont(ImFontPtr font)
    {
        return ImRaii.PushFont(font);
    }
    
    public static IDisposable WithColor(ImGuiCol colorType, uint color)
    {
        return ImRaii.PushColor(colorType, color);
    }
    
    public static IDisposable WithStyle(ImGuiStyleVar style, float value)
    {
        return ImRaii.PushStyle(style, value);
    }
    
    public static IDisposable WithStyle(ImGuiStyleVar style, Vector2 value)
    {
        return ImRaii.PushStyle(style, value);
    }
    
    private class DummyDisposable : IDisposable
    {
        public void Dispose() { }
    }
    
    private class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> disposables = new();
        
        public void Add(IDisposable disposable)
        {
            disposables.Add(disposable);
        }
        
        public void Dispose()
        {
            for (var i = disposables.Count - 1; i >= 0; i--)
            {
                disposables[i].Dispose();
            }
        }
    }
}
