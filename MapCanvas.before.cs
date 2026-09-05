
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using EvenniaMapMaker.Models;
using EvenniaMapMaker.ViewModels;

namespace EvenniaMapMaker.Controls;

public partial class MapCanvas : UserControl
{
    public const int CellW = 150;
    public const int CellH = 110;
    public const int RoomW = 134;
    public const int RoomH = 80;
    private const int MapSize = 2000;

    private MainViewModel? _vm;
public MapCanvas() { InitializeComponent(); Loaded += OnLoaded; }

    private void OnLoaded(object s, RoutedEventArgs e)
    {
        _vm = DataContext as MainViewModel;
        if (_vm != null)
        {
            _vm.MapNeedsRefresh += RefreshMap;
            _vm.PropertyChanged += (_, a) => { if (a.PropertyName == nameof(MainViewModel.BuildMode)) RefreshMap(); };
        }
        ScrollViewer.Focus();
    }

    public void RefreshMap()
    {
        RoomsLayer.Children.Clear();
        ConnectionsLayer.Children.Clear();
        if (_vm == null) return;
        DrawGrid();
        int cz = _vm.CurrentZ;

        // Connections (same-Z only, no Up/Down)
        foreach (var conn in _vm.Connections)
        {
            var src = _vm.Rooms.FirstOrDefault(r => r.Id == conn.SourceRoomId);
            var dst = _vm.Rooms.FirstOrDefault(r => r.Id == conn.DestinationRoomId);
            if (src == null || dst == null) continue;
            if (conn.Direction is Direction.Up or Direction.Down) continue;
            if (src.Z != cz || dst.Z != cz) continue;
            bool sel = _vm.SelectedConnection?.Id == conn.Id;
            double sx = src.X * CellW + CellW/2, sy = src.Y * CellH + CellH/2;
            double dx = dst.X * CellW + CellW/2, dy = dst.Y * CellH + CellH/2;
            var line = new Line { X1 = sx, Y1 = sy, X2 = dx, Y2 = dy, Stroke = sel ? Brushes.Gold : conn.ExitType == ExitType.Door ? new SolidColorBrush(Color.FromRgb(0x8B,0x45,0x13)) : new SolidColorBrush(Color.FromRgb(0x50,0x50,0x50)), StrokeThickness = sel ? 2.5 : 1.5, StrokeDashArray = conn.ExitType == ExitType.Door ? new DoubleCollection(new[] { 4.0, 2.0 }) : null, Cursor = Cursors.Hand, Tag = conn };
            line.MouseLeftButtonDown += OnConnClick;
            ConnectionsLayer.Children.Add(line);
            var lbl = new TextBlock { Text = DirectionHelper.GetDirectionLabel(conn.Direction), Foreground = sel ? Brushes.Gold : new SolidColorBrush(Color.FromRgb(0x99,0x99,0x99)), FontSize = 9, FontWeight = FontWeights.Bold, Background = new SolidColorBrush(Color.FromRgb(0x2D,0x2D,0x2D)), Padding = new Thickness(2,0,2,0), Tag = conn };
            Canvas.SetLeft(lbl, (sx + dx)/2 - 8);
            Canvas.SetTop(lbl, (sy + dy)/2 - 8);
            lbl.MouseLeftButtonDown += OnConnClick;
            lbl.Cursor = Cursors.Hand;
            ConnectionsLayer.Children.Add(lbl);
        }

        // Rooms on current floor
        var floorRooms = _vm.Rooms.Where(r => r.Z == cz).ToList();
        foreach (var room in floorRooms)
        {
            bool isSel = _vm.SelectedRoom?.Id == room.Id;
            double px = room.X * CellW + (CellW - RoomW) / 2;
            double py = room.Y * CellH + (CellH - RoomH) / 2;
            var border = new Border { Width = RoomW, Height = RoomH, Background = isSel ? new SolidColorBrush(Color.FromRgb(0x30,0x50,0x80)) : _vm.BuildMode ? new SolidColorBrush(Color.FromRgb(0x2D,0x3D,0x2D)) : new SolidColorBrush(Color.FromRgb(0x3C,0x3C,0x3C)), BorderBrush = isSel ? new SolidColorBrush(Color.FromRgb(0xFF,0xD7,0x00)) : new SolidColorBrush(Color.FromRgb(0x55,0x55,0x55)), BorderThickness = new Thickness(isSel ? 2 : 1), CornerRadius = new CornerRadius(4), Tag = room, Cursor = Cursors.Hand };
            var stack = new StackPanel { Margin = new Thickness(6,4,6,4) };
            stack.Children.Add(new TextBlock { Text = room.DisplayTitle, Foreground = isSel ? Brushes.White : new SolidColorBrush(Color.FromRgb(0xDD,0xDD,0xDD)), FontSize = 11, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = RoomW - 16 });
            if (!string.IsNullOrWhiteSpace(room.RoomType)) stack.Children.Add(new Border { Background = new SolidColorBrush(Color.FromRgb(0x55,0x3C,0x00)), CornerRadius = new CornerRadius(2), Padding = new Thickness(4,1,4,1), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0,2,0,0), Child = new TextBlock { Text = room.RoomType, FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0xFF,0xCC,0x44)) } });
            bool hasUp = _vm.Connections.Any(c => c.SourceRoomId == room.Id && c.Direction == Direction.Up);
            bool hasDown = _vm.Connections.Any(c => c.SourceRoomId == room.Id && c.Direction == Direction.Down);
            if (hasUp || hasDown)
            {
                var txt = (hasUp ? "U" : "") + (hasDown ? " D" : "");
                stack.Children.Add(new TextBlock { Text = txt, FontSize = 8, Foreground = new SolidColorBrush(Color.FromRgb(0x88,0xBB,0xFF)), HorizontalAlignment = HorizontalAlignment.Right });
            }
            if (room.Z != 0) stack.Children.Add(new TextBlock { Text = "Z:" + room.Z, FontSize = 8, Foreground = new SolidColorBrush(Color.FromRgb(0x88,0x88,0x88)) });
            border.Child = stack;
            Canvas.SetLeft(border, px);
            Canvas.SetTop(border, py);
            border.MouseLeftButtonDown += OnRoomClick;
            border.MouseRightButtonDown += OnRoomRightClick;
            RoomsLayer.Children.Add(border);
        }
    }

    private void OnConnClick(object s, MouseButtonEventArgs e)
    {
        if (s is FrameworkElement el && el.Tag is ConnectionModel conn && _vm != null)
            _vm.SelectedConnection = conn;
    }

    private void OnRoomClick(object s, MouseButtonEventArgs e)
    {
        if (s is Border b && b.Tag is RoomModel room && _vm != null)
        {
            _vm.SelectedRoom = room;
            e.Handled = true;
        }
    }

    private void OnRoomRightClick(object s, MouseButtonEventArgs e)
    {
        if (s is not Border b || b.Tag is not RoomModel room || _vm == null) return;
        _vm.SelectedRoom = room; e.Handled = true;
        var menu = new ContextMenu();
        menu.Items.Add(MakeMenuItem("Edit Title...", () => { var dlg = new EvenniaMapMaker.Dialogs.QuickEditDialog("Title", room.Title, false); dlg.Owner = Window.GetWindow(this); if (dlg.ShowDialog() == true) { room.Title = dlg.ResultText; _vm.MarkDirtyPublic(); RefreshMap(); } }));
        menu.Items.Add(MakeMenuItem("Edit Description...", () => { var dlg = new EvenniaMapMaker.Dialogs.QuickEditDialog("Description", room.Description, true); dlg.Owner = Window.GetWindow(this); if (dlg.ShowDialog() == true) { room.Description = dlg.ResultText; _vm.MarkDirtyPublic(); RefreshMap(); } }));
        var exitMenu = new MenuItem { Header = "Add Exit" };
        foreach (Direction dir in Enum.GetValues<Direction>()) { var d = dir; exitMenu.Items.Add(MakeMenuItem(d.ToString(), () => _vm.CreateConnection(room, /*target picked below*/ room, d))); }
        menu.Items.Add(exitMenu);
        menu.Items.Add(new Separator());
        menu.Items.Add(MakeMenuItem("Delete Room", () => { _vm.DeleteRoom(room); RefreshMap(); }));
        menu.IsOpen = true;
    }

    private static MenuItem MakeMenuItem(string h, Action a) { var mi = new MenuItem { Header = h }; mi.Click += (_, _) => a(); return mi; }

    private void OnMapMouseDown(object s, MouseButtonEventArgs e)
    {
        if (_vm == null) return;
        if (e.OriginalSource != MapSurface && e.OriginalSource != GridLayer && e.OriginalSource != ConnectionsLayer) return;
        _vm.SelectedRoom = null; _vm.SelectedConnection = null;
        var pos = e.GetPosition(MapSurface);
        int gx = (int)Math.Floor(pos.X / CellW);
        int gy = (int)Math.Floor(pos.Y / CellH);
        _vm.CreateRoomAt(gx, gy, _vm.CurrentZ);
    }

    private void OnMapWheel(object s, MouseWheelEventArgs e)
    {
        if (_vm == null) return;
        if (e.Delta > 0) _vm.CurrentZ++; else _vm.CurrentZ--;
        e.Handled = true;
    }

    private void OnKeyDown(object s, KeyEventArgs e)
    {
        if (_vm == null) return;
        if (e.Key == Key.Delete) { _vm.DeleteSelected(); RefreshMap(); e.Handled = true; }
        else if (e.Key == Key.Escape) { _vm.SelectedRoom = null; _vm.SelectedConnection = null; RefreshMap(); e.Handled = true; }
        else
        {
            var dir = DirectionHelper.KeyToDirection(e.Key);
            if (dir.HasValue) { _vm.NavigateOrBuild(dir.Value); e.Handled = true; }
        }
    }

    private void ApplyZoom() { }

    private void DrawGrid()
    {
        GridLayer.Children.Clear();
        var alt = new SolidColorBrush(Color.FromRgb(0x2A,0x2A,0x2E));
        var ln = new SolidColorBrush(Color.FromRgb(0x3E,0x3E,0x3E));
        int max = MapSize * CellW;
        for (int x = 0; x < max; x += CellW)
            for (int y = 0; y < max; y += CellH)
                if ((x/CellW + y/CellH) % 2 == 0)
                {
                    var r = new Rectangle { Width = CellW, Height = CellH, Fill = alt, IsHitTestVisible = false };
                    Canvas.SetLeft(r, x); Canvas.SetTop(r, y); GridLayer.Children.Add(r);
                }
        for (int x = 0; x <= max; x += CellW) GridLayer.Children.Add(new Line { X1 = x, Y1 = 0, X2 = x, Y2 = max, Stroke = ln, StrokeThickness = 1, IsHitTestVisible = false });
        for (int y = 0; y <= max; y += CellH) GridLayer.Children.Add(new Line { X1 = 0, Y1 = y, X2 = max, Y2 = y, Stroke = ln, StrokeThickness = 1, IsHitTestVisible = false });
    }
}
