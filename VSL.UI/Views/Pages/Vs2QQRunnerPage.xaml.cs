using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using VSL.UI.ViewModels;

namespace VSL.UI.Views.Pages;

public partial class Vs2QQRunnerPage : UserControl
{
    private MainViewModel? _boundViewModel;

    public Vs2QQRunnerPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachToViewModel(DataContext as MainViewModel);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachFromViewModel();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        DetachFromViewModel();
        AttachToViewModel(e.NewValue as MainViewModel);
    }

    private void AttachToViewModel(MainViewModel? vm)
    {
        if (vm is null)
        {
            return;
        }

        _boundViewModel = vm;
        _boundViewModel.Vs2QQConsoleLines.CollectionChanged += OnConsoleLinesChanged;
    }

    private void DetachFromViewModel()
    {
        if (_boundViewModel is null)
        {
            return;
        }

        _boundViewModel.Vs2QQConsoleLines.CollectionChanged -= OnConsoleLinesChanged;
        _boundViewModel = null;
    }

    private void OnConsoleLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_boundViewModel?.IsVs2QQConsoleAutoFollow != true)
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            if (Vs2QQConsoleList.Items.Count > 0)
            {
                Vs2QQConsoleList.ScrollIntoView(Vs2QQConsoleList.Items[Vs2QQConsoleList.Items.Count - 1]);
            }
        });
    }
}
