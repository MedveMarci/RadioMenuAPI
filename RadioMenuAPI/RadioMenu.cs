using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;

namespace RadioMenuAPI;

/// <summary>
///     Represents a radio menu bound to a specific radio item (by serial).
///     Players cycle items with radio range, confirm with radio toggle.
/// </summary>
public class RadioMenu
{
    /// <summary>
    ///     The display name of this menu (shown as a header).
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    ///     Optional tag for identifying or grouping menus. Use with <see cref="RadioMenuManager.GetMenusByTag"/> or <see cref="RadioMenuManager.TryGetMenuByTag"/>.
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
    public RadioMenuItem AddItem(string label, Action<Player, RadioMenuItem>? onSelected = null, string? description = null)
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