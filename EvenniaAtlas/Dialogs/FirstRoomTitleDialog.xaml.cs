using System.Windows;

namespace EvenniaAtlas.Dialogs;

public partial class FirstRoomTitleDialog : Window
{
    public string RoomTitle => TitleBox.Text.Trim().Length > 0 ? TitleBox.Text.Trim() : "Room";

    public FirstRoomTitleDialog(string currentDefault)
    {
        InitializeComponent();
        TitleBox.Text = string.IsNullOrWhiteSpace(currentDefault) ? "Room" : currentDefault;
        TitleBox.Focus();
        TitleBox.SelectAll();
    }

    private void Create_Click(object sender, RoutedEventArgs e)
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