using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tishko.Navigation;

namespace Tishko.ViewModels.Windows;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService  _navigationService;

    public MainWindowViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        _navigationService.PropertyChanged += OnNavigationChanged;
        _navigationService.Navigate(PageRoute.Home);
    }
    private void OnNavigationChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(INavigationService.CurrentViewModel))
            OnPropertyChanged(nameof(CurrentViewModel));
        else if (e.PropertyName is nameof(INavigationService.CanGoBack))
            OnPropertyChanged(nameof(CanGoBack));
    } 
    public object? CurrentViewModel => _navigationService.CurrentViewModel;
    public bool CanGoBack => _navigationService.CanGoBack;

    [RelayCommand] public void NavigateHome()
        => _navigationService.Navigate(PageRoute.Home);

    [RelayCommand] public void NavigateSettings()
        => _navigationService.Navigate(PageRoute.Settings);

    [RelayCommand] public void NavigateStatistics()
        => _navigationService.Navigate(PageRoute.Statistics);
    [RelayCommand] public void GoBack()
        => _navigationService.GoBack();
}