using System.Windows;
using System.Windows.Controls;
using EvenniaAtlas.Models;
using EvenniaAtlas.ViewModels;

namespace EvenniaAtlas.Views;

public partial class ItemEditorWindow : Window
{
    private ItemEditorViewModel ViewModel => (ItemEditorViewModel)DataContext;

    public ItemEditorWindow(MapProject project, Action markDirtyCallback)
    {
        InitializeComponent();
        var vm = new ItemEditorViewModel();
        vm.MarkDirtyCallback = markDirtyCallback;
        vm.SetProject(project);
        DataContext = vm;
    }

    private void ItemTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel?.SelectedItem != null)
            ViewModel.OnItemTypeChanged();
    }
}