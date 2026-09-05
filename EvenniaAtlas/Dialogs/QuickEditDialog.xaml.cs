using System.Windows;

namespace EvenniaAtlas.Dialogs;

public partial class QuickEditDialog : Window
{
    public string ResultText => EditBox.Text;

    public QuickEditDialog(string fieldName, string currentValue, bool multiLine)
    {
        InitializeComponent();
        Title = $"Edit {fieldName}";
        FieldLabel.Text = $"{fieldName}:";
        EditBox.Text = currentValue ?? "";
        if (!multiLine)
        {
            EditBox.AcceptsReturn = false;
            EditBox.TextWrapping = TextWrapping.NoWrap;
            EditBox.Height = 28;
        }
        EditBox.Focus();
        EditBox.SelectAll();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}