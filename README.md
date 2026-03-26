# RadioMenuAPI

[![Version](https://img.shields.io/github/v/release/MedveMarci/RadioMenuAPI?&label=Version&color=d500ff)](https://github.com/MedveMarci/RadioMenuAPI/releases/latest) [![LabAPI Version](https://img.shields.io/badge/LabAPI_Version-1.1.6-b84ee87)](https://github.com/northwood-studios/LabAPI/releases/tag/1.1.6) [![SCP:SL Version](https://img.shields.io/badge/SCP:SL_Version-14.2.6-blue?&color=e5b200)](https://store.steampowered.com/app/700330/SCP_Secret_Laboratory/) [![Total Downloads](https://img.shields.io/github/downloads/MedveMarci/RadioMenuAPI/total.svg?label=Total%20Downloads&color=&color=ffbf00)]()<br>

A [LabAPI](https://github.com/northwood-studios/LabAPI) plugin for [SCP: Secret Laboratory](https://store.steampowered.com/app/700330/SCP_Secret_Laboratory/) that provides an API for creating interactive radio-based menus for players.

---

## How It Works

RadioMenuAPI hooks into three existing radio events and repurposes them as menu navigation controls:

| Player action | Menu action |
|---|---|
| Equip the radio | Open the menu |
| Press **Range** | Cycle to the next item |
| Press **Toggle** (on/off) | Confirm the selected item |
| Unequip the radio | Close the menu |

The current selection and the full item list are shown to the player as an on-screen hint that updates with each press.

---

## Installation

1. Download the latest `RadioMenuAPI.dll` from [Releases](../../releases).
2. Place it in your server's `LabAPI/plugins/global` folder.
3. Restart the server.

Other plugins that depend on RadioMenuAPI must also be placed in the plugins folder. RadioMenuAPI must load before them (it loads alphabetically by filename).

---

## Usage for Developers

Add a reference `RadioMenuAPI.dll` in your project or you can download the NuGet package from [NuGet.org](https://www.nuget.org/packages/RadioMenuAPI/).

### Quick start — give a player a radio menu

```csharp
// Give the player a radio item and immediately attach a menu to it
var menu = player.GiveRadioMenu("Sabotage Menu");

menu.AddItem("Fix Lights",     (p, item) => ActivateLights(p),     "Increase fog on crewmates");
menu.AddItem("Lock Doors",     (p, item) => ActivateDoors(p),      "Seal all doors for 10 seconds");
menu.AddItem("Comms Sabotage", (p, item) => ActivateComms(p),      "Disable task display");
```

You can also construct the entire menu upfront and hand it to a player directly:

```csharp
var menu = new RadioMenu
{
    Title = "Vote",
    Items =
    [
        new RadioMenuItem("Option A", (p, _) => DoA(p)),
        new RadioMenuItem("Option B", (p, _) => DoB(p)),
        new RadioMenuItem("Option C", (p, _) => DoC(p), "Extra description"),
    ],
    OnOpened = (p, m) => p.SendHint($"{m.Title} opened", 2f),
};

// Give the player a radio and attach the pre-built menu to it
player.GiveRadioMenu(menu);
```

### Attach a menu to an existing radio

```csharp
var radio = player.AddItem(ItemType.Radio);
if (radio != null)
{
    var menu = RadioMenuManager.CreateMenu(radio.Serial, "My Menu");
    menu.AddItem("Option A", (p, item) => DoA(p));
    menu.AddItem("Option B", (p, item) => DoB(p));
}
```

### Lifecycle callbacks

```csharp
menu.OnOpened = (player, menu) =>
{
    player.SendHint("Menu opened!", 2f);
};

menu.OnClosed = (player, menu) =>
{
    player.SendHint("Menu closed.", 2f);
};
```

### Static events

RadioMenuAPI exposes four static events on `RadioMenuEvents` that fire for all players and menus:

```csharp
using RadioMenuAPI.Events;

// Fired when any player equips a radio with a menu
RadioMenuEvents.MenuOpened += OnMenuOpened;

// Fired when any player unequips a radio with a menu
RadioMenuEvents.MenuClosed += OnMenuClosed;

// Fired when any player cycles to a new item (Range button)
RadioMenuEvents.ItemChanged += OnItemChanged;

// Fired when any player confirms a selection (Toggle button)
RadioMenuEvents.ItemSelected += OnItemSelected;

void OnMenuOpened(MenuOpenedEventArgs ev)
{
    // ev.Player, ev.Menu
}

void OnMenuClosed(MenuClosedEventArgs ev)
{
    // ev.Player, ev.Menu
}

void OnItemChanged(MenuItemChangedEventArgs ev)
{
    // ev.Player, ev.Menu
    // ev.PreviousItem, ev.PreviousIndex
    // ev.NewItem,      ev.NewIndex
}

void OnItemSelected(MenuItemSelectedEventArgs ev)
{
    // ev.Player, ev.Menu, ev.Item, ev.Index
}
```

### Player-scoped event subscriptions

Use the extension methods on `Player` to subscribe to events for a **specific player only**. The returned delegate must be kept to unsubscribe later.

```csharp
using RadioMenuAPI.Extensions;

// Subscribe
var handler = player.SubscribeItemSelected(ev =>
{
    Log.Info($"{ev.Player.Nickname} selected {ev.Item.Label}");
});

// Unsubscribe when no longer needed
player.UnsubscribeItemSelected(handler);
```

All four player-scoped methods follow the same pattern:

| Subscribe | Unsubscribe |
|---|---|
| `player.SubscribeMenuOpened(handler)` | `player.UnsubscribeMenuOpened(handler)` |
| `player.SubscribeMenuClosed(handler)` | `player.UnsubscribeMenuClosed(handler)` |
| `player.SubscribeItemChanged(handler)` | `player.UnsubscribeItemChanged(handler)` |
| `player.SubscribeItemSelected(handler)` | `player.UnsubscribeItemSelected(handler)` |

### Disable individual items at runtime

```csharp
var killItem = menu.AddItem("Kill Player", (p, item) => KillTarget(p), "Instant kill");

// Disable it later (e.g. during cooldown)
killItem.Enabled = false;
```

Disabled items are shown in grey with a `[disabled]` tag and cannot be selected.

### Query menu state

```csharp
// Player extensions
player.HasActiveRadioMenu();        // bool
player.GetActiveRadioMenu();        // RadioMenu?
player.GetSelectedRadioMenuItem();  // RadioMenuItem?
player.GetSelectedRadioMenuIndex(); // int
player.CloseRadioMenu();            // force-close without unequipping
player.ClearRadioMenus();           // remove all menus from this player

// ReferenceHub extensions
hub.HasActiveRadioMenu();
hub.GetActiveRadioMenu();
// ...

// Static API
RadioMenuManager.TryGetMenu(serial, out var menu);
RadioMenuManager.AssignMenu(serial, menu);
RadioMenuManager.RemoveMenu(serial);
RadioMenuManager.CloseRadioMenu(player);
RadioMenuManager.GetMenusByTag("tag");
RadioMenuManager.TryGetMenuByTag("tag", out var menu);
RadioMenuManager.RemoveInactiveMenus();
RadioMenuManager.ClearAll();
```

---

## RadioMenu options

| Property | Type | Default | Description |
|---|---|---|---|
| `Title` | `string?` | `null` | Header shown above the item list |
| `Tag` | `string?` | `null` | Optional tag for identifying/grouping menus (see `RadioMenuManager.GetMenusByTag`) |
| `Items` | `List<RadioMenuItem>` | `[]` | The selectable items |
| `OnOpened` | `Action<Player, RadioMenu>?` | `null` | Fired when the player equips this radio |
| `OnClosed` | `Action<Player, RadioMenu>?` | `null` | Fired when the player unequips this radio |
| `SuppressDefaultBehavior` | `bool` | `true` | Suppresses battery drain and radio sounds |
| `HintDuration` | `float` | `1` | How long (seconds) the hint stays visible after each input |

## RadioMenuItem options

| Property | Type | Default | Description |
|---|---|---|---|
| `Label` | `string?` | `null` | The item name shown in the list |
| `Description` | `string?` | `null` | Extra line shown below the label when this item is highlighted |
| `Enabled` | `bool` | `true` | Whether the item can be selected. Setting to `false` auto-moves players to the next enabled item |
| `OnSelected` | `Action<Player, RadioMenuItem>?` | `null` | Fired when the player confirms this item. Optional — subscribe to `RadioMenuEvents.ItemSelected` instead if preferred |

---

## Full example

```csharp
using LabApi.Events.CustomHandlers;
using LabApi.Events.Arguments.PlayerEvents;
using RadioMenuAPI;
using RadioMenuAPI.Extensions;

public class MyEventHandler : CustomEventsHandler
{
    public override void OnServerRoundStarted()
    {
        foreach (var player in Player.ReadyList)
        {
            var menu = player.GiveRadioMenu("Actions");

            menu.AddItem("Heal",    (p, _) => p.Health = p.MaxHealth, "Restore full health");
            menu.AddItem("Respawn", (p, _) => RespawnPlayer(p),       "Send to spawn");
            menu.AddItem("Kill",    (p, _) => p.Kill("Radio"),        "Instant death");

            menu.OnOpened = (p, m) => p.SendHint($"<b>{m.Title}</b> opened", 2f);
            menu.OnClosed = (p, m) => p.SendHint("Menu closed", 2f);
        }
    }
}
```

---

## Credits
- Created by [MedveMarci](https://github.com/MedveMarci)
