using System.Windows;
using System.Windows.Data;
using EvenniaAtlas.Models;
using EvenniaAtlas.ViewModels;

namespace EvenniaAtlas.Views;

public partial class LootTableEditorWindow : Window
{
    private LootTableEditorViewModel ViewModel => (LootTableEditorViewModel)DataContext;

    public LootTableEditorWindow(MapProject project, Action markDirtyCallback)
    {
        InitializeComponent();
        var vm = new LootTableEditorViewModel();
        vm.MarkDirtyCallback = markDirtyCallback;
        vm.SetProject(project);
        DataContext = vm;

        // Any TwoWay binding edit marks the project dirty — even without changing selection
        AddHandler(Binding.SourceUpdatedEvent,
            new EventHandler<DataTransferEventArgs>((_, _) => vm.MarkDirtyCallback?.Invoke()),
            handledEventsToo: true);
    }
}