using System;
using LabApi.Features.Wrappers;
using RadioMenuAPI.Events;

namespace RadioMenuAPI;

public class RadioMenuItem
{
    /// <summary>Creates an item using object-initializer syntax.</summary>
    public RadioMenuItem()
    {
    }

    /// <param name="label">The display label.</param>
    public RadioMenuItem(string label)
    {
        Label = label;
    }

    /// <param name="label">The display label.</param>
    /// <param name="description">Optional description shown when highlighted.</param>
    public RadioMenuItem(string label, string description)
    {
        Label = label;
        Description = description;
    }

    /// <param name="label">The display label.</param>
    /// <param name="onSelected">Callback fired when the player confirms this item.</param>
    /// <param name="description">Optional description shown when highlighted.</param>
    public RadioMenuItem(string label, Action<Player, RadioMenuItem>? onSelected, string? description = null)
    {
        Label = label;
        OnSelected = onSelected;
        Description = description;
    }

    /// <summary>The display label shown to the player.</summary>
    public string? Label { get; set; }

    /// <summary>Optional description shown below the label when this item is highlighted.</summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Whether this item can be selected. Disabled items are shown in grey and cannot be confirmed.
    ///     Setting this to <c>false</c> automatically moves any player currently on this item to the next enabled item.
    /// </summary>
    public bool Enabled
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            if (!value)
                RadioMenuManager.BumpPlayersOffItem(this);
        }
    } = true;

    /// <summary>
    ///     Called when the player confirms this item (Toggle button).
    ///     Optional — use <see cref="RadioMenuEvents.ItemSelected" /> instead if you prefer event-based handling.
    /// </summary>
    public Action<Player, RadioMenuItem>? OnSelected { get; set; }

    /// <summary>
    ///     Called when the player unlocks a previously locked item by toggling it again.
    ///     Only relevant when <see cref="RadioMenu.LockOnSelect" /> is true.
    /// </summary>
    public Action<Player, RadioMenuItem>? OnDeselected { get; set; }
}