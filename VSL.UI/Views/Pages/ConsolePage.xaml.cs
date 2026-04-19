using System.Windows.Controls;
using System.Collections.Specialized;
using System.Windows;
using VSL.UI.ViewModels;

namespace VSL.UI.Views.Pages;

public partial class ConsolePage : UserControl
{
    private MainViewModel? _boundViewModel;

    public ConsolePage()
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
        _boundViewModel.ConsoleLines.CollectionChanged += OnConsoleLinesChanged;
    }

    private void DetachFromViewModel()
    {
        if (_boundViewModel is null)
        {
            return;
        }

        _boundViewModel.ConsoleLines.CollectionChanged -= OnConsoleLinesChanged;
        _boundViewModel = null;
    }

    private void OnConsoleLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_boundViewModel?.IsConsoleAutoFollow != true)
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            if (ConsoleList.Items.Count > 0)
            {
                ConsoleList.ScrollIntoView(ConsoleList.Items[ConsoleList.Items.Count - 1]);
            }
        });
    }
}
