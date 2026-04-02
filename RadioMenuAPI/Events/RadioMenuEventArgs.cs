using LabApi.Features.Wrappers;

namespace RadioMenuAPI.Events;

/// <summary>
///     Arguments for when a player opens a radio menu (equips the radio).
/// </summary>
public class MenuOpenedEventArgs(Player player, RadioMenu menu)
{
    /// <summary>The player who opened the menu.</summary>
    public Player Player { get; } = player;

    /// <summary>The menu that was opened.</summary>
    public RadioMenu Menu { get; } = menu;
}

/// <summary>
///     Arguments for when a player closes a radio menu (unequips the radio).
/// </summary>
public class MenuClosedEventArgs(Player player, RadioMenu menu)
{
    /// <summary>The player who closed the menu.</summary>
    public Player Player { get; } = player;

    /// <summary>The menu that was closed.</summary>
    public RadioMenu Menu { get; } = menu;
}

/// <summary>
///     Arguments for when a player navigates to a different menu item (presses Range).
/// </summary>
public class MenuItemChangedEventArgs(
    Player player,
    RadioMenu menu,
    RadioMenuItem previousItem,
    int previousIndex,
    RadioMenuItem newItem,
    int newIndex)
{
    /// <summary>The player who changed the selection.</summary>
    public Player Player { get; } = player;

    /// <summary>The menu being navigated.</summary>
    public RadioMenu Menu { get; } = menu;

    /// <summary>The previously highlighted item.</summary>
    public RadioMenuItem PreviousItem { get; } = previousItem;

    /// <summary>The index of the previously highlighted item.</summary>
    public int PreviousIndex { get; } = previousIndex;

    /// <summary>The newly highlighted item.</summary>
    public RadioMenuItem NewItem { get; } = newItem;

    /// <summary>The index of the newly highlighted item.</summary>
    public int NewIndex { get; } = newIndex;
}

/// <summary>
///     Arguments for when a player confirms/selects a menu item (presses Toggle).
/// </summary>
public class MenuItemSelectedEventArgs(Player player, RadioMenu menu, RadioMenuItem item, int index)
{
    /// <summary>The player who selected the item.</summary>
    public Player Player { get; } = player;

    /// <summary>The menu the item belongs to.</summary>
    public RadioMenu Menu { get; } = menu;

    /// <summary>The item that was selected.</summary>
    public RadioMenuItem Item { get; } = item;

    /// <summary>The index of the selected item.</summary>
    public int Index { get; } = index;
}