using System.Windows;

namespace EvenniaMapMaker.Dialogs;

public partial class NewProjectDialog : Window
{
    public string AreaName => AreaNameBox.Text.Trim();
    public string AreaId => AreaIdBox.Text.Trim();

    public NewProjectDialog()
    {
        InitializeComponent();
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AreaName) || string.IsNullOrWhiteSpace(AreaId))
        {
            MessageBox.Show("Please enter both Area Name and Area ID.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}