
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using EvenniaAtlas.Models;
using EvenniaAtlas.ViewModels;

namespace EvenniaAtlas.Controls;

public partial class MapCanvas : UserControl
{
    public const int CellW = 140;
    public const int CellH = 100;
    public const int RoomW = 126;
    public const int RoomH = 76;
    private const int OriginX = 7000;
    private const int OriginY = 7000;
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
        RefreshMap();
    }

    private static double PX(int gx) => OriginX + gx * CellW + (CellW - RoomW) / 2;
    private static double PY(int gy) => OriginY + gy * CellH + (CellH - RoomH) / 2;
    private static double CX(int gx) => OriginX + gx * CellW + CellW / 2;
    private static double CY(int gy) => OriginY + gy * CellH + CellH / 2;

    public void RefreshMap()
    {
        RoomsLayer.Children.Clear();
        ConnectionsLayer.Children.Clear();
        if (_vm == null) return;
        DrawGrid();
        int cz = _vm.CurrentZ;

        var drawnPairs = new HashSet<string>();

        foreach (var conn in _vm.Connections)
        {
            var src = _vm.Rooms.FirstOrDefault(r => r.Id == conn.SourceRoomId);
            var dst = _vm.Rooms.FirstOrDefault(r => r.Id == conn.DestinationRoomId);
            if (src == null || dst == null) continue;
            if (conn.Direction is Direction.Up or Direction.Down) continue;
            if (src.Z != cz || dst.Z != cz) continue;

            string ordered = string.CompareOrdinal(src.Id, dst.Id) < 0 ? src.Id + "|" + dst.Id : dst.Id + "|" + src.Id;
            if (drawnPairs.Contains(ordered)) continue;
            drawnPairs.Add(ordered);

            bool isSel = _vm.SelectedConnection?.Id == conn.Id;
            bool hasReverse = _vm.Connections.Any(c => c.SourceRoomId == dst.Id && c.DestinationRoomId == src.Id && c.Direction is not Direction.Up and not Direction.Down);
            bool isOneWay = conn.IsOneWay || !hasReverse;

            double x1 = CX(src.X), y1 = CY(src.Y), x2 = CX(dst.X), y2 = CY(dst.Y);

            var hitPath = new System.Windows.Shapes.Path
            {
                Data = new LineGeometry(new Point(x1, y1), new Point(x2, y2)),
                Stroke = Brushes.Transparent, StrokeThickness = 10,
                Cursor = Cursors.Hand, Tag = conn
            };
            hitPath.MouseLeftButtonDown += OnConnClick;
            ConnectionsLayer.Children.Add(hitPath);

            Color lc = isSel ? Color.FromRgb(0xFF,0xD7,0x00) : conn.ExitType == ExitType.Door ? Color.FromRgb(0x8B,0x45,0x13) : isOneWay ? Color.FromRgb(0x77,0x77,0x99) : Color.FromRgb(0x50,0x50,0x50);
            ConnectionsLayer.Children.Add(new Line { X1=x1,Y1=y1,X2=x2,Y2=y2, Stroke=new SolidColorBrush(lc), StrokeThickness=isSel?3:1.8, StrokeDashArray=conn.ExitType==ExitType.Door?new DoubleCollection(new[]{4.0,2.0}):null, IsHitTestVisible=false });

            if (isOneWay)
            {
                double mx=(x1+x2)/2, my=(y1+y2)/2, a=Math.Atan2(y2-y1,x2-x1), s=6;
                var arrow=new Polygon
                {
                    Points=new PointCollection{new(mx+s*Math.Cos(a),my+s*Math.Sin(a)),new(mx-s*Math.Cos(a-1.2),my-s*Math.Sin(a-1.2)),new(mx-s*Math.Cos(a+1.2),my-s*Math.Sin(a+1.2))},
                    Fill=new SolidColorBrush(lc), IsHitTestVisible=false
                };
                ConnectionsLayer.Children.Add(arrow);
            }

            string label;
            if (!isOneWay && hasReverse)
            {
                var ra=DirectionHelper.GetDirectionLabel(conn.Direction);
                var rb=DirectionHelper.GetDirectionLabel(DirectionHelper.GetReverseDirection(conn.Direction));
                label=ra+"/"+rb;
            }
            else { label=DirectionHelper.GetDirectionLabel(conn.Direction); if(isOneWay) label=">"+label; }

            var lbl=new TextBlock{Text=label,Foreground=isSel?Brushes.Gold:new SolidColorBrush(Color.FromRgb(0x99,0x99,0x99)),FontSize=9,FontWeight=FontWeights.Bold,Background=new SolidColorBrush(Color.FromRgb(0x2D,0x2D,0x2D)),Padding=new Thickness(3,1,3,1),Tag=conn};
            Canvas.SetLeft(lbl,(x1+x2)/2-label.Length*3.5);
            Canvas.SetTop(lbl,(y1+y2)/2-8);
            lbl.MouseLeftButtonDown+=OnConnClick; lbl.Cursor=Cursors.Hand;
            ConnectionsLayer.Children.Add(lbl);
        }

        var floorRooms = _vm.Rooms.Where(r => r.Z == cz).ToList();
        foreach (var room in floorRooms)
        {
            bool isSel = _vm.SelectedRoom?.Id == room.Id;
            var border = new Border
            {
                Width = RoomW, Height = RoomH,
                Background = isSel ? new SolidColorBrush(Color.FromRgb(0x30,0x50,0x80)) : _vm.BuildMode ? new SolidColorBrush(Color.FromRgb(0x2D,0x3D,0x2D)) : new SolidColorBrush(Color.FromRgb(0x3C,0x3C,0x3C)),
                BorderBrush = isSel ? new SolidColorBrush(Color.FromRgb(0xFF,0xD7,0x00)) : new SolidColorBrush(Color.FromRgb(0x55,0x55,0x55)),
                BorderThickness = new Thickness(isSel ? 2 : 1),
                CornerRadius = new CornerRadius(4), Tag = room, Cursor = Cursors.Hand
            };
            var stack = new StackPanel { Margin = new Thickness(6,4,6,4) };
            stack.Children.Add(new TextBlock { Text = room.DisplayTitle, Foreground = isSel ? Brushes.White : new SolidColorBrush(Color.FromRgb(0xDD,0xDD,0xDD)), FontSize = 11, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = RoomW - 16 });
            if (!string.IsNullOrWhiteSpace(room.RoomType))
                stack.Children.Add(new Border { Background = new SolidColorBrush(Color.FromRgb(0x55,0x3C,0x00)), CornerRadius = new CornerRadius(2), Padding = new Thickness(4,1,4,1), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0,2,0,0), Child = new TextBlock { Text = room.RoomType, FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0xFF,0xCC,0x44)) } });
            bool hu = _vm.Connections.Any(c => c.SourceRoomId == room.Id && c.Direction == Direction.Up);
            bool hd = _vm.Connections.Any(c => c.SourceRoomId == room.Id && c.Direction == Direction.Down);
            if (hu || hd)
                stack.Children.Add(new TextBlock { Text = (hu ? "U " : "") + (hd ? "D" : ""), FontSize = 8, Foreground = new SolidColorBrush(Color.FromRgb(0x88,0xBB,0xFF)), HorizontalAlignment = HorizontalAlignment.Right });
            border.Child = stack;
            Canvas.SetLeft(border, PX(room.X)); Canvas.SetTop(border, PY(room.Y));
            border.MouseLeftButtonDown += OnRoomClick;
            border.MouseRightButtonDown += OnRoomRightClick;
            RoomsLayer.Children.Add(border);
        }
    }

    private void OnConnClick(object s, MouseButtonEventArgs e)
    {
        if (s is FrameworkElement el && el.Tag is ConnectionModel conn && _vm != null)
        { _vm.SelectedConnection = conn; RefreshMap(); }
    }

    private void OnRoomClick(object s, MouseButtonEventArgs e)
    {
        if (s is Border b && b.Tag is RoomModel room && _vm != null)
        { _vm.SelectedRoom = room; e.Handled = true; }
    }

    private void OnRoomRightClick(object s, MouseButtonEventArgs e)
    {
        if (s is not Border b || b.Tag is not RoomModel room || _vm == null) return;
        _vm.SelectedRoom = room; e.Handled = true;
        var menu = new ContextMenu();
        menu.Items.Add(MakeMenu("Edit Title...", () => {
            var dlg = new EvenniaAtlas.Dialogs.QuickEditDialog("Title", room.Title, false);
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true) { room.Title = dlg.ResultText; _vm.MarkDirtyPublic(); RefreshMap(); }
        }));
        menu.Items.Add(MakeMenu("Edit Description...", () => {
            var dlg = new EvenniaAtlas.Dialogs.QuickEditDialog("Description", room.Description, true);
            dlg.Owner = Window.GetWindow(this);
            if (dlg.ShowDialog() == true) { room.Description = dlg.ResultText; _vm.MarkDirtyPublic(); RefreshMap(); }
        }));
        menu.Items.Add(new Separator());
        var exitMenu = new MenuItem { Header = "Add Exit" };
        foreach (Direction dir in Enum.GetValues<Direction>())
        { var d = dir; exitMenu.Items.Add(MakeMenu(d.ToString(), () => ShowExitTargetPicker(room, d))); }
        menu.Items.Add(exitMenu);
        menu.Items.Add(new Separator());
        menu.Items.Add(MakeMenu("Delete Room", () => { _vm.DeleteRoom(room); RefreshMap(); }));
        menu.IsOpen = true;
    }

    private void ShowExitTargetPicker(RoomModel source, Direction dir)
    {
        if (_vm == null) return;
        var others = _vm.Rooms.Where(r => r.Id != source.Id).ToList();
        if (others.Count == 0)
        { MessageBox.Show("No other rooms exist to connect to.", "No Target", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var picker = new EvenniaAtlas.Dialogs.RoomPickerDialog(others);
        picker.Owner = Window.GetWindow(this);
        if (picker.ShowDialog() == true && picker.SelectedRoom != null)
        { _vm.CreateConnection(source, picker.SelectedRoom, dir); _vm.MarkDirtyPublic(); RefreshMap(); }
    }

    private static MenuItem MakeMenu(string h, Action a)
    { var mi = new MenuItem { Header = h }; mi.Click += (_, _) => a(); return mi; }

    private void OnMapMouseDown(object s, MouseButtonEventArgs e)
    {
        if (_vm == null) return;
        var pos = e.GetPosition(MapSurface);
        int gx = (int)Math.Floor((pos.X - OriginX) / (double)CellW);
        int gy = (int)Math.Floor((pos.Y - OriginY) / (double)CellH);
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
        else { var d = DirectionHelper.KeyToDirection(e.Key); if (d.HasValue) { _vm.NavigateOrBuild(d.Value); e.Handled = true; } }
    }

    private void DrawGrid()
    {
        GridLayer.Children.Clear();
        var alt = new SolidColorBrush(Color.FromRgb(0x2A,0x2A,0x2E));
        var ln = new SolidColorBrush(Color.FromRgb(0x3E,0x3E,0x3E));
        int minGX = -50, maxGX = 50, minGY = -30, maxGY = 30;
        for (int gx = minGX; gx <= maxGX; gx++)
            for (int gy = minGY; gy <= maxGY; gy++)
                if ((gx + gy) % 2 == 0)
                {
                    var r = new Rectangle { Width = CellW, Height = CellH, Fill = alt, IsHitTestVisible = false };
                    Canvas.SetLeft(r, OriginX + gx * CellW); Canvas.SetTop(r, OriginY + gy * CellH); GridLayer.Children.Add(r);
                }
        for (int gx = minGX; gx <= maxGX + 1; gx++) GridLayer.Children.Add(new Line { X1 = OriginX + gx * CellW, Y1 = OriginY + minGY * CellH, X2 = OriginX + gx * CellW, Y2 = OriginY + (maxGY + 1) * CellH, Stroke = ln, StrokeThickness = 1, IsHitTestVisible = false });
        for (int gy = minGY; gy <= maxGY + 1; gy++) GridLayer.Children.Add(new Line { X1 = OriginX + minGX * CellW, Y1 = OriginY + gy * CellH, X2 = OriginX + (maxGX + 1) * CellW, Y2 = OriginY + gy * CellH, Stroke = ln, StrokeThickness = 1, IsHitTestVisible = false });
    }
}
