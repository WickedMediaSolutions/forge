# Master Task List - Evennia Atlas

## Phase 1: Models & Coordinate System
- [x] Direction enum (10 directions)
- [x] ExitType enum (Normal, Door)
- [x] DirectionHelper: GetCoordinateDelta, GetNeighborCoordinate, KeyToDirection
- [x] RoomModel: int X/Y/Z grid coordinates (NOT pixel positions)
- [x] ConnectionModel with shared door IDs
- [x] DoorModel
- [x] MapProject

## Phase 2: ViewModel
- [x] BuildMode toggle (ON/OFF)
- [x] AutoReverse toggle (ON/OFF)
- [x] CurrentZ floor selection
- [x] CreateRoomAt(x,y,z) — grid-coordinate based, rejects occupied cells
- [x] NavigateOrBuild(direction) — navigates to existing room or creates+connects
- [x] CreateConnection with auto-reverse support
- [x] Delete, Undo/Redo
- [x] Project save/load/export

## Phase 3: Map Canvas
- [x] Visual position = coordinate × cell size (150×110)
- [x] Z-level filtering (show only current floor)
- [x] Click grid cell to create room
- [x] Numpad/arrow key navigation (1-9 = directions, PgUp/PgDn = up/down)
- [x] Right-click context menu on rooms
- [x] Up/Down (U/D) indicators on rooms
- [x] Connection lines for same-Z exits only
- [x] Checkerboard grid background

## Phase 4: MainWindow UI
- [x] Toolbar: New, Open, Save, Build Mode, Auto Reverse, Floor Up/Down, Delete, Export, Undo, Redo
- [x] Room properties panel
- [x] Connection/exit properties panel
- [x] Status bar with room count, Build Mode state, Auto Reverse state, current floor

## Phase 5: Save/Load/Export
- [x] JSON save (.evenniamap) with int coordinates
- [x] JSON load preserving all data
- [x] Evennia export (.evennia.json) with int X/Y/Z

## Pending
- [ ] Manual connection mode between non-adjacent rooms (via dialog)
- [ ] Fit Map button
- [ ] Validation before export
- [ ] Test map workflow verification
- [ ] Multi-floor visual stacking (Z layers shown side-by-side or as tabs)