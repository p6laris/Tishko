using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Tishko.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Orb.Start();
    }

    private void Button_OnClick1(object? sender, RoutedEventArgs e)
    {
        Orb.Stop();
    }
}