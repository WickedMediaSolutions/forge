using System.Windows;

namespace EvenniaAtlas.Dialogs;

public partial class HelpManualWindow : Window
{
    public HelpManualWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}