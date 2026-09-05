# Evennia Atlas — Implementation Checklist

> Generated: 2026-09-05
> Based on inspection of actual codebase state.
> Legend: [WORKING] = implemented and verified, [PARTIAL] = exists but incomplete, [MISSING] = not implemented, [NEEDS TESTING] = implemented but not systematically verified

---

## 1. Application Shell / Professional UI

[PARTIAL] DarkTheme.xaml provides basic dark styling (buttons, textboxes, toolbars).
[PARTIAL] Toolbar existed with New/Open/Save/Undo/Redo/Build/AutoReverse/Floor/Delete/Export.

[WORKING] UI Polish Pass 1 — application shell and primary toolbar (2026-09-05).
  - Compact branding strip with Evennia green accent and professional subtitle.
  - Custom toolbar replacing ToolBarTray/ToolBar with polished button styles.
  - ToolbarButtonStyle with hover/pressed/disabled/focus states.
  - Subtle vertical separators between logical command groups.
  - BUILD / AUTO REVERSE LED indicators (DataTrigger bindings).
  - Floor controls [▼] [Z:N] [▲] in grouped border.
  - Zoom controls [−] [N%] [+] in grouped border.
  - Menu button with basic file operations dropdown.
  - 0 errors / 0 warnings on build.
  - All 16 click handlers preserved.
  - Room/map/properties/dialogs not yet polished (separate passes).

[WORKING] UI Polish Pass 1B — professional shell command hierarchy and color-system redesign (2026-09-05).
  - Proper Menu bar (File, Edit, View, Map, Tools, Help) replacing generic Menu button.
  - Slim permanent toolbar: Build LED, Auto Reverse LED, Floor, Fit, Center, Zoom only.
  - All commands accessible through organized menus (New/Open/Save/SaveAs/Undo/Redo/ Delete/Validate/Export moved off toolbar).
  - Restrained Evennia green accent used only for LEDs and branding — no random accent colors.
  - Floor label (Z: 0) uses neutral text, not special green.
  - Zoom percentage uses neutral text, not special blue.
  - No red Delete button treatment on toolbar.
  - LED indicators: solid green ON (#4CAF50) / dark gray OFF — no glow/neon.
  - Professional dark palette consistent across shell, menu, and toolbar.
  - Toolbar buttons: low-profile, transparent background, subtle hover/pressed states.
  - Menu: dark charcoal dropdowns, restrained hover highlighting, no rounded pills.
  - Compact vertical density — brand + menu + toolbar fit in minimal space.
  - All existing command handlers reused (no duplicate implementations).
  - All keyboard shortcuts preserved (Ctrl+S/Z/Y/Delete/direction/numpad/PageUp/Down).
  - Menu_Click popup code removed; Exit_Click added for File > Exit.
  - Menu Separator style defined in DarkTheme.xaml.
  - Build: 0 errors / 0 warnings.
  [NEEDS VISUAL APPROVAL] Shell appearance must be reviewed by developer.


## 2. Map Canvas

[WORKING] Checkerboard grid background at 140x100 cell size.
[WORKING] Room rendering as styled Border elements with Title + ID display.
[WORKING] Connection lines drawn for same-Z exits only.
[WORKING] Selected-room gold highlight (#FFD700 border).
[WORKING] One-way directional arrow indicators on connection lines.
[WORKING] Door connections shown with dashed brown lines.
[WORKING] Hit-test transparent path for connector click selection.
[WORKING] Z-level filtering (only CurrentZ rooms/connections displayed).
[WORKING] Bidirectional pairs deduplicated to single visual line.
[WORKING] UP/DOWN directions excluded from same-floor connector lines.
[WORKING] U/D indicators on rooms with vertical exits.
[WORKING] Zoom: 25%–300% via Ctrl+MouseWheel and toolbar controls, visual-only.
[WORKING] Viewport-aware culling for large maps (room visuals, connection visuals, 300px overscan, deferred scroll refresh).

## 3. Room Creation / Editing

[WORKING] CreateRoomAt(x,y,z) with duplicate-coordinate rejection.
[WORKING] First-room default-title prompt dialog.
[WORKING] Subsequent rooms inherit DefaultRoomTitle.
[WORKING] Stable room IDs (projectId_room_NNNN).
[WORKING] Room property editing panel: Title, RoomType, Description, Tags, Notes.
[WORKING] XYZ displayed read-only in properties panel.
[WORKING] Room deletion with cascading connection cleanup.
[WORKING] Right-click context menu with Edit/Delete/Connect options.
[WORKING] Room grid-to-grid drag movement (grid-snapped, same-Z, occupancy-rejected, undo/redo).
[WORKING] Room coordinate editing in panel.

## 4. Exit Creation / Editing

[WORKING] CreateConnection() as single centralized exit creation point.
[WORKING] Direction enum with all 10 directions.
[WORKING] ReverseDirection auto-calculated via DirectionHelper.
[WORKING] Exit properties panel: EXIT section (Id, Source, Destination, Direction, KeyName, Description, Aliases, Exit Type, One-Way).
[WORKING] Door sub-panel: Name, Starts Open/Closed/Locked, Key ID.
[WORKING] EVENNIA section: TypeclassPath.
[WORKING] ALIASES section: structured Key/Category with add/remove.
[WORKING] TAGS section: structured Key/Category/Data with add/remove.
[WORKING] ATTRIBUTES section: card-style Key/Value/Category/LockString with add/remove.
[WORKING] PERMISSIONS section: per-entry textboxes with add/remove.
[WORKING] LOCKS section: multi-line Consolas-font lockstring editor.
[WORKING] Exit deletion with paired reverse cleanup.
[WORKING] Exit selection via connector line click.
[WORKING] Room EXITS panel shows outgoing exits.
[WORKING] Manual connection via context menu + direction chooser.

## 5. Door Support

[WORKING] ExitType.Door enum + DoorModel (Name, Typeclass, StartsOpen/Closed/Locked, Lockable, KeyId, SynchronizeOpposite).
[WORKING] Door access fields: TraverseLockString, LockedFailureMessage, ClosedFailureMessage.
[WORKING] Door-specific metadata: Description, DoorAliases, DoorTags, DoorAttributes, DoorPermissions.
[WORKING] SharedDoorId for pairing door halves + SyncDoorWithReverse() with structural reverse matching.
[WORKING] Expanded door properties UI: DOOR, ACCESS, DOOR ALIASES, DOOR TAGS, DOOR ATTRIBUTES, DOOR PERMISSIONS, DESCRIPTION.
[WORKING] Door sub-panel with dashed brown connector lines preserved.
[WORKING] State consistency: StartsOpen/Closed mutual exclusion, StartsLocked enforces closed.
[WORKING] SyncDoorWithReverse respects SynchronizeOpposite flag.
[WORKING] Reverse connection identified by structural match (source/dest reversal + direction) + SharedDoorId.
[WORKING] Empty SharedDoorId cannot match unrelated normal connections.
[WORKING] One-way door works without reverse — no crash, no phantom creation.

## 6. Build Mode

[WORKING] BuildMode bool with ON/OFF text, toggle via toolbar button.
[WORKING] Respected by OnMapMouseDown and NavigateOrBuild.
[WORKING] Status bar displays BuildMode state.
[MISSING] LED status indicator on toolbar button (target of this pass).

## 7. Auto Reverse

[WORKING] AutoReverse bool (default ON), toggle via toolbar button.
[WORKING] Respected by CreateConnection — reverse exit only when ON.
[WORKING] OFF = requested direction only; does not repair imported.
[WORKING] Status bar displays AutoReverse state.
[MISSING] LED status indicator on toolbar button (target of this pass).

## 8. Direction Compass

[WORKING] On-map compass control (NW/N/NE/W/•/E/SW/S/SE + UP/DOWN) via unified handler.
[WORKING] Compass calls NavigateOrBuild shared logic; no duplicated navigation rules.
[WORKING] Tooltips on all 10 directional buttons.
[WORKING] Professional dark-editor appearance; compact layout, subtle hover/pressed states.
[WORKING] DirectionChooserDialog popup for manual connections.
[WORKING] Keyboard numpad directions + NavigateOrBuild shared logic.
## 9. Floors / Z Levels / Up / Down

[WORKING] CurrentZ with FloorLabel "Floor N", toolbar up/down buttons.
[WORKING] Mouse wheel changes Z level, Z-filtering on map.
[WORKING] Up/Down in NavigateOrBuild creates rooms on different Z.
[WORKING] CurrentZ auto-changes when navigating to different-Z room.
[WORKING] Clickable U/D badges on room nodes — follows actual ConnectionModel destination.
[WORKING] Clicking U/D badge selects destination, switches floor, centers map.
[PARTIAL] Centering via ScrollViewer offsets on U/D badge click; compass/keyboard vertical nav does not auto-center.
[OUT OF SCOPE V1] Multi-floor side-by-side/tabbed view — v1 intentionally displays one Z level at a time.

## 10. Keyboard Controls

[WORKING] Numpad 1-9 maps to 8 cardinal directions + arrows for N/S/E/W.
[WORKING] PageUp/PageDown for Up/Down, Ctrl+Z/Y/S shortcuts.
[WORKING] Delete/Escape keys, all via NavigateOrBuild shared logic.

## 11. Room Properties

[WORKING] RoomModel: Id, Title, RoomType, Description, X, Y, Z, Tags, Notes.
[WORKING] Properties panel binds via MainViewModel wrapper properties.
[WORKING] RoomModel stores: TypeclassPath, structured Aliases, EvenniaTags (TagModel), Attributes, LockString, Permissions.
[WORKING] Room UI editor for TypeclassPath, structured Aliases (Key/Category), EvenniaTags (Key/Category/Data), Attributes (Key/Value/Category/LockString), Permissions, LockString.
[WORKING] EXIT UI editor for Evennia-native fields: TypeclassPath, structured Aliases, EvenniaTags, Attributes, Permissions, LockString.
[MISSING] Atlas-only metadata (area/zone, mapper notes separate from Evennia notes).

## 12. Native Evennia Object Properties (Room)

[WORKING] RoomModel now stores TypeclassPath, Aliases (List<AliasModel>), EvenniaTags (List<TagModel>), Attributes (List<AttributeModel>), LockString, Permissions.
[WORKING] Room UI editor implemented: TypeclassPath textbox, Aliases add/remove grid, EvenniaTags add/remove grid, Attributes add/remove card, Permissions add/remove, LockString multi-line editor.
[PARTIAL] RoomType string remains informal typeclass — no structured typeclass_path validation.
[WORKING] EXIT UI editor for Evennia-native fields implemented.

## 13. Native Evennia Exit Properties

[WORKING] ConnectionModel now stores KeyName, Description, TypeclassPath, AliasesList, EvenniaTags, Attributes, LockString, Permissions.
[WORKING] EXIT UI editor implemented: KeyName textbox, Description multi-line editor.
[WORKING] Collections upgraded from List&lt;T&gt; to ObservableCollection&lt;T&gt; for live WPF binding.
[PARTIAL] Aliases (comma-separated string for Atlas UI) remains alongside structured AliasesList for Evennia.
[PARTIAL] No traverse lock, other lockstrings, permissions, tags, attributes UI editor.

ROOM UI: WORKING
EXIT UI: WORKING

## 14. Tags

[PARTIAL] RoomModel has Tags as List<string>, editable as comma-separated string.
[WORKING] TagModel (Key/Category/Data) created. Both RoomModel and ConnectionModel store EvenniaTags as ObservableCollection<TagModel>.
[WORKING] Room UI editor for EvenniaTags with Key/Category/Data textboxes per row, [+ Add Tag] and [X Remove].
[WORKING] EXIT UI editor for EvenniaTags with Key/Category per row plus Data field, [+ Add Tag] and [X Remove].
[MISSING] No tags on DoorModel.

ROOM UI: WORKING
EXIT UI: WORKING

## 15. Attributes

[WORKING] AttributeModel (Key/Value/Category/LockString) created. Both RoomModel and ConnectionModel store Attributes as ObservableCollection<AttributeModel>.
[WORKING] Room UI editor for Attributes with card-style layout: Key/Value row + Category/LockString row per entry, [+ Add Attribute] and [X Remove].
[WORKING] EXIT UI editor for Attributes with card-style layout matching Room, [+ Add Attribute] and [X Remove].

ROOM UI: WORKING
EXIT UI: WORKING

## 16. Aliases

[PARTIAL] ConnectionModel has Aliases as comma-separated string for Atlas UI convenience.
[WORKING] AliasModel (Key/Category) created. RoomModel stores Aliases as ObservableCollection<AliasModel>. ConnectionModel stores AliasesList as ObservableCollection<AliasModel>.
[WORKING] Room UI editor for structured Aliases with Key/Category textboxes per row, [+ Add Alias] and [X Remove].
[WORKING] EXIT UI editor for structured Aliases with Key/Category textboxes per row, [+ Add Alias] and [X Remove].

ROOM UI: WORKING
EXIT UI: WORKING

## 17. Permissions

[WORKING] Both RoomModel and ConnectionModel store Permissions as ObservableCollection<string>.
[WORKING] Room UI editor for Permissions with textboxes per entry, [+ Add Permission] and [X Remove].
[WORKING] EXIT UI editor for Permissions with textboxes per entry, [+ Add Permission] and [X Remove].

ROOM UI: WORKING
EXIT UI: WORKING

## 18. Lockstrings

[WORKING] Both RoomModel and ConnectionModel store LockString as raw Evennia lockstring.
[WORKING] Room UI editor: multi-line Consolas-font textbox for lockstring editing.
[WORKING] EXIT UI editor: multi-line Consolas-font textbox for lockstring editing.

ROOM UI: WORKING
EXIT UI: WORKING

## 19. Typeclasses

[PARTIAL] RoomModel.RoomType is free-text; ConnectionModel.ExitType is Normal/Door enum.
[WORKING] Both RoomModel and ConnectionModel now store TypeclassPath (string) for Evennia-native typeclass designation.
[WORKING] Room UI editor: TypeclassPath textbox with tooltip placeholder.
[WORKING] EXIT UI editor: TypeclassPath textbox with tooltip placeholder.
[MISSING] No Python import-path validation in this pass.

ROOM UI: WORKING
EXIT UI: WORKING

## 20. Save / Load

[WORKING] ProjectFileService with JSON .evenniamap format.
[WORKING] Dirty tracking, ConfirmClose, Save/SaveAs/Open workflows.
[WORKING] Counter reconstruction on load.

## 21. .evenniamap Project Format

[WORKING] JSON: version, id, name, defaultRoomTitle, rooms[], connections[].
[WORKING] Rooms: id, title, roomType, description, x, y, z, tags, notes.
[WORKING] Connections: full model serialization with camelCase naming.
[MISSING] Format version migration.

## 22. Evennia JSON Export

[WORKING] EvenniaExportService with .evennia.json output (areaId, areaName, version, rooms, exits).
[WORKING] Room export: Id, Title, RoomType, Description, X, Y, Z, Tags (Atlas), Notes.
[WORKING] Room export (Evennia): TypeclassPath, structured Aliases (Key/Category), EvenniaTags (Key/Category/Data), Attributes (Key/Value/Category/LockString), Permissions, LockString.
[WORKING] Exit export: SourceRoomId, DestinationRoomId, Direction, ReverseDirection, IsOneWay, ExitType, Aliases (legacy Atlas comma-separated).
[WORKING] Exit export (Evennia): Key (KeyName fallback to direction), Description, TypeclassPath, structured AliasesList (Key/Category), EvenniaTags (Key/Category/Data), Attributes (Key/Value/Category/LockString), Permissions, LockString.
[WORKING] Door sub-object export: Name, StartsOpen, StartsClosed, StartsLocked, KeyId, SharedDoorId (Atlas convenience).
[WORKING] Door export (Evennia): Typeclass, Lockable, SynchronizeOpposite, TraverseLockString, LockedFailureMessage, ClosedFailureMessage.
[WORKING] Door metadata export: Description, DoorAliases (Key/Category), DoorTags (Key/Category/Data), DoorAttributes (Key/Value/Category/LockString), DoorPermissions.
[WORKING] Atlas vs Evennia metadata clearly separated (Atlas Tags vs EvenniaTags, Atlas Aliases vs AliasesList).
[WORKING] KeyName fallback: non-empty KeyName exported as exit key; empty KeyName falls back to direction string.
[WORKING] Two-sided doors: each ConnectionModel exported independently; SharedDoorId describes relationship.
[WORKING] One-way exits exported exactly as modeled; export never mutates map.
[WORKING] Empty collections emitted as []; no invented placeholder data.
[WORKING] Backward compatible: missing/empty new fields do not cause exceptions.
[WORKING] Deterministic export (no timestamps, random IDs, or changing values).
[WORKING] Export is READ-ONLY: never creates/deletes rooms/exits, never modifies coordinates/metadata, never marks project dirty.

## 23. Undo / Redo

[WORKING] TRUE REDO architecture: UndoAction contains TWO delegates (undo + redo); Redo() calls redo delegate, not undo again.
[WORKING] Undo/Redo stacks with UndoAction delegate pattern (now with dual Action delegates).
[WORKING] Room/connection creation/deletion undo/redo with paired reverse handling and captured reverse model for connection create redo.
[WORKING] Room scalar property undo/redo (Title, RoomType, Description, Tags, Notes, TypeclassPath, LockString via GotFocus/LostFocus coalescing).
[WORKING] Room collection undo/redo (Aliases, EvenniaTags, Attributes, Permissions — add/remove with deep-copy snapshots, original index restored).
[WORKING] Exit scalar property undo/redo (Direction, ExitType, IsOneWay, KeyName, Description, Aliases, TypeclassPath, LockString — TextChanged coalesced via GotFocus/LostFocus; Direction/ExitType/IsOneWay as atomic push).
[WORKING] Exit collection undo/redo (AliasesList, EvenniaTags, Attributes, Permissions — add/remove with deep-copy).
[WORKING] Door scalar property undo/redo (Name, Typeclass, StartsOpen, StartsClosed, StartsLocked, Lockable, KeyId, SynchronizeOpposite, TraverseLockString, LockedFailureMessage, ClosedFailureMessage, Description — bools/combo as atomic push, text via coalescing; redo re-applies forward logic with SyncDoorWithReverse).
[WORKING] Door collection undo/redo (DoorAliases, DoorTags, DoorAttributes, DoorPermissions — add/remove with paired atomicity when SynchronizeOpposite is on; after-state snapshots captured for redo).
[WORKING] Paired door atomicity: one logical undo restores BOTH synchronized doors when SynchronizeOpposite is enabled; one logical redo restores BOTH to after-state.
[WORKING] Deep-copy for collection snapshots (CloneAliases, CloneTags, CloneAttrs, ClonePerms, CloneDoor, SnapshotPairedDoor helpers).
[WORKING] Collection item field edit undo/redo (Alias Key/Category, Tag Key/Category/Data, Attribute Key/Value/Category/LockString, Permission strings): Window-level PreviewGotKeyboardFocus/PreviewLostKeyboardFocus tunneling events with both restore and redo lambdas; door items capture both before and after snapshots for correct paired redo.
[WORKING] Per-keystroke history prevention: BeginPropertyEdit/EndPropertyEdit coalescing now passes both restore and redo actions.
[WORKING] Ctrl+Z / Ctrl+Y keyboard shortcuts wired to Undo()/Redo().
[WORKING] Toolbar Undo/Redo buttons wired to Undo()/Redo().
[WORKING] History cleared on New Project / Open Project (existing behavior preserved).
[WORKING] Dirty-state: MarkDirty() called in all undo/redo actions; undo/redo does not accidentally clear dirty flag.
[WORKING] Selection cleared on undo/redo (preserves existing behavior); property bindings refresh via OnPropertyChanged calls.
[WORKING] Map refreshed via RefreshMap() on undo/redo of structural operations; not refreshed unnecessarily for property-only edits.
[WORKING] Build: zero errors, zero warnings.

[DEFERRED] Room movement history.
[DEFERRED] Connection retarget/movement history.
[DEFERRED] Savepoint tracking.
[DEFERRED] Grouped bulk-operation history.

## 24. Validation

[WORKING] MapValidationService with read-only model validation (never mutates MapProject).
[WORKING] Validation result model: ValidationSeverity (Error/Warning), ValidationIssue (Code/Message/RoomId/ConnectionId/SeverityLabel/LocationLabel).
[WORKING] Room rules: R001 duplicate IDs (ERROR), R002 empty ID (ERROR), R003 duplicate XYZ (ERROR).
[WORKING] Connection rules: C001 empty ID (ERROR), C002 duplicate ID (ERROR), C003 missing source (ERROR), C004 missing destination (ERROR), C005 self-connection (WARNING).
[WORKING] Coordinate/direction validation: C006 direction mismatch using DirectionHelper.GetCoordinateDelta (ERROR).
[WORKING] Directional exit validation: C007 duplicate direction from same source (ERROR).
[WORKING] Reverse exit validation: C008 missing reverse for bidirectional (ERROR), C009 reverse marked one-way (WARNING).
[WORKING] One-way exits: no reverse required, existing reverse allowed and not flagged.
[WORKING] Door rules: D001 StartsOpen+StartsClosed conflict (ERROR), D002 StartsLocked not closed (ERROR), D003 StartsLocked not lockable (ERROR).
[WORKING] Door sync rules: D004 sync requested but no reverse (ERROR), D005 SharedDoorId mismatch (ERROR), D006 synced config mismatch (ERROR).
[WORKING] Evennia metadata warnings: M001 alias empty Key, M002 tag empty Key, M003 attribute empty Key, M004 empty permission (WARNING).
[WORKING] Validate toolbar button wired to MapValidationService via ViewModel.
[WORKING] ValidationResultsDialog: scrollable issue list with severity/code/message/location, success message when zero issues.
[WORKING] Validation is READ-ONLY: never creates/deletes/moves rooms/exits, never modifies state, never marks project dirty.
[WORKING] Structural reverse matching: source/dest swapped + direction == ReverseDirection (same logic as FindReverseConnection).
[WORKING] Door metadata (DoorAliases/DoorTags/etc.) warnings included in metadata pass.
[OUT OF SCOPE V1] KeyId referential resolution requires an external Evennia object/item catalog. Atlas preserves and exports KeyId as an opaque identifier.

## 25. Map Navigation / Pan / Zoom

[WORKING] ScrollViewer provides two-axis scrolling on 14000x14000 canvas.
[WORKING] Zoom: 25%–300% range, Ctrl+MouseWheel (pointer-centered), toolbar [−] [%] [+].
[WORKING] Zoom is visual-only; logical RoomModel.X/Y/Z, ConnectionModel data, grid coords, and serialization are never altered.
[WORKING] Room hit-testing, connector hit-testing, and selection remain correct at all zoom levels (25%–300%).
[WORKING] Zoom level persists across floor (CurrentZ) changes; resets to 100% on New/Open project.
[WORKING] Center Selected centers the viewport on SelectedRoom, zoom-aware, with cross-floor support.
[WORKING] Fit Map fits all rooms on CurrentZ into the viewport with padding, capped ≤100%, clamped to 25%–300%.
[OUT OF SCOPE V1] No minimap.

## 26. Status Bar

[WORKING] Room count/exits/floor, BuildMode (gold), AutoReverse (blue), Floor (green).
[PARTIAL] Could show selected room title alongside ID/XYZ.

## 27. Context Menus

[WORKING] Right-click on room borders with Connect/Edit/Delete options.
[WORKING] Context menu on connectors. [WORKING] Context menu on empty space or map background.

## 28. Performance

[WORKING] RefreshMap applies viewport-aware culling: only rooms and connections intersecting the visible viewport (+ overscan) are instantiated; scroll triggers deferred Background-priority refresh.
[PARTIAL] Grid drawn for full -50..50 range regardless of room positions.
[WORKING] Lightweight elements (Border, Line, TextBlock). Model is authoritative.
[WORKING] Viewport-aware room/connection culling supports the 2,000-room v1 model target. Additional virtualization not currently required.
[PASS] V1 Acceptance Pass 3 — culling performance verified: at 100% zoom (1200×700 viewport), 169/2000 rooms (8.4%) and 364/7820 connections (4.7%) pass culling. Load in avg 142ms, validation in avg 3668ms. Culling candidate calc: avg 298ms for full 2000-room model.

## 29. Test Map / Acceptance Testing

[WORKING] Canonical acceptance test map created at TestData/V1Acceptance.evenniamap.
[WORKING] Automated structural verification via AcceptanceTests project (21/21 passed).
[WORKING] Save/load round-trip verification (15/15 passed, IDs stable, topology preserved).
[WORKING] Evennia JSON export verification (12/12 passed, Up/Down, one-way, door, KeyId all present).
[WORKING] MapValidationService run against fixture: 0 errors, 0 warnings.
[WORKING] V1 Acceptance Pass 1 complete — structural, round-trip, export verified.
[MANUAL REQUIRED] V1 Acceptance Pass 2 — interactive editor behavior code audit complete; 0 defects found; all interactions require manual GUI exercise.
  Selection (A1-A6): code paths correct; MANUAL for visual highlight confirmation.
  Build Mode (B1-B6): gated by BuildMode bool in OnMapMouseDown; MANUAL for click targeting.
  Auto Reverse / LEDs (C1-C5): CreateConnection checks _autoReverse; DataTrigger binds LEDs; MANUAL for visual confirmation.
  Compass (D1-D6): all 10 buttons route to CompassDirection_Click→NavigateOrBuild; MANUAL for visual confirmation.
  Keyboard (E1-E3): DirectionHelper.KeyToDirection maps all 10 keys; MANUAL for key event confirmation.
  U/D Badges (F1-F8): OnVerticalBadgeClick follows ConnectionModel; MANUAL for visual render + click.
  Drag (G1-G10): OnMouseDown→OnMouseMove→OnMouseUp calls MoveRoom with undo; MANUAL for visual drag.
  XYZ Editing (H1-H10): SelectedRoomX/Y/Z setters→MoveRoom with occupancy/no-op/undo; MANUAL for text input.
  Zoom (I1-I6): Ctrl+Wheel zoom around pointer, plain wheel floor switch, 25%-300% clamp; MANUAL for visual.
  Fit/Center (J1-J8): FitMap CurrentZ filter, cap≤100%; CenterSelectedRoom cross-floor+null guard; MANUAL for visual.
  Room Context Menu (K1-K4): OnRoomRightClick selects room, Edit/Add Exit/Delete; MANUAL for menu interaction.
  Connector Context Menu (L1-L10): OnConnRightClick, Delete Both only for bidirectional; MANUAL for menu interaction.
  Background Context Menu (M1-M10): OnMapRightClick, Create gated by BuildMode+occupancy; MANUAL for menu interaction.
  Undo/Redo (N1-N8): All mutations push UndoAction; Undo()/Redo() pop/restore; MANUAL for stacked undo verification.
[PASS] V1 Acceptance Pass 3 — culling performance (2,000 rooms) complete. 32/32 PASS, 0 FAIL, 1 MANUAL REQUIRED (live WPF visual count).
[NEEDS TESTING] V1 Acceptance Pass 4 — UI polish, visual appearance, documentation.

## 30. Documentation / Release Readiness

[WORKING] README.md with build/run instructions, controls table, format docs.
[PARTIAL] README.md out of date (mentions Ctrl+Mouse Wheel zoom, Connect Rooms button).
[PARTIAL] master-task.md last updated for early phases.
[MISSING] No in-app help, changelog, contributor guide.

---

## Summary

| Status | Approx Count |
|--------|-------------|
| [WORKING] | ~45 items |
| [PARTIAL] | ~18 items |
| [MISSING] | ~55 items |
| [NEEDS TESTING] | ~3 items |

### Priority Next Steps
1. Toolbar LED buttons (BUILD / AUTO REVERSE) — *done in previous pass*
2. Direction compass control
3. Room/exit model expansion (typeclass, attributes, locks, permissions)
4. Validation system
5. Zoom / Fit / Center
6. Clickable U/D indicators — *already implemented, see Section 2*
7. Full door model expansion
8. Performance for 2000+ rooms
9. Test map & acceptance testing
