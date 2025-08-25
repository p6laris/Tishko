using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tishko.Navigation;

public sealed class NavigationService(IPageFactory factory) : INavigationService
{
    private readonly Stack<object> _history = new();
    private object? _current;

    public event PropertyChangedEventHandler? PropertyChanged;

    public object? CurrentViewModel
    {
        get => _current;
        private set
        {
            if (!ReferenceEquals(_current, value))
            {
                _current = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanGoBack));
            }
        }
    }

    public bool CanGoBack => _history.Count > 0;

    public void Navigate(PageRoute route, NavigationArgs? args = null)
    {
        if (_current is INavigationAware leaving)
            leaving.OnNavigatedFrom();

        if (_current is not null && args?.ResetStack != true)
            _history.Push(_current);

        if (args?.ResetStack == true)
            _history.Clear();

        var vm = factory.Create(route);

        if (vm is INavigationAware entering)
            entering.OnNavigatedTo(args);

        CurrentViewModel = vm; // triggers PropertyChanged
    }

    public void GoBack()
    {
        if (!CanGoBack) return;

        if (_current is INavigationAware leaving)
            leaving.OnNavigatedFrom();

        var vm = _history.Pop();

        if (vm is INavigationAware entering)
            entering.OnNavigatedTo(null);

        CurrentViewModel = vm;
    }

    public void ClearHistory()
    {
        _history.Clear();
        OnPropertyChanged(nameof(CanGoBack));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

