
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
    private const double ViewportOverscanPx = 300.0;
    private MainViewModel? _vm;
    private bool _isRefreshing;
    private bool _scrollRefreshPending;

    // Room drag state
    private RoomModel? _draggedRoom;
    private bool _isDragging;
    private Point _dragStartPos;
    private int _dragOriginX, _dragOriginY;
    private bool _dragThresholdMet;
    private const double DragThresholdPx = 5.0;

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

    /// <summary>
    /// Returns the expanded visible viewport rectangle in pre-transform
    /// (logical) canvas coordinates, including the overscan margin.
    /// </summary>
    private (double left, double top, double right, double bottom) GetExpandedVisibleBounds()
    {
        double scale = _vm?.ZoomScale ?? 1.0;
        if (scale <= 0) scale = 1.0;

        double vw = ScrollViewer.ViewportWidth;
        double vh = ScrollViewer.ViewportHeight;
        double sx = ScrollViewer.HorizontalOffset;
        double sy = ScrollViewer.VerticalOffset;

        // Overscan in pre-transform coordinates
        double over = ViewportOverscanPx / scale;

        double left   = sx / scale - over;
        double top    = sy / scale - over;
        double right  = (sx + Math.Max(vw, 1)) / scale + over;
        double bottom = (sy + Math.Max(vh, 1)) / scale + over;

        return (left, top, right, bottom);
    }

    /// <summary>Checks whether a room's visual rectangle intersects the given pre-transform bounds.</summary>
    private static bool IsRoomVisualInBounds(RoomModel room, (double l, double t, double r, double b) bounds)
    {
        double rl = PX(room.X);
        double rt = PY(room.Y);
        double rr = rl + RoomW;
        double rb = rt + RoomH;

        return !(rr <= bounds.l || rl >= bounds.r || rb <= bounds.t || rt >= bounds.b);
    }

    /// <summary>
    /// Simple line-segment vs axis-aligned rectangle intersection test.
    /// Returns true when either endpoint lies inside the rectangle or the
    /// segment crosses one of its edges.
    /// </summary>
    private static bool LineIntersectsRect(
        double x1, double y1, double x2, double y2,
        double l, double t, double r, double b)
    {
        // Quick-reject: bounding box of segment entirely outside
        double sl = Math.Min(x1, x2), sr = Math.Max(x1, x2);
        double st = Math.Min(y1, y2), sb = Math.Max(y1, y2);
        if (sl >= r || sr <= l || st >= b || sb <= t)
            return false;

        // Either endpoint inside
        if ((x1 >= l && x1 <= r && y1 >= t && y1 <= b) ||
            (x2 >= l && x2 <= r && y2 >= t && y2 <= b))
            return true;

        double dx = x2 - x1;
        double dy = y2 - y1;

        // Left edge (x = l)
        if (dx != 0)
        {
            if ((x1 <= l && x2 >= l) || (x2 <= l && x1 >= l))
            {
                double y = y1 + dy * (l - x1) / dx;
                if (y >= t && y <= b) return true;
            }
        }
        // Right edge (x = r)
        if (dx != 0)
        {
            if ((x1 <= r && x2 >= r) || (x2 <= r && x1 >= r))
            {
                double y = y1 + dy * (r - x1) / dx;
                if (y >= t && y <= b) return true;
            }
        }
        // Top edge (y = t)
        if (dy != 0)
        {
            if ((y1 <= t && y2 >= t) || (y2 <= t && y1 >= t))
            {
                double x = x1 + dx * (t - y1) / dy;
                if (x >= l && x <= r) return true;
            }
        }
        // Bottom edge (y = b)
        if (dy != 0)
        {
            if ((y1 <= b && y2 >= b) || (y2 <= b && y1 >= b))
            {
                double x = x1 + dx * (b - y1) / dy;
                if (x >= l && x <= r) return true;
            }
        }

        return false;
    }

    public void RefreshMap()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;
        try
        {
            _scrollRefreshPending = false;
            RoomsLayer.Children.Clear();
            ConnectionsLayer.Children.Clear();
            if (_vm == null) return;
            DrawGrid();
            int cz = _vm.CurrentZ;

            var bounds = GetExpandedVisibleBounds();

            var drawnPairs = new HashSet<string>();

            foreach (var conn in _vm.Connections)
            {
                var src = _vm.Rooms.FirstOrDefault(r => r.Id == conn.SourceRoomId);
                var dst = _vm.Rooms.FirstOrDefault(r => r.Id == conn.DestinationRoomId);
                if (src == null || dst == null) continue;
                if (conn.Direction is Direction.Up or Direction.Down) continue;
                if (src.Z != cz || dst.Z != cz) continue;

                // Viewport culling: skip connection whose line segment does not
                // intersect the expanded visible viewport
                double x1 = CX(src.X), y1 = CY(src.Y), x2 = CX(dst.X), y2 = CY(dst.Y);
                if (!LineIntersectsRect(x1, y1, x2, y2, bounds.left, bounds.top, bounds.right, bounds.bottom))
                    continue;

                string ordered = string.CompareOrdinal(src.Id, dst.Id) < 0 ? src.Id + "|" + dst.Id : dst.Id + "|" + src.Id;
                if (drawnPairs.Contains(ordered)) continue;
                drawnPairs.Add(ordered);

            bool isSel = _vm.SelectedConnection?.Id == conn.Id;
            bool hasReverse = _vm.Connections.Any(c => c.SourceRoomId == dst.Id && c.DestinationRoomId == src.Id && c.Direction is not Direction.Up and not Direction.Down);
            bool isOneWay = conn.IsOneWay || !hasReverse;

            var hitPath = new System.Windows.Shapes.Path
            {
                Data = new LineGeometry(new Point(x1, y1), new Point(x2, y2)),
                Stroke = Brushes.Transparent, StrokeThickness = 10,
                Cursor = Cursors.Hand, Tag = conn
            };
            hitPath.MouseLeftButtonDown += OnConnClick;
            hitPath.MouseRightButtonDown += OnConnRightClick;
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
            lbl.MouseLeftButtonDown+=OnConnClick; lbl.MouseRightButtonDown+=OnConnRightClick; lbl.Cursor=Cursors.Hand;
            ConnectionsLayer.Children.Add(lbl);
        }

        var floorRooms = _vm.Rooms.Where(r => r.Z == cz).ToList();
        foreach (var room in floorRooms)
        {
            // Viewport culling: always render the selected room
            if (!IsRoomVisualInBounds(room, bounds) && _vm.SelectedRoom?.Id != room.Id)
                continue;

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
            {
                var badgeRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 3, 0, 0) };
                if (hu)
                {
                    var ub = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x5A)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x6A, 0x9A)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(5, 1, 5, 1),
                        Cursor = Cursors.Hand,
                        Tag = (room, Direction.Up)
                    };
                    ub.Child = new TextBlock { Text = "U", FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0xBB, 0xFF)) };
                    ub.MouseLeftButtonDown += OnVerticalBadgeClick;
                    ub.MouseEnter += (_, _) => ub.Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x52, 0x7A));
                    ub.MouseLeave += (_, _) => ub.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x5A));
                    badgeRow.Children.Add(ub);
                }
                if (hd)
                {
                    var db = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x5A)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x6A, 0x9A)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(5, 1, 5, 1),
                        Cursor = Cursors.Hand,
                        Tag = (room, Direction.Down),
                        Margin = new Thickness(3, 0, 0, 0)
                    };
                    db.Child = new TextBlock { Text = "D", FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0xBB, 0xFF)) };
                    db.MouseLeftButtonDown += OnVerticalBadgeClick;
                    db.MouseEnter += (_, _) => db.Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x52, 0x7A));
                    db.MouseLeave += (_, _) => db.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x3A, 0x5A));
                    badgeRow.Children.Add(db);
                }
                stack.Children.Add(badgeRow);
            }
            border.Child = stack;
            Canvas.SetLeft(border, PX(room.X)); Canvas.SetTop(border, PY(room.Y));
            border.MouseLeftButtonDown += OnRoomClick;
            border.MouseRightButtonDown += OnRoomRightClick;
            RoomsLayer.Children.Add(border);
        }
        }
        finally { _isRefreshing = false; }
    }

    private void OnConnClick(object s, MouseButtonEventArgs e)
    {
        if (s is FrameworkElement el && el.Tag is ConnectionModel conn && _vm != null)
        { _vm.SelectedConnection = conn; RefreshMap(); }
    }

    private void OnConnRightClick(object s, MouseButtonEventArgs e)
    {
        if (s is not FrameworkElement el || el.Tag is not ConnectionModel conn || _vm == null) return;
        e.Handled = true;
        // Select the connection so the property panel reflects it
        _vm.SelectedConnection = conn;
        RefreshMap();

        var menu = new ContextMenu();
        menu.Items.Add(MakeMenu("Edit Exit", () =>
        {
            // Connection is already selected; ensure property panel displays it
            _vm.SelectedConnection = conn;
        }));
        menu.Items.Add(new Separator());
        menu.Items.Add(MakeMenu("Delete This Exit", () =>
        {
            _vm.DeleteSingleConnection(conn);
            RefreshMap();
        }));

        // Check for structural reverse
        var rev = _vm.FindReverseConnection(conn);
        if (rev != null)
        {
            menu.Items.Add(new Separator());
            menu.Items.Add(MakeMenu("Delete Both Directions", () =>
            {
                _vm.DeleteConnectionPair(conn);
                RefreshMap();
            }));
        }
        menu.IsOpen = true;
    }

    private void OnRoomClick(object s, MouseButtonEventArgs e)
    {
        if (s is not Border b || b.Tag is not RoomModel room || _vm == null) return;

        // If already dragging, ignore (defensive)
        if (_isDragging) { e.Handled = true; return; }

        // Select the room immediately for property panel responsiveness
        _vm.SelectedRoom = room;
        RefreshMap();

        // Begin drag tracking
        _draggedRoom = room;
        _dragOriginX = room.X;
        _dragOriginY = room.Y;
        _dragStartPos = e.GetPosition(MapSurface);
        _isDragging = true;
        _dragThresholdMet = false;
        MapSurface.CaptureMouse();

        e.Handled = true;
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

    // Clickable vertical U/D badge handler — follows the real ConnectionModel
    private void OnVerticalBadgeClick(object s, MouseButtonEventArgs e)
    {
        if (s is not Border badge || badge.Tag is not ValueTuple<RoomModel, Direction> tag || _vm == null) return;
        var (room, dir) = tag;
        var conn = _vm.Connections.FirstOrDefault(c => c.SourceRoomId == room.Id && c.Direction == dir);
        if (conn == null) return;
        var dest = _vm.Rooms.FirstOrDefault(r => r.Id == conn.DestinationRoomId);
        if (dest == null) return;
        _vm.SelectedRoom = dest;
        if (dest.Z != _vm.CurrentZ) _vm.CurrentZ = dest.Z;
        CenterOnRoom(dest);
        RefreshMap();
    }

    /// <summary>Centers the viewport on a room, accounting for current zoom.</summary>
    public void CenterOnRoom(RoomModel room)
    {
        double cx = CX(room.X);
        double cy = CY(room.Y);
        double scale = _vm?.ZoomScale ?? 1.0;
        double vw = ScrollViewer.ViewportWidth;
        double vh = ScrollViewer.ViewportHeight;
        if (vw > 0 && vh > 0)
        {
            ScrollViewer.ScrollToHorizontalOffset(cx * scale - vw / 2);
            ScrollViewer.ScrollToVerticalOffset(cy * scale - vh / 2);
        }
    }

    /// <summary>Centers the viewport on SelectedRoom. If it is on another floor, switches floor first.</summary>
    public void CenterSelectedRoom()
    {
        if (_vm?.SelectedRoom == null) return;
        var room = _vm.SelectedRoom;
        if (room.Z != _vm.CurrentZ)
        {
            _vm.CurrentZ = room.Z;
            // Defer centering until after floor-switch RefreshMap completes layout
            Dispatcher.BeginInvoke(new Action(() => CenterOnRoom(room)),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }
        else
        {
            CenterOnRoom(room);
        }
    }

    private const double FitPaddingViewportPx = 30.0;

    /// <summary>
    /// Fits all rooms on CurrentZ into the viewport.
    /// Computes a zoom scale that makes every room on this floor visible
    /// (with padding), capped at 100%, then centers the bounding box.
    /// Does not modify selection or project state.
    /// </summary>
    public void FitMap()
    {
        if (_vm == null) return;
        var floorRooms = _vm.Rooms.Where(r => r.Z == _vm.CurrentZ).ToList();
        if (floorRooms.Count == 0) return;

        // Bounding box in pre-transform canvas coordinates (including room visual edges)
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;
        foreach (var room in floorRooms)
        {
            double left = PX(room.X);
            double top = PY(room.Y);
            double right = left + RoomW;
            double bottom = top + RoomH;
            if (left < minX) minX = left;
            if (top < minY) minY = top;
            if (right > maxX) maxX = right;
            if (bottom > maxY) maxY = bottom;
        }

        double contentW = maxX - minX;
        double contentH = maxY - minY;
        if (contentW <= 0 || contentH <= 0) return;

        double vw = ScrollViewer.ViewportWidth;
        double vh = ScrollViewer.ViewportHeight;
        if (vw <= 0 || vh <= 0) return;

        double usableW = vw - 2.0 * FitPaddingViewportPx;
        double usableH = vh - 2.0 * FitPaddingViewportPx;
        if (usableW <= 0) usableW = 1.0;
        if (usableH <= 0) usableH = 1.0;

        // Fit scale: min of horizontal and vertical ratios, capped at 1.0 (prefer not zooming above 100%)
        double fitScale = Math.Min(usableW / contentW, usableH / contentH);
        fitScale = Math.Min(fitScale, 1.0);
        fitScale = Math.Clamp(fitScale, MainViewModel.ZoomMin, MainViewModel.ZoomMax);

        _vm.ZoomScale = fitScale;

        // Defer centering until after ScaleTransform layout processes
        double bcx = (minX + maxX) / 2.0;
        double bcy = (minY + maxY) / 2.0;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            double scale = _vm.ZoomScale;
            ScrollViewer.ScrollToHorizontalOffset(bcx * scale - vw / 2.0);
            ScrollViewer.ScrollToVerticalOffset(bcy * scale - vh / 2.0);
        }), System.Windows.Threading.DispatcherPriority.Loaded);
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

    /// <summary>
    /// Creates a room at the given grid coordinates and auto-connects to SelectedRoom
    /// if it is in a neighboring same-floor cell.  Mirrors the exact behaviour of the
    /// existing left-click Build Mode creation path, respecting AutoReverse, Undo/Redo,
    /// and all connection rules.
    /// </summary>
    private void CreateRoomAndConnect(int gx, int gy, int gz)
    {
        if (_vm == null) return;

        var sourceRoom = _vm.SelectedRoom;
        var newRoom = _vm.CreateRoomAt(gx, gy, gz, false);

        if (newRoom == null)
            return;

        if (sourceRoom != null)
        {
            foreach (Direction dir in Enum.GetValues<Direction>())
            {
                if (dir is Direction.Up or Direction.Down)
                    continue;

                var (nx, ny, nz) = DirectionHelper.GetNeighborCoordinate(sourceRoom, dir);

                if (nx == newRoom.X && ny == newRoom.Y && nz == newRoom.Z)
                {
                    _vm.CreateConnection(sourceRoom, newRoom, dir);
                    return;
                }
            }
        }

        RefreshMap();
    }

    private void OnMapMouseDown(object s, MouseButtonEventArgs e)
    {
        if (_vm == null || !_vm.BuildMode)
            return;

        if (e.OriginalSource != MapSurface &&
            e.OriginalSource != GridLayer &&
            e.OriginalSource != ConnectionsLayer)
            return;

        var pos = e.GetPosition(MapSurface);
        int gx = (int)Math.Floor((pos.X - OriginX) / (double)CellW);
        int gy = (int)Math.Floor((pos.Y - OriginY) / (double)CellH);

        CreateRoomAndConnect(gx, gy, _vm.CurrentZ);
    }

    /// <summary>
    /// Opens a compact professional context menu when the user right-clicks
    /// EMPTY map/background space.  Room and connector right-click handlers
    /// continue to set e.Handled, so this handler only fires for empty space.
    /// </summary>
    private void OnMapRightClick(object s, MouseButtonEventArgs e)
    {
        if (_vm == null) return;

        // Only respond to right-click on empty map surface — not on rooms or connectors.
        // Room/connector handlers set e.Handled = true, which we also respect here.
        if (e.OriginalSource != MapSurface &&
            e.OriginalSource != GridLayer &&
            e.OriginalSource != ConnectionsLayer)
            return;

        var pos = e.GetPosition(MapSurface);
        int gx = (int)Math.Floor((pos.X - OriginX) / (double)CellW);
        int gy = (int)Math.Floor((pos.Y - OriginY) / (double)CellH);
        int gz = _vm.CurrentZ;

        // Occupancy check for the clicked cell
        bool occupied = _vm.Rooms.Any(r => r.X == gx && r.Y == gy && r.Z == gz);

        var menu = new ContextMenu();

        // --- Create Room Here ---
        var createItem = MakeMenu("Create Room Here", () =>
        {
            CreateRoomAndConnect(gx, gy, gz);
        });
        createItem.IsEnabled = _vm.BuildMode && !occupied;
        menu.Items.Add(createItem);

        menu.Items.Add(new Separator());

        // --- Fit Map ---
        menu.Items.Add(MakeMenu("Fit Map", () => FitMap()));

        // --- Center Selected ---
        var centerItem = MakeMenu("Center Selected", () => CenterSelectedRoom());
        centerItem.IsEnabled = _vm.SelectedRoom != null;
        menu.Items.Add(centerItem);

        e.Handled = true;
        menu.IsOpen = true;
    }

/// <summary>Mouse-move handler for room dragging.</summary>
    private void OnMapMouseMove(object s, MouseEventArgs e)
    {
        if (_vm == null || !_isDragging || _draggedRoom == null) return;

        var pos = e.GetPosition(MapSurface);
        double dx = pos.X - _dragStartPos.X;
        double dy = pos.Y - _dragStartPos.Y;

        if (!_dragThresholdMet)
        {
            if (Math.Abs(dx) < DragThresholdPx && Math.Abs(dy) < DragThresholdPx)
                return;
            // Threshold exceeded — enter drag mode
            _dragThresholdMet = true;

            // Apply opacity feedback to the dragged room's visual
            foreach (var child in RoomsLayer.Children)
            {
                if (child is Border b && b.Tag == _draggedRoom)
                {
                    b.Opacity = 0.5;
                    b.Cursor = Cursors.SizeAll;
                    break;
                }
            }
        }

        // (No continuous RefreshMap — lightweight V1 approach)
    }

    /// <summary>Mouse-up handler for room drag completion.</summary>
    private void OnMapMouseUp(object s, MouseButtonEventArgs e)
    {
        if (_vm == null || !_isDragging) return;

        MapSurface.ReleaseMouseCapture();

        // Restore opacity on the dragged visual
        if (_dragThresholdMet && _draggedRoom != null)
        {
            foreach (var child in RoomsLayer.Children)
            {
                if (child is Border b && b.Tag == _draggedRoom)
                {
                    b.Opacity = 1.0;
                    b.Cursor = Cursors.Hand;
                    break;
                }
            }

            // Compute target grid coordinate from current mouse position
            var pos = e.GetPosition(MapSurface);
            int targetX = (int)Math.Floor((pos.X - OriginX) / (double)CellW);
            int targetY = (int)Math.Floor((pos.Y - OriginY) / (double)CellH);

            // Only move if target differs from origin
            if (targetX != _dragOriginX || targetY != _dragOriginY)
            {
                _vm.MoveRoom(_draggedRoom, targetX, targetY, _draggedRoom.Z);
            }
        }

        // Reset drag state
        _draggedRoom = null;
        _isDragging = false;
        _dragThresholdMet = false;

        RefreshMap();
    }

    private void OnMapWheel(object s, MouseWheelEventArgs e)
    {
        if (_vm == null) return;

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            // Ctrl+Wheel = zoom around pointer
            double oldScale = _vm.ZoomScale;
            double newScale = oldScale + (e.Delta > 0 ? MainViewModel.ZoomIncrement : -MainViewModel.ZoomIncrement);
            newScale = Math.Clamp(newScale, MainViewModel.ZoomMin, MainViewModel.ZoomMax);
            if (Math.Abs(newScale - oldScale) < 0.001) { e.Handled = true; return; }

            // Capture mouse position relative to ScrollViewer viewport
            var vpPos = e.GetPosition(ScrollViewer);
            double sx = ScrollViewer.HorizontalOffset;
            double sy = ScrollViewer.VerticalOffset;

            // Map surface coordinate under the pointer (pre-transform)
            double mapX = (sx + vpPos.X) / oldScale;
            double mapY = (sy + vpPos.Y) / oldScale;

            // Apply new scale (triggers RefreshMap via property setter)
            _vm.ZoomScale = newScale;

            // Defer scroll adjustment until after layout processes the new transform
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ScrollViewer.ScrollToHorizontalOffset(mapX * newScale - vpPos.X);
                ScrollViewer.ScrollToVerticalOffset(mapY * newScale - vpPos.Y);
            }), System.Windows.Threading.DispatcherPriority.Loaded);

            e.Handled = true;
        }
        else
        {
            // Plain wheel = change floor
            if (e.Delta > 0) _vm.CurrentZ++; else _vm.CurrentZ--;
            e.Handled = true;
        }
    }

    private void OnKeyDown(object s, KeyEventArgs e)
    {
        if (_vm == null) return;
        if (e.Key == Key.Delete) { _vm.DeleteSelected(); RefreshMap(); e.Handled = true; }
        else if (e.Key == Key.Escape) { _vm.SelectedRoom = null; _vm.SelectedConnection = null; RefreshMap(); e.Handled = true; }
        else { var d = DirectionHelper.KeyToDirection(e.Key); if (d.HasValue) { _vm.NavigateOrBuild(d.Value); e.Handled = true; } }
    }

    private void OnScrollChanged(object s, ScrollChangedEventArgs e)
    {
        if (_isRefreshing || _scrollRefreshPending) return;
        _scrollRefreshPending = true;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _scrollRefreshPending = false;
            RefreshMap();
        }), System.Windows.Threading.DispatcherPriority.Background);
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




