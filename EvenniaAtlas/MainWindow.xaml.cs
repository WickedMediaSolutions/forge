using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
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
        SetWindowIcon();
        DataContext = new MainViewModel();
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void SetWindowIcon()
    {
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "the-forge-logo-ico.png");
            if (File.Exists(iconPath))
                Icon = BitmapFrame.Create(new Uri(iconPath));
        }
        catch
        {
            // Icon is non-critical; continue without it
        }
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

    // Evennia room metadata handlers
    private void AddRoomAlias_Click(object sender, RoutedEventArgs e) => ViewModel.AddRoomAlias();
    private void RemoveRoomAlias_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is AliasModel alias) ViewModel.RemoveRoomAlias(alias);
    }

    private void AddRoomEvenniaTag_Click(object sender, RoutedEventArgs e) => ViewModel.AddRoomEvenniaTag();
    private void RemoveRoomEvenniaTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is TagModel tag) ViewModel.RemoveRoomEvenniaTag(tag);
    }

    private void AddRoomAttribute_Click(object sender, RoutedEventArgs e) => ViewModel.AddRoomAttribute();
    private void RemoveRoomAttribute_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is AttributeModel attr) ViewModel.RemoveRoomAttribute(attr);
    }

    private void AddRoomPermission_Click(object sender, RoutedEventArgs e) => ViewModel.AddRoomPermission();
    private void RemoveRoomPermission_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is string perm) ViewModel.RemoveRoomPermission(perm);
    }
// Evennia connection metadata handlers
    private void AddConnectionAlias_Click(object sender, RoutedEventArgs e) => ViewModel.AddConnectionAlias();
    private void RemoveConnectionAlias_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is AliasModel alias) ViewModel.RemoveConnectionAlias(alias);
    }

    private void AddConnectionTag_Click(object sender, RoutedEventArgs e) => ViewModel.AddConnectionTag();
    private void RemoveConnectionTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is TagModel tag) ViewModel.RemoveConnectionTag(tag);
    }

    private void AddConnectionAttribute_Click(object sender, RoutedEventArgs e) => ViewModel.AddConnectionAttribute();
    private void RemoveConnectionAttribute_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is AttributeModel attr) ViewModel.RemoveConnectionAttribute(attr);
    }

    private void AddConnectionPermission_Click(object sender, RoutedEventArgs e) => ViewModel.AddConnectionPermission();
    private void RemoveConnectionPermission_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is string perm) ViewModel.RemoveConnectionPermission(perm);
    }

    // Evennia door metadata handlers
    private void AddDoorAlias_Click(object sender, RoutedEventArgs e) => ViewModel.AddDoorAlias();
    private void RemoveDoorAlias_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is AliasModel alias) ViewModel.RemoveDoorAlias(alias);
    }

    private void AddDoorTag_Click(object sender, RoutedEventArgs e) => ViewModel.AddDoorTag();
    private void RemoveDoorTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is TagModel tag) ViewModel.RemoveDoorTag(tag);
    }

    private void AddDoorAttribute_Click(object sender, RoutedEventArgs e) => ViewModel.AddDoorAttribute();
    private void RemoveDoorAttribute_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is AttributeModel attr) ViewModel.RemoveDoorAttribute(attr);
    }

    private void AddDoorPermission_Click(object sender, RoutedEventArgs e) => ViewModel.AddDoorPermission();
    private void RemoveDoorPermission_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.DataContext is string perm) ViewModel.RemoveDoorPermission(perm);
    }

    private void DoorPermissionTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        if (tb.DataContext is not string originalValue) return;
        var perms = ViewModel.DoorPermissions;
        if (perms == null) return;

        var idx = FindPermissionIndex(perms, originalValue);
        if (idx < 0) return;

        string newValue = tb.Text ?? string.Empty;
        if (newValue == originalValue) return;

        perms[idx] = newValue;
        ViewModel.MarkDirtyPublic();
    }

    private void ConnectionPermissionTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        if (tb.DataContext is not string originalValue) return;
        var perms = ViewModel.ConnectionPermissions;
        if (perms == null) return;

        var idx = FindPermissionIndex(perms, originalValue);
        if (idx < 0) return;

        string newValue = tb.Text ?? string.Empty;
        if (newValue == originalValue) return;

        perms[idx] = newValue;
        ViewModel.MarkDirtyPublic();
    }

    private void PermissionTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox tb) return;
        if (tb.DataContext is not string originalValue) return;
        var perms = ViewModel.RoomPermissions;
        if (perms == null) return;

        var idx = FindPermissionIndex(perms, originalValue);
        if (idx < 0) return;

        string newValue = tb.Text ?? string.Empty;
        if (newValue == originalValue) return;

        perms[idx] = newValue;
        ViewModel.MarkDirtyPublic();
    }

    private static int FindPermissionIndex(ObservableCollection<string> perms, string target)
    {
        for (int i = 0; i < perms.Count; i++)
            if (ReferenceEquals(perms[i], target)) return i;
        return -1;
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
    private void Exit_Click(object s, RoutedEventArgs e) => Close();
    private void Validate_Click(object s, RoutedEventArgs e) => ViewModel.ValidateProject();
    private void ZoomIn_Click(object s, RoutedEventArgs e) => ViewModel.ZoomIn();
    private void ZoomOut_Click(object s, RoutedEventArgs e) => ViewModel.ZoomOut();
    private void Center_Click(object s, RoutedEventArgs e) => MapCanvasControl.CenterSelectedRoom();
    private void Fit_Click(object s, RoutedEventArgs e) => MapCanvasControl.FitMap();

    // Help menu handlers
    private void HelpHowToUse_Click(object s, RoutedEventArgs e)
    {
        var dialog = new Dialogs.HelpManualWindow { Owner = this };
        dialog.ShowDialog();
    }

    private void HelpGitHub_Click(object s, RoutedEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
            "https://github.com/dirtysouthjosh/evennia-atlas")
            { UseShellExecute = true });
    }

    private void HelpAbout_Click(object s, RoutedEventArgs e)
    {
        var dialog = new Dialogs.AboutDialog { Owner = this };
        dialog.ShowDialog();
    }

    private void ItemEditor_Click(object s, RoutedEventArgs e)
    {
        var editor = new Views.ItemEditorWindow(ViewModel.CurrentProject, ViewModel.MarkDirtyPublic);
        editor.Owner = this;
        editor.Show();
    }

    private void NpcEditor_Click(object s, RoutedEventArgs e)
    {
        var editor = new Views.NpcEditorWindow(ViewModel.CurrentProject, ViewModel.MarkDirtyPublic);
        editor.Owner = this;
        editor.Show();
    }

    private void ShopEditor_Click(object s, RoutedEventArgs e)
    {
        var editor = new Views.ShopEditorWindow(ViewModel.CurrentProject, ViewModel.MarkDirtyPublic);
        editor.Owner = this;
        editor.Show();
    }

    private void LootTableEditor_Click(object s, RoutedEventArgs e)
    {
        var editor = new Views.LootTableEditorWindow(ViewModel.CurrentProject, ViewModel.MarkDirtyPublic);
        editor.Owner = this;
        editor.Show();
    }

    private void QuestEditor_Click(object s, RoutedEventArgs e)
    {
        var editor = new Views.QuestEditorWindow(ViewModel.CurrentProject, ViewModel.MarkDirtyPublic);
        editor.Owner = this;
        editor.Show();
    }

    private void DialogueEditor_Click(object s, RoutedEventArgs e)
    {
        var editor = new Views.DialogueEditorWindow(ViewModel.CurrentProject, ViewModel.MarkDirtyPublic);
        editor.Owner = this;
        editor.Show();
    }

    private void GameDataEditor_Click(object s, RoutedEventArgs e)
    {
        var editor = new Views.GameDataEditorWindow(ViewModel.CurrentProject, ViewModel.MarkDirtyPublic);
        editor.Owner = this;
        editor.Show();
    }
}

