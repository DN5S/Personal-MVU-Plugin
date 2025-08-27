using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility.Raii;
using SamplePlugin.Core.Module;
using SamplePlugin.Core.Configuration;
using Dalamud.Bindings.ImGui;

namespace SamplePlugin.Core.UI;

public class MainWindow : Window, IDisposable
{
    private readonly ModuleManager moduleManager;
    private readonly PluginConfiguration configuration;
    private readonly Action openConfigWindow;
    
    public MainWindow(ModuleManager moduleManager, PluginConfiguration configuration, Action openConfigWindow) 
        : base("Sample Plugin###SamplePluginMain", ImGuiWindowFlags.None)
    {
        this.moduleManager = moduleManager ?? throw new ArgumentNullException(nameof(moduleManager));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.openConfigWindow = openConfigWindow ?? throw new ArgumentNullException(nameof(openConfigWindow));
        
        Size = new Vector2(800, 600);
        SizeCondition = ImGuiCond.FirstUseEver;
    }
    
    public override void Draw()
    {
        DrawHeader();
        
        ImGui.Separator();
        
        if (moduleManager.LoadedModules.Count == 0)
        {
            DrawNoModulesMessage();
        }
        else
        {
            DrawModuleTabs();
        }
    }
    
    private void DrawHeader()
    {
        ImGui.Text("Sample Plugin - Module Management System");
        ImGui.SameLine();
        
        // Settings button on the right
        var buttonWidth = 100;
        ImGui.SetCursorPosX(ImGui.GetWindowWidth() - buttonWidth - ImGui.GetStyle().WindowPadding.X);
        
        if (ImGui.Button("Settings", new Vector2(buttonWidth, 0)))
        {
            openConfigWindow();
        }
        
        ImGui.TextDisabled($"Loaded Modules: {moduleManager.LoadedModules.Count}");
    }
    
    private void DrawNoModulesMessage()
    {
        var windowSize = ImGui.GetWindowSize();
        var textHeight = ImGui.GetTextLineHeight() * 4;
        
        ImGui.SetCursorPosY((windowSize.Y - textHeight) / 2);
        
        var text = "No modules are currently loaded.";
        var textWidth = ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX((windowSize.X - textWidth) / 2);
        ImGui.TextDisabled(text);
        
        text = "Check the settings to enable modules.";
        textWidth = ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX((windowSize.X - textWidth) / 2);
        ImGui.TextDisabled(text);
        
        var buttonText = "Open Settings";
        var buttonWidth = ImGui.CalcTextSize(buttonText).X + ImGui.GetStyle().FramePadding.X * 4;
        ImGui.SetCursorPosX((windowSize.X - buttonWidth) / 2);
        
        if (ImGui.Button(buttonText))
        {
            openConfigWindow();
        }
    }
    
    private void DrawModuleTabs()
    {
        using var tabBar = ImRaii.TabBar("ModuleTabs");
        if (!tabBar) return;
        
        foreach (var module in moduleManager.LoadedModules)
        {
            using var tab = ImRaii.TabItem(module.Name);
            if (!tab) continue;
            
            DrawModuleContent(module);
        }
        
        // Add a tab for the overview
        using var overviewTab = ImRaii.TabItem("Overview");
        if (overviewTab)
        {
            DrawOverview();
        }
    }
    
    private void DrawModuleContent(IModule module)
    {
        ImGui.Text($"Module: {module.Name}");
        ImGui.TextDisabled($"Version: {module.Version}");
        
        // Show module configuration status
        var moduleConfig = configuration.GetModuleConfig(module.Name);
        if (moduleConfig.IsEnabled)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0, 1, 0, 1), "[Enabled]");
        }
        else
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "[Disabled in config]");
        }
        
        if (module.Dependencies.Length > 0)
        {
            ImGui.TextDisabled($"Dependencies: {string.Join(", ", module.Dependencies)}");
        }
        
        ImGui.Separator();
        ImGui.Spacing();
        
        // Let the module draw its own UI
        using (ImRaii.Child("ModuleContent", new Vector2(0, 0), false))
        {
            try
            {
                module.DrawUI();
            }
            catch (Exception ex)
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Error drawing module UI: {ex.Message}");
            }
        }
    }
    
    private void DrawOverview()
    {
        ImGui.Text("System Overview");
        ImGui.Separator();
        
        // Display configuration statistics
        if (LayoutHelpers.BeginSection("Configuration"))
        {
            var allModuleConfigs = configuration.GetAllModuleConfigs();
            var enabledCount = allModuleConfigs.Count(c => c.Value.IsEnabled);
            var disabledCount = allModuleConfigs.Count - enabledCount;
            
            ImGui.Text($"Total Configured Modules: {allModuleConfigs.Count}");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0, 1, 0, 1), $"Enabled: {enabledCount}");
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), $"Disabled: {disabledCount}");
            
            ImGui.Text($"Currently Loaded: {moduleManager.LoadedModules.Count}");
            
            if (disabledCount > 0)
            {
                ImGui.TextDisabled("Some modules are disabled. Check settings to enable them.");
            }
            LayoutHelpers.EndSection();
        }
        
        ImGui.Spacing();
        
        if (LayoutHelpers.BeginSection("Loaded Modules"))
        {
            if (ImGui.BeginTable("ModuleOverviewTable", 4, 
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Module", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("Version", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Dependencies", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableHeadersRow();
                
                foreach (var module in moduleManager.LoadedModules)
                {
                    ImGui.TableNextRow();
                    
                    ImGui.TableNextColumn();
                    ImGui.Text(module.Name);
                    
                    ImGui.TableNextColumn();
                    ImGui.TextDisabled(module.Version);
                    
                    ImGui.TableNextColumn();
                    var deps = module.Dependencies.Length > 0 
                        ? string.Join(", ", module.Dependencies) 
                        : "None";
                    ImGui.TextDisabled(deps);
                    
                    ImGui.TableNextColumn();
                    ImGui.TextColored(new Vector4(0, 1, 0, 1), "Active");
                }
                
                ImGui.EndTable();
            }
            LayoutHelpers.EndSection();
        }
        
        ImGui.Spacing();
        
        if (LayoutHelpers.BeginSection("Statistics"))
        {
            ImGui.Text($"Total Modules Loaded: {moduleManager.LoadedModules.Count}");
            
            // Count modules with dependencies
            var modulesWithDeps = 0;
            foreach (var module in moduleManager.LoadedModules)
            {
                if (module.Dependencies.Length > 0)
                    modulesWithDeps++;
            }
            
            ImGui.Text($"Modules with Dependencies: {modulesWithDeps}");
            LayoutHelpers.EndSection();
        }
        
        ImGui.Spacing();
        
        if (ImGui.Button("Open Configuration"))
        {
            openConfigWindow();
        }
        
        ImGui.SameLine();
        
        if (ImGui.Button("Reload All Modules"))
        {
            // This would reload all modules - implementation would go here
            ImGui.OpenPopup("ReloadConfirmation");
        }

        using var popup = ImRaii.Popup("ReloadConfirmation");
        if (popup)
        {
            ImGui.Text("Are you sure you want to reload all modules?");
            ImGui.Text("This may cause temporary interruption.");
                
            if (ImGui.Button("Yes, Reload"))
            {
                // Reload logic would go here
                ImGui.CloseCurrentPopup();
            }
                
            ImGui.SameLine();
                
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }
        }
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
