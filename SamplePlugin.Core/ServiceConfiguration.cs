using Microsoft.Extensions.DependencyInjection;
using SamplePlugin.Core.Application;

namespace SamplePlugin.Core;

public static class ServiceConfiguration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<MainWindowViewModel>();
    }
}