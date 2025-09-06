using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Tishko.ViewModels.Controls;

public partial class SessionPlayerViewModel : ObservableObject
{
    private readonly DispatcherTimer _timer;

    private TimeSpan _elapsed;
    private bool _isRunning;

    // This property will be generated with INotifyPropertyChanged support
    [ObservableProperty]
    private string elapsedFormatted = "00:00:00:00";

    public SessionPlayerViewModel()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += (_, _) =>
        {
            _elapsed = _elapsed.Add(_timer.Interval);
            ElapsedFormatted = $"{_elapsed.Hours:D2}:{_elapsed.Minutes:D2}:{_elapsed.Seconds:D2}:{_elapsed.Milliseconds / 10:D2}";
        };
    }

    [RelayCommand]
    private void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _timer.Start();
    }

    [RelayCommand]
    private void Pause()
    {
        _isRunning = false;
        _timer.Stop();
    }

    [RelayCommand]
    private void Stop()
    {
        _isRunning = false;
        _timer.Stop();
        _elapsed = TimeSpan.Zero;
        ElapsedFormatted = "00:00:00:00";
    }
}