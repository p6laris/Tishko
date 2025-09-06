using System;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Tishko.ViewModels.Controls;

namespace Tishko.Controls;

public partial class SessionPlayer : UserControl
{
    private readonly SessionPlayerViewModel viewModel;
    public SessionPlayer()
    {
        InitializeComponent();
        viewModel = App.Services.GetRequiredService<SessionPlayerViewModel>();
        DataContext = viewModel;
    }
}