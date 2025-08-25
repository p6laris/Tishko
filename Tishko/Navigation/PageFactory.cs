using System;
using Microsoft.Extensions.DependencyInjection;
using Tishko.ViewModels.Pages;

namespace Tishko.Navigation;

public sealed class PageFactory(IServiceProvider services) : IPageFactory
{
    public object Create(PageRoute route) => route switch
    {
        PageRoute.Home => services.GetRequiredService<HomeViewModel>(),
        PageRoute.Settings => services.GetRequiredService<SettingsViewModel>(),
        PageRoute.Statistics => services.GetRequiredService<StatisticsViewModel>(),
        _ => throw new InvalidOperationException($"No page registered for route '{route}'.")
    };
}
