# Evennia Map Maker

A Windows desktop application for visually creating and editing MUD areas for Evennia.

## Build & Run

### Prerequisites
- .NET 8 SDK or later
- Windows 10/11

### Build
```powershell
cd MapMaker
dotnet build
```

### Run
```powershell
dotnet run --project EvenniaMapMaker
```

Or run the built executable:
```powershell
.\EvenniaMapMaker\bin\Debug\net8.0-windows\EvenniaMapMaker.exe
```

## Basic Controls

| Action | Control |
|---|---|
| Create Room | Left-click empty map space |
| Select Room | Left-click room |
| Edit Room | Double-click room or select + use properties panel |
| Drag Room | Left-click + drag room |
| Delete Room | Select room + press Delete |
| Connect Rooms | Click "Connect Rooms" toolbar button, then click source room, then destination room, choose direction |
| Select Connection | Click connection line or label on map |
| Delete Connection | Select connection + press Delete |
| Zoom | Ctrl + Mouse Wheel |
| Undo | Ctrl+Z or toolbar button |
| Redo | Ctrl+Y or toolbar button |
| Save | Ctrl+S or toolbar button |
| New Project | Click "New" in toolbar |

## Map Project Format (.evenniamap)

Projects are saved as JSON with the `.evenniamap` extension.

```json
{
  "version": 1,
  "id": "silvermere",
  "name": "Silvermere",
  "rooms": [
    {
      "id": "silvermere_room_0001",
      "title": "Town Square",
      "description": "The dark heart of Silvermere...",
      "x": 500,
      "y": 300,
      "z": 0,
      "tags": ["town_square", "evil_start"],
      "notes": ""
    }
  ],
  "connections": [
    {
      "id": "connection_0001",
      "sourceRoomId": "silvermere_room_0001",
      "destinationRoomId": "silvermere_room_0002",
      "direction": "north",
      "reverseDirection": "south",
      "isOneWay": false,
      "exitType": "normal",
      "aliases": "",
      "sharedDoorId": "",
      "door": null,
      "autoCreateReverse": true
    }
  ]
}
```

## Evennia Export Format (.evennia.json)

Export via **Export Evennia** toolbar button. Generates a structured JSON file for Evennia consumption.

```json
{
  "areaId": "silvermere",
  "areaName": "Silvermere",
  "version": 1,
  "rooms": [
    {
      "id": "silvermere_room_0001",
      "title": "Town Square",
      "description": "...",
      "x": 500,
      "y": 300,
      "z": 0,
      "tags": ["town_square"],
      "notes": ""
    }
  ],
  "exits": [
    {
      "sourceRoomId": "silvermere_room_0001",
      "destinationRoomId": "silvermere_room_0002",
      "direction": "north",
      "reverseDirection": "south",
      "isOneWay": false,
      "exitType": "normal",
      "aliases": [],
      "door": null
    }
  ]
}
```

## Directions Supported

- North, South, East, West
- Northeast, Northwest, Southeast, Southwest
- Up, Down

## Door Configuration

Exits can be configured as doors with:
- Door Name
- Starts Open / Starts Closed / Starts Locked
- Key ID for locked doors
- Door state is synchronized across both directions via shared door ID

## Architecture

- **Framework**: .NET 8, WPF
- **Pattern**: MVVM (lightweight)
- **Models**: `RoomModel`, `ConnectionModel`, `DoorModel`, `MapProject`
- **Services**: `ProjectFileService`, `EvenniaExportService`
- **ViewModels**: `MainViewModel`
- **Controls**: `MapCanvas` (custom zoomable/pannable map)

## This application is ONLY for Evennia

No support for ROM, CircleMUD, CoffeeMUD, SMAUG, Diku, or any other MUD engine.