using System;
using System.Collections.Generic;
using System.Linq;
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
    private string moduleToDisable = string.Empty;
    private List<string> affectedDependents = new();
    
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
        
        if (LayoutHelpers.BeginSection("Plugin Information"))
        {
            ImGui.Text("Sample Plugin - Modular Architecture Template");
            ImGui.TextDisabled("A template for creating modular Dalamud plugins");
            ImGui.Spacing();
            ImGui.Text($"Configuration Version: {configuration.Version}");
            LayoutHelpers.EndSection();
        }
        
        ImGui.Spacing();
        
        if (LayoutHelpers.BeginSection("Configuration Management"))
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
            LayoutHelpers.EndSection();
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
        
        if (LayoutHelpers.BeginSection("Module Management"))
        {
            if (ImGui.BeginTable("ModuleTable", 4, 
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Module", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("Version", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();
                
                // Show all registered modules, not just loaded ones
                if (moduleManager.Registry.ModuleInfos.Count == 0)
                {
                    moduleManager.Registry.DiscoverModules();
                }
                
                foreach (var kvp in moduleManager.Registry.ModuleInfos)
                {
                    var moduleName = kvp.Key;
                    var moduleInfo = kvp.Value;
                    
                    ImGui.TableNextRow();
                    
                    ImGui.TableNextColumn();
                    ImGui.Text(moduleName);
                    
                    ImGui.TableNextColumn();
                    ImGui.TextDisabled(moduleInfo.Version);
                    
                    ImGui.TableNextColumn();
                    
                    // Get module configuration to check if enabled
                    var moduleConfig = configuration.GetModuleConfig(moduleName);
                    var isLoaded = moduleManager.LoadedModules.Any(m => m.Name == moduleName);
                    
                    if (moduleConfig.IsEnabled && isLoaded)
                    {
                        ImGui.TextColored(new Vector4(0, 1, 0, 1), "Loaded");
                    }
                    else if (moduleConfig.IsEnabled && !isLoaded)
                    {
                        ImGui.TextColored(new Vector4(1, 1, 0, 1), "Enabled");
                    }
                    else
                    {
                        ImGui.TextColored(new Vector4(1, 0, 0, 1), "Disabled");
                    }
                    
                    ImGui.TableNextColumn();
                    
                    if (ImGui.Button($"Configure##{moduleName}"))
                    {
                        selectedModuleName = moduleName;
                    }
                    
                    ImGui.SameLine();
                    
                    // Show the current state from configuration
                    var currentEnabled = moduleConfig.IsEnabled;
                    var checkboxEnabled = currentEnabled;
                    
                    if (ImGui.Checkbox($"##Enable{moduleName}", ref checkboxEnabled))
                    {
                        // Checkbox was clicked, check if we can actually change it
                        if (currentEnabled && !checkboxEnabled)
                        {
                            // Trying to disable
                            var (canDisable, dependents) = moduleManager.CanDisableModule(moduleName, configuration);
                            if (!canDisable && dependents.Count > 0)
                            {
                                // Cannot disable, show warning
                                moduleToDisable = moduleName;
                                affectedDependents = dependents.ToList();
                                ImGui.OpenPopup("Disable Module Warning");
                                // Don't change the config yet
                            }
                            else
                            {
                                // Can disable directly
                                moduleConfig.IsEnabled = false;
                                configuration.SetModuleConfig(moduleName, moduleConfig);
                                configuration.Save();
                                moduleManager.ApplyConfigurationChanges(configuration);
                            }
                        }
                        else if (!currentEnabled && checkboxEnabled)
                        {
                            // Trying to enable
                            if (!moduleManager.AreDependenciesSatisfied(moduleName, configuration))
                            {
                                // Cannot enable, show error
                                ImGui.OpenPopup("EnableDependencyError");
                                // Don't change the config
                            }
                            else
                            {
                                // Can enable directly
                                moduleConfig.IsEnabled = true;
                                configuration.SetModuleConfig(moduleName, moduleConfig);
                                configuration.Save();
                                moduleManager.ApplyConfigurationChanges(configuration);
                            }
                        }
                    }
                    
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(currentEnabled ? "Disable module" : "Enable module");
                    }
                }
                
                ImGui.EndTable();
            }
            LayoutHelpers.EndSection();
        }
        
        ImGui.Spacing();
        
        // Show selected module details
        if (!string.IsNullOrEmpty(selectedModuleName))
        {
            if (LayoutHelpers.BeginSection($"Selected Module: {selectedModuleName}"))
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
                LayoutHelpers.EndSection();
            }
            
            ImGui.Spacing();
        }
        
        if (LayoutHelpers.BeginSection("Module Dependencies"))
        {
            ImGui.TextWrapped("Some modules depend on others. Disabling a module will also disable " +
                            "all modules that depend on it.");
            
            ImGui.Spacing();
            
            // Show the dependency tree
            foreach (var kvp in moduleManager.Registry.ModuleInfos)
            {
                if (kvp.Value.Dependencies.Length == 0) continue;
                
                ImGui.Text($"{kvp.Key} depends on: {string.Join(", ", kvp.Value.Dependencies)}");
            }
            LayoutHelpers.EndSection();
        }
        
        // Dependency warning popup modal
        var popupOpen = true;
        using (var disableWarningPopup = 
               ImRaii.PopupModal("Disable Module Warning", ref popupOpen, 
                                 ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            if (disableWarningPopup)
            {
                ImGui.SetWindowSize(new Vector2(400, 0));
                
                ImGui.Text($"Warning: Disabling {moduleToDisable} will also disable:");
                ImGui.Spacing();
                
                // Create a child region for the dependent list to ensure proper scrolling if needed
                using (ImRaii.Child("DependentsList", new Vector2(0, Math.Min(affectedDependents.Count * 25, 150)), true))
                {
                    foreach (var dependent in affectedDependents)
                    {
                        ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), $"  • {dependent}");
                    }
                }
                
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                
                ImGui.Text("Do you want to continue?");
                ImGui.Spacing();
                
                var buttonWidth = 120f;
                var spacing = 10f;
                var totalWidth = buttonWidth * 2 + spacing;
                var startX = (ImGui.GetContentRegionAvail().X - totalWidth) / 2;
                
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + startX);
                
                if (ImGui.Button("Yes, Disable All", new Vector2(buttonWidth, 0)))
                {
                    // Disable the module and all its dependents
                    var moduleConfig = configuration.GetModuleConfig(moduleToDisable);
                    moduleConfig.IsEnabled = false;
                    configuration.SetModuleConfig(moduleToDisable, moduleConfig);
                    
                    // Also disable all dependent modules
                    foreach (var dependent in affectedDependents)
                    {
                        var depConfig = configuration.GetModuleConfig(dependent);
                        depConfig.IsEnabled = false;
                        configuration.SetModuleConfig(dependent, depConfig);
                    }
                    
                    configuration.Save();
                    moduleManager.ApplyConfigurationChanges(configuration);
                    ImGui.CloseCurrentPopup();
                    moduleToDisable = string.Empty;
                    affectedDependents.Clear();
                }
                
                ImGui.SameLine();
                
                if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0)))
                {
                    ImGui.CloseCurrentPopup();
                    moduleToDisable = string.Empty;
                    affectedDependents.Clear();
                }
            }
        }
        
        // Dependency error popup for enabling
        using var popup = ImRaii.Popup("EnableDependencyError");
        if (popup)
        {
            ImGui.Text("Cannot enable this module because its dependencies are not enabled.");
            ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), "Please enable the required dependencies first.");
                
            if (ImGui.Button("OK"))
            {
                ImGui.CloseCurrentPopup();
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
