using System.Windows;
using System.Windows.Data;
using EvenniaAtlas.Models;
using EvenniaAtlas.ViewModels;

namespace EvenniaAtlas.Views;

public partial class ShopEditorWindow : Window
{
    private ShopEditorViewModel ViewModel => (ShopEditorViewModel)DataContext;

    public ShopEditorWindow(MapProject project, Action markDirtyCallback)
    {
        InitializeComponent();
        var vm = new ShopEditorViewModel();
        vm.MarkDirtyCallback = markDirtyCallback;
        vm.SetProject(project);
        DataContext = vm;

        // Any TwoWay binding edit marks the project dirty — even without changing selection
        AddHandler(Binding.SourceUpdatedEvent,
            new EventHandler<DataTransferEventArgs>((_, _) => vm.MarkDirtyCallback?.Invoke()),
            handledEventsToo: true);
    }

    private void ClearOwner_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedShop != null)
            ViewModel.SelectedShop.NpcId = string.Empty;
    }
}