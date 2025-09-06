using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Tishko.Views.Windows;

public partial class MainWindowView : Window
{
    public MainWindowView()
    {
        InitializeComponent();
#if DEBUG
        // Attach DevTools and set shortcut to Ctrl+Q
        this.AttachDevTools(new KeyGesture(Key.Q, KeyModifiers.Control));
#endif 
    }
}