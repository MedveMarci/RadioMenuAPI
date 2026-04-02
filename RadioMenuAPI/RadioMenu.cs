using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;

namespace RadioMenuAPI;

public class RadioMenu
{
    /// <summary>
    ///     The display name of this menu (shown as a header).
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    ///     Optional tag for identifying or grouping menus. Use with <see cref="RadioMenuManager.GetMenusByTag" /> or
    ///     <see cref="RadioMenuManager.TryGetMenuByTag" />.
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    ///     The list of items in this menu.
    /// </summary>
    public List<RadioMenuItem> Items { get; init; } = [];

    /// <summary>
    ///     Called when the menu is opened (player equips the radio).
    /// </summary>
    public Action<Player, RadioMenu>? OnOpened { get; set; }

    /// <summary>
    ///     Called when the menu is closed (player unequips the radio).
    /// </summary>
    public Action<Player, RadioMenu>? OnClosed { get; set; }

    /// <summary>
    ///     Controls how items are displayed. <see cref="MenuDisplayMode.List" /> shows all items at once;
    ///     <see cref="MenuDisplayMode.Pager" /> shows one item at a time and cycles with Range.
    ///     Default: <see cref="MenuDisplayMode.List" />.
    /// </summary>
    public MenuDisplayMode DisplayMode { get; set; } = MenuDisplayMode.List;

    /// <summary>
    ///     If true, confirming an item (Toggle) locks the selection: the player cannot navigate or
    ///     select other items until they toggle the same item again to unlock it.
    ///     Default: false.
    /// </summary>
    public bool LockOnSelect { get; set; } = false;

    /// <summary>
    ///     If true, the default radio behavior (battery drain, sound) is suppressed.
    ///     Default: true.
    /// </summary>
    public bool SuppressDefaultBehavior { get; set; } = true;

    /// <summary>
    ///     Duration in seconds for the hint display. Default: 1.5s.
    /// </summary>
    public float HintDuration { get; set; } = 1f;

    /// <summary>
    ///     Adds a new item to the menu.
    /// </summary>
    public RadioMenuItem AddItem(string label, Action<Player, RadioMenuItem>? onSelected = null,
        string? description = null)
    {
        var item = new RadioMenuItem(label, onSelected, description);
        Items.Add(item);
        return item;
    }

    /// <summary>
    ///     Removes an item from the menu.
    /// </summary>
    public bool RemoveItem(RadioMenuItem item)
    {
        return Items.Remove(item);
    }

    /// <summary>
    ///     Removes all items from the menu.
    /// </summary>
    public void ClearItems()
    {
        Items.Clear();
    }
}