using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using SamplePlugin.Core.Reactive;
using SamplePlugin.Core.Configuration;

namespace SamplePlugin.Core.Module;

public class ModuleManager(IServiceProvider globalServices, IPluginLog logger) : IDisposable
{
    private readonly List<IModule> modules = [];
    private readonly Dictionary<string, IServiceProvider> moduleServices = new();
    private ModuleRegistry? registry;
    
    public IReadOnlyList<IModule> LoadedModules => modules.AsReadOnly();
    public ModuleRegistry Registry => registry ??= new ModuleRegistry(logger);

    public void LoadModule<T>() where T : IModule, new()
    {
        var module = new T();
        LoadModule(module);
    }
    
    public void LoadModule(IModule module)
    {
        ArgumentNullException.ThrowIfNull(module);

        try
        {
            if (modules.Any(m => m.Name == module.Name))
            {
                logger.Warning($"Module {module.Name} is already loaded");
                return;
            }
            
            foreach (var dependency in module.Dependencies)
            {
                if (modules.All(m => m.Name != dependency))
                {
                    logger.Error($"Module {module.Name} requires {dependency} which is not loaded");
                    throw new InvalidOperationException($"Module {module.Name} requires {dependency} which is not loaded");
                }
            }
            
            var services = new ServiceCollection();
            
            // Register known services from the global service provider
            services.AddSingleton(globalServices);
            services.AddSingleton(globalServices.GetRequiredService<IPluginLog>());
            services.AddSingleton(globalServices.GetRequiredService<EventBus>());
            services.AddSingleton(globalServices.GetRequiredService<PluginConfiguration>());
            
            // Register optional services if they exist
            var pluginInterface = globalServices.GetService<IDalamudPluginInterface>();
            if (pluginInterface != null) services.AddSingleton(pluginInterface);
            
            var commandManager = globalServices.GetService<ICommandManager>();
            if (commandManager != null) services.AddSingleton(commandManager);
            
            var chatGui = globalServices.GetService<IChatGui>();
            if (chatGui != null) services.AddSingleton(chatGui);
            
            var windowSystem = globalServices.GetService<WindowSystem>();
            if (windowSystem != null) services.AddSingleton(windowSystem);
            
            module.RegisterServices(services);
            
            var moduleProvider = services.BuildServiceProvider();
            moduleServices[module.Name] = moduleProvider;
            
            if (module is ModuleBase moduleBase)
            {
                moduleBase.InjectDependencies(moduleProvider);
            }
            
            module.Initialize();
            modules.Add(module);
            
            logger.Information($"Loaded module: {module.Name} v{module.Version}");
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Failed to load module: {module.Name}");
            throw;
        }
    }
    
    public void UnloadModule(string moduleName)
    {
        var module = modules.FirstOrDefault(m => m.Name == moduleName);
        if (module == null) return;
        
        var dependents = modules.Where(m => m.Dependencies.Contains(moduleName)).ToList();
        foreach (var dependent in dependents)
        {
            UnloadModule(dependent.Name);
        }
        
        module.Dispose();
        modules.Remove(module);
        
        if (moduleServices.TryGetValue(moduleName, out var provider))
        {
            if (provider is IDisposable disposable)
                disposable.Dispose();
            moduleServices.Remove(moduleName);
        }
        
        logger.Information($"Unloaded module: {moduleName}");
    }
    
    public void DrawUI()
    {
        foreach (var module in modules)
        {
            try
            {
                module.DrawUI();
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error drawing UI for module: {module.Name}");
            }
        }
    }
    
    public void DrawConfiguration()
    {
        foreach (var module in modules)
        {
            try
            {
                module.DrawConfiguration();
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error drawing configuration for module: {module.Name}");
            }
        }
    }
    
    /// <summary>
    /// Discovers and loads all registered modules
    /// </summary>
    public void LoadAllRegisteredModules(PluginConfiguration configuration)
    {
        Registry.DiscoverModules();
        
        if (!Registry.ValidateDependencies())
        {
            logger.Warning("Some module dependencies are not satisfied");
        }
        
        var modulesToLoad = Registry.GetModulesInLoadOrder();
        
        foreach (var moduleName in modulesToLoad)
        {
            var moduleConfig = configuration.GetModuleConfig(moduleName);
            
            // Check if the module should be loaded based on configuration
            if (!moduleConfig.IsEnabled)
            {
                logger.Information($"Skipping disabled module: {moduleName}");
                continue;
            }
            
            var module = Registry.CreateModuleInstance(moduleName);
            if (module != null)
            {
                try
                {
                    LoadModule(module);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Failed to load module: {moduleName}");
                }
            }
        }
    }
    
    /// <summary>
    /// Gets module info for a loaded module
    /// </summary>
    public ModuleInfoAttribute? GetModuleInfo(string moduleName)
    {
        return Registry.ModuleInfos.GetValueOrDefault(moduleName);
    }
    
    public void Dispose()
    {
        foreach (var module in modules.ToList())
        {
            UnloadModule(module.Name);
        }
        GC.SuppressFinalize(this);   
    }
}
