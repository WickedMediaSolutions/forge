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
[WOKING] KeyName fallback: non-empty KeyName exported as exit key; empty KeyName falls back to direction string.
[WOKING] Two-sided doors: each ConnectionModel exported independently; SharedDoorId describes relationship.
[WOKING] One-way exits exported exactly as modeled; export never mutates map.
[WOKING] Empty collections emitted as []; no invented placeholder data.
[WOKING] Backward compatible: missing/empty new fields do not cause exceptions.
[WOKING] Deterministic export (no timestamps, random IDs, or changing values).
[WOKING] Export is READ-ONLY: never creates/deletes rooms/exits, never modifies coordinates/metadata, never marks project dirty.