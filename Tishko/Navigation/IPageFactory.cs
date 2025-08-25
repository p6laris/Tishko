namespace Tishko.Navigation;

public interface IPageFactory
{
    object Create(PageRoute route);
}