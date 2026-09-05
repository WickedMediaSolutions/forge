using System.Windows;
using System.Windows.Controls;
using EvenniaMapMaker.Models;

namespace EvenniaMapMaker.Dialogs;

public partial class DirectionChooserDialog : Window
{
    public Direction SelectedDirection { get; private set; }

    public DirectionChooserDialog()
    {
        InitializeComponent();
    }

    private void Direction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string dirStr)
        {
            SelectedDirection = Enum.Parse<Direction>(dirStr);
            DialogResult = true;
            Close();
        }
    }
}