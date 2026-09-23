using System.Windows;
using System.Windows.Data;
using EvenniaAtlas.Models;
using EvenniaAtlas.ViewModels;

namespace EvenniaAtlas.Views;

public partial class GameDataEditorWindow : Window
{
    public GameDataEditorWindow(MapProject project, Action markDirtyCallback)
    {
        InitializeComponent();
        var vm = new GameDataViewModel();
        vm.MarkDirtyCallback = markDirtyCallback;
        vm.SetProject(project);
        DataContext = vm;

        // Any TwoWay binding edit marks the project dirty — even without changing selection
        AddHandler(Binding.SourceUpdatedEvent,
            new EventHandler<DataTransferEventArgs>((_, _) => vm.MarkDirtyCallback?.Invoke()),
            handledEventsToo: true);
    }
}