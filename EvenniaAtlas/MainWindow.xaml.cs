using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EvenniaAtlas.Models;
using EvenniaAtlas.ViewModels;

namespace EvenniaAtlas;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;
    private bool _suppressComboEvents;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var p = e.PropertyName;
        if (p == nameof(MainViewModel.SelectedConnectionDirection) && !_suppressComboEvents)
            SyncDirectionCombo();
    }

    private void SyncDirectionCombo()
    {
        _suppressComboEvents = true;
        var dn = ViewModel.SelectedConnectionDirection.ToString();
        foreach (ComboBoxItem item in DirectionCombo.Items)
            if (item.Tag is string t && t == dn) { DirectionCombo.SelectedItem = item; break; }
        _suppressComboEvents = false;
    }

    private void DirectionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressComboEvents || ViewModel.SelectedConnection == null) return;
        if (DirectionCombo.SelectedItem is ComboBoxItem item && item.Tag is string dn)
            if (System.Enum.TryParse<Direction>(dn, out var d)) ViewModel.SelectedConnectionDirection = d;
    }

    private void ExitTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressComboEvents || ViewModel.SelectedConnection == null) return;
        if (ExitTypeCombo.SelectedItem is ComboBoxItem item && item.Content is string tn)
            if (System.Enum.TryParse<ExitType>(tn, out var et)) ViewModel.SelectedConnectionExitType = et;
    }

    private void ConnectionListItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border b && b.DataContext is ConnectionModel conn) ViewModel.SelectedConnection = conn;
    }

    private void Window_Closing(object sender, CancelEventArgs e) { if (!ViewModel.ConfirmClose()) e.Cancel = true; }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control) { ViewModel.SaveProject(); e.Handled = true; }
        else if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control) { ViewModel.Undo(); MapCanvasControl.RefreshMap(); e.Handled = true; }
        else if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control) { ViewModel.Redo(); MapCanvasControl.RefreshMap(); e.Handled = true; }
    }

    // Toolbar handlers
    private void New_Click(object s, RoutedEventArgs e) => ViewModel.NewProject();
    private void Open_Click(object s, RoutedEventArgs e) => ViewModel.OpenProject();
    private void Save_Click(object s, RoutedEventArgs e) => ViewModel.SaveProject();
    private void SaveAs_Click(object s, RoutedEventArgs e) => ViewModel.SaveProjectAs();
    private void Export_Click(object s, RoutedEventArgs e) => ViewModel.ExportEvennia();
    private void BuildMode_Click(object s, RoutedEventArgs e) => ViewModel.BuildMode = !ViewModel.BuildMode;
    private void AutoReverse_Click(object s, RoutedEventArgs e) => ViewModel.AutoReverse = !ViewModel.AutoReverse;
    private void FloorUp_Click(object s, RoutedEventArgs e) => ViewModel.CurrentZ++;
    private void FloorDown_Click(object s, RoutedEventArgs e) => ViewModel.CurrentZ--;
    private void Delete_Click(object s, RoutedEventArgs e) { ViewModel.DeleteSelected(); MapCanvasControl.RefreshMap(); }
    private void Undo_Click(object s, RoutedEventArgs e) { ViewModel.Undo(); MapCanvasControl.RefreshMap(); }
    private void Redo_Click(object s, RoutedEventArgs e) { ViewModel.Redo(); MapCanvasControl.RefreshMap(); }
}

