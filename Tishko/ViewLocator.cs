using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using StaticViewLocator;
using Tishko.ViewModels;

namespace Tishko;

[StaticViewLocator]
public partial class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var type = param.GetType();

        if (s_views.TryGetValue(type, out var factory))
        {
            var view = factory();
            view.DataContext = param;
            return view;
        }

        return new TextBlock { Text = $"Not Found: {type.FullName}" };
    }

    public bool Match(object? data) => data is ViewModelBase;
}