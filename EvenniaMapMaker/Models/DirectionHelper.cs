using System.Windows.Input;

namespace EvenniaMapMaker.Models;

public static class DirectionHelper
{
    public static Direction GetReverseDirection(Direction dir)
    {
        return dir switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East => Direction.West,
            Direction.West => Direction.East,
            Direction.Northeast => Direction.Southwest,
            Direction.Northwest => Direction.Southeast,
            Direction.Southeast => Direction.Northwest,
            Direction.Southwest => Direction.Northeast,
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            _ => Direction.North
        };
    }

    public static string GetDirectionLabel(Direction dir)
    {
        return dir switch
        {
            Direction.North => "N", Direction.South => "S",
            Direction.East => "E", Direction.West => "W",
            Direction.Northeast => "NE", Direction.Northwest => "NW",
            Direction.Southeast => "SE", Direction.Southwest => "SW",
            Direction.Up => "U", Direction.Down => "D",
            _ => "?"
        };
    }

    /// <summary>
    /// Returns (dX, dY, dZ) coordinate delta for a given direction.
    /// North = Y-1, South = Y+1, East = X+1, West = X-1, etc.
    /// Up = Z+1, Down = Z-1.
    /// </summary>
    public static (int dX, int dY, int dZ) GetCoordinateDelta(Direction dir)
    {
        return dir switch
        {
            Direction.North => (0, -1, 0),
            Direction.South => (0, 1, 0),
            Direction.East => (1, 0, 0),
            Direction.West => (-1, 0, 0),
            Direction.Northeast => (1, -1, 0),
            Direction.Northwest => (-1, -1, 0),
            Direction.Southeast => (1, 1, 0),
            Direction.Southwest => (-1, 1, 0),
            Direction.Up => (0, 0, 1),
            Direction.Down => (0, 0, -1),
            _ => (0, 0, 0)
        };
    }

    /// <summary>
    /// Given a room and a direction, returns the coordinate of the neighboring cell.
    /// </summary>
    public static (int x, int y, int z) GetNeighborCoordinate(RoomModel room, Direction dir)
    {
        var (dx, dy, dz) = GetCoordinateDelta(dir);
        return (room.X + dx, room.Y + dy, room.Z + dz);
    }

    /// <summary>
    /// Maps Numpad keys (1-9) and PageUp/PageDown to directions.
    /// </summary>
    public static Direction? KeyToDirection(Key key) => key switch
    {
        System.Windows.Input.Key.NumPad8 or System.Windows.Input.Key.Up => Direction.North,
        System.Windows.Input.Key.NumPad9 => Direction.Northeast,
        System.Windows.Input.Key.NumPad6 or System.Windows.Input.Key.Right => Direction.East,
        System.Windows.Input.Key.NumPad3 => Direction.Southeast,
        System.Windows.Input.Key.NumPad2 or System.Windows.Input.Key.Down => Direction.South,
        System.Windows.Input.Key.NumPad1 => Direction.Southwest,
        System.Windows.Input.Key.NumPad4 or System.Windows.Input.Key.Left => Direction.West,
        System.Windows.Input.Key.NumPad7 => Direction.Northwest,
        System.Windows.Input.Key.PageUp => Direction.Up,
        System.Windows.Input.Key.PageDown => Direction.Down,
        _ => null
    };
}