using System.Collections.Generic;
using System.Windows;
using EvenniaAtlas.Models;

namespace EvenniaAtlas.Dialogs;

public partial class RoomPickerDialog : Window
{
    public RoomModel? SelectedRoom { get; private set; }

    public RoomPickerDialog(List<RoomModel> rooms)
    {
        InitializeComponent();
        RoomList.ItemsSource = rooms;
    }

    private void RoomList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        SelectedRoom = RoomList.SelectedItem as RoomModel;
        OkBtn.IsEnabled = SelectedRoom != null;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedRoom != null)
        {
            DialogResult = true;
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}