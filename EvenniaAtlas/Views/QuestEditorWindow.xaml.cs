using System.Windows;
using System.Windows.Data;
using EvenniaAtlas.Models;
using EvenniaAtlas.ViewModels;

namespace EvenniaAtlas.Views;

public partial class QuestEditorWindow : Window
{
    private QuestEditorViewModel ViewModel => (QuestEditorViewModel)DataContext;

    public QuestEditorWindow(MapProject project, Action markDirtyCallback)
    {
        InitializeComponent();
        var vm = new QuestEditorViewModel();
        vm.MarkDirtyCallback = markDirtyCallback;
        vm.SetProject(project);
        DataContext = vm;

        // Any TwoWay binding edit marks the project dirty — even without changing selection
        AddHandler(Binding.SourceUpdatedEvent,
            new EventHandler<DataTransferEventArgs>((_, _) => vm.MarkDirtyCallback?.Invoke()),
            handledEventsToo: true);
    }
}