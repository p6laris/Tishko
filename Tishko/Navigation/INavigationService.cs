using System.ComponentModel;

namespace Tishko.Navigation;

public interface INavigationService : INotifyPropertyChanged
{
    object? CurrentViewModel { get; }
    bool CanGoBack { get; }

    void Navigate(PageRoute route, NavigationArgs args = null);
    void GoBack();
    void ClearHistory();
}
