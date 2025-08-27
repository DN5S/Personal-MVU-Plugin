using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility.Raii;
using SamplePlugin.Core.Module;
using SamplePlugin.Core.Configuration;
using Dalamud.Bindings.ImGui;

namespace SamplePlugin.Core.UI;

public class ConfigurationWindow : Window, IDisposable
{
    private readonly ModuleManager moduleManager;
    private readonly PluginConfiguration configuration;
    private string selectedModuleName = string.Empty;
    
    public ConfigurationWindow(ModuleManager moduleManager, PluginConfiguration configuration) 
        : base("Sample Plugin Configuration###SamplePluginConfig", ImGuiWindowFlags.None)
    {
        this.moduleManager = moduleManager ?? throw new ArgumentNullException(nameof(moduleManager));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        Size = new Vector2(700, 500);
        SizeCondition = ImGuiCond.FirstUseEver;
    }
    
    public override void Draw()
    {
        using var tabBar = ImRaii.TabBar("ConfigTabs");
        if (!tabBar) return;
        
        // General settings tab
        using (var generalTab = ImRaii.TabItem("General"))
        {
            if (generalTab)
                DrawGeneralSettings();
        }
        
        // Module tabs
        using (var modulesTab = ImRaii.TabItem("Modules"))
        {
            if (modulesTab)
                DrawModuleSettings();
        }
        
        // Individual module configuration tabs
        foreach (var module in moduleManager.LoadedModules)
        {
            using var moduleTab = ImRaii.TabItem($"{module.Name} Settings");
            if (!moduleTab) continue;
            
            DrawModuleConfiguration(module);
        }
    }
    
    private void DrawGeneralSettings()
    {
        ImGui.Text("General Plugin Settings");
        ImGui.Separator();
        
        using (LayoutHelpers.BeginSection("Plugin Information"))
        {
            ImGui.Text("Sample Plugin - Modular Architecture Template");
            ImGui.TextDisabled("A template for creating modular Dalamud plugins");
            ImGui.Spacing();
            ImGui.Text($"Configuration Version: {configuration.Version}");
        }
        
        ImGui.Spacing();
        
        using (LayoutHelpers.BeginSection("Configuration Management"))
        {
            if (ImGui.Button("Save Configuration"))
            {
                configuration.Save();
                ImGui.OpenPopup("SaveConfirmation");
            }
            
            ImGui.SameLine();
            
            if (ImGui.Button("Reset to Defaults"))
            {
                ImGui.OpenPopup("ResetConfirmation");
            }
            
            ImGui.SameLine();
            
            if (ImGui.Button("Export Configuration"))
            {
                // Export logic would go here
                ImGui.OpenPopup("ExportInfo");
            }
            
            ImGui.SameLine();
            
            if (ImGui.Button("Import Configuration"))
            {
                // Import logic would go here
                ImGui.OpenPopup("ImportWarning");
            }
        }
        
        // Popups
        using (var popup = ImRaii.Popup("SaveConfirmation"))
        {
            if (popup)
            {
                ImGui.Text("Configuration saved successfully!");
                if (ImGui.Button("OK"))
                    ImGui.CloseCurrentPopup();
            }
        }
        
        using (var popup = ImRaii.Popup("ResetConfirmation"))
        {
            if (popup)
            {
                ImGui.Text("Are you sure you want to reset all settings to defaults?");
                ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), "This action cannot be undone!");
                
                if (ImGui.Button("Yes, Reset"))
                {
                    configuration.Reset();
                    ImGui.CloseCurrentPopup();
                }
                
                ImGui.SameLine();
                
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
            }
        }
        
        using (var popup = ImRaii.Popup("ExportInfo"))
        {
            if (popup)
            {
                ImGui.Text("Configuration export is not yet implemented.");
                if (ImGui.Button("OK"))
                    ImGui.CloseCurrentPopup();
            }
        }
        
        using (var popup = ImRaii.Popup("ImportWarning"))
        {
            if (popup)
            {
                ImGui.Text("Configuration import is not yet implemented.");
                if (ImGui.Button("OK"))
                    ImGui.CloseCurrentPopup();
            }
        }
    }
    
    private void DrawModuleSettings()
    {
        ImGui.Text("Module Management");
        ImGui.Separator();
        
        using (LayoutHelpers.BeginSection("Loaded Modules"))
        {
            if (ImGui.BeginTable("ModuleTable", 4, 
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Module", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("Version", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();
                
                foreach (var module in moduleManager.LoadedModules)
                {
                    ImGui.TableNextRow();
                    
                    ImGui.TableNextColumn();
                    ImGui.Text(module.Name);
                    
                    ImGui.TableNextColumn();
                    ImGui.TextDisabled(module.Version);
                    
                    ImGui.TableNextColumn();
                    
                    // Get module configuration to check if enabled
                    var moduleConfig = configuration.GetModuleConfig(module.Name);
                    if (moduleConfig.IsEnabled)
                    {
                        ImGui.TextColored(new Vector4(0, 1, 0, 1), "Enabled");
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(1, 0, 0, 1), "Disabled");
                    }
                    
                    ImGui.TableNextColumn();
                    
                    if (ImGui.Button($"Configure##{module.Name}"))
                    {
                        selectedModuleName = module.Name;
                    }
                    
                    ImGui.SameLine();
                    
                    var isEnabled = moduleConfig.IsEnabled;
                    if (ImGui.Checkbox($"##Enable{module.Name}", ref isEnabled))
                    {
                        moduleConfig.IsEnabled = isEnabled;
                        configuration.SetModuleConfig(module.Name, moduleConfig);
                        configuration.Save();
                    }
                    
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(isEnabled ? "Disable module" : "Enable module");
                    }
                }
                
                ImGui.EndTable();
            }
        }
        
        ImGui.Spacing();
        
        // Show selected module details
        if (!string.IsNullOrEmpty(selectedModuleName))
        {
            using (LayoutHelpers.BeginSection($"Selected Module: {selectedModuleName}"))
            {
                var moduleInfo = moduleManager.GetModuleInfo(selectedModuleName);
                if (moduleInfo != null)
                {
                    ImGui.Text($"Version: {moduleInfo.Version}");
                    if (!string.IsNullOrEmpty(moduleInfo.Author))
                        ImGui.Text($"Author: {moduleInfo.Author}");
                    if (!string.IsNullOrEmpty(moduleInfo.Description))
                        ImGui.TextWrapped($"Description: {moduleInfo.Description}");
                    if (moduleInfo.Dependencies.Length > 0)
                        ImGui.Text($"Dependencies: {string.Join(", ", moduleInfo.Dependencies)}");
                    
                    ImGui.Spacing();
                    if (ImGui.Button("Clear Selection"))
                    {
                        selectedModuleName = string.Empty;
                    }
                }
            }
            
            ImGui.Spacing();
        }
        
        using (LayoutHelpers.BeginSection("Module Dependencies"))
        {
            ImGui.TextWrapped("Some modules depend on others. Disabling a module will also disable " +
                            "all modules that depend on it.");
            
            ImGui.Spacing();
            
            // Show the dependency tree
            foreach (var module in moduleManager.LoadedModules)
            {
                if (module.Dependencies.Length == 0) continue;
                
                ImGui.Text($"{module.Name} depends on: {string.Join(", ", module.Dependencies)}");
            }
        }
    }
    
    private void DrawModuleConfiguration(IModule module)
    {
        ImGui.Text($"Configuration for {module.Name}");
        ImGui.Separator();
        
        // Module info
        ImGui.TextDisabled($"Version: {module.Version}");
        if (module.Dependencies.Length > 0)
        {
            ImGui.TextDisabled($"Dependencies: {string.Join(", ", module.Dependencies)}");
        }
        
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        
        // Let the module draw its own configuration
        using (ImRaii.Child("ModuleConfig", new Vector2(0, 0), false))
        {
            try
            {
                module.DrawConfiguration();
                
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                
                if (ImGui.Button($"Save {module.Name} Settings"))
                {
                    configuration.Save();
                    ImGui.OpenPopup($"Save{module.Name}Confirmation");
                }

                using var popup = ImRaii.Popup($"Save{module.Name}Confirmation");
                if (popup)
                {
                    ImGui.Text($"{module.Name} settings saved!");
                    if (ImGui.Button("OK"))
                        ImGui.CloseCurrentPopup();
                }
            }
            catch (Exception ex)
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Error drawing module configuration: {ex.Message}");
            }
        }
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
