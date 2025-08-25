namespace Tishko.Navigation;

public interface INavigationAware
{
    void OnNavigatedTo(NavigationArgs? args);
    void OnNavigatedFrom();
}
