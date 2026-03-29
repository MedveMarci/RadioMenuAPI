using LabApi.Features.Wrappers;

namespace RadioMenuAPI.Extensions;

public static class ReferenceHubExtensions
{
    /// <summary>Gives the player a radio and assigns a new menu to it.</summary>
    /// <param name="hub">The reference hub of the player.</param>
    /// <param name="title">Optional title displayed as the menu header.</param>
    /// <returns>The created <see cref="RadioMenu"/>, or <c>null</c> if the radio could not be added.</returns>
    public static RadioMenu? GiveRadioMenu(this ReferenceHub hub, string? title = null)
    {
        var player = Player.Get(hub);
        return player == null ? null : RadioMenuManager.GiveRadioMenu(player, title);
    }

    /// <summary>Gives the player a radio and assigns an existing menu to it.</summary>
    /// <param name="hub">The reference hub of the player.</param>
    /// <param name="menu">The menu to assign.</param>
    /// <returns><c>true</c> if the radio was successfully added.</returns>
    public static bool GiveRadioMenu(this ReferenceHub hub, RadioMenu menu)
    {
        var player = Player.Get(hub);
        return player != null && RadioMenuManager.GiveRadioMenu(player, menu);
    }

    /// <summary>Gets the <see cref="RadioMenu"/> the player currently has open, or <c>null</c>.</summary>
    /// <param name="hub">The reference hub of the player.</param>
    public static RadioMenu? GetActiveRadioMenu(this ReferenceHub hub)
    {
        var id = hub.GetInstanceID();
        if (!RadioMenuManager.PlayerActiveRadio.TryGetValue(id, out var serial)) return null;
        RadioMenuManager.MenusBySerial.TryGetValue(serial, out var menu);
        return menu;
    }

    /// <summary>Gets the currently highlighted <see cref="RadioMenuItem"/>, or <c>null</c>.</summary>
    /// <param name="hub">The reference hub of the player.</param>
    public static RadioMenuItem? GetSelectedRadioMenuItem(this ReferenceHub hub)
    {
        var player = Player.Get(hub);
        return player == null ? null : RadioMenuManager.GetSelectedItem(player);
    }

    /// <summary>Gets the index of the currently highlighted item. Returns -1 if no active menu.</summary>
    /// <param name="hub">The reference hub of the player.</param>
    public static int GetSelectedRadioMenuIndex(this ReferenceHub hub)
    {
        var id = hub.GetInstanceID();
        return RadioMenuManager.PlayerSelections.TryGetValue(id, out var idx) ? idx : -1;
    }

    /// <summary>Returns <c>true</c> if the player currently has a radio menu open.</summary>
    /// <param name="hub">The reference hub of the player.</param>
    public static bool HasActiveRadioMenu(this ReferenceHub hub)
        => RadioMenuManager.PlayerActiveRadio.ContainsKey(hub.GetInstanceID());

    /// <summary>
    ///     Closes the player's active radio menu, fires <see cref="RadioMenu.OnClosed"/>, and clears the hint.
    ///     Does not remove the radio item from inventory.
    /// </summary>
    /// <param name="hub">The reference hub of the player.</param>
    public static void CloseRadioMenu(this ReferenceHub hub)
    {
        var player = Player.Get(hub);
        if (player == null) return;
        player.CurrentItem = null;
        RadioMenuManager.CloseRadioMenu(player);
    }

    /// <summary>Removes all radio menus from the player. Does not remove radio items from inventory.</summary>
    /// <param name="hub">The reference hub of the player.</param>
    public static void ClearRadioMenus(this ReferenceHub hub)
    {
        var player = Player.Get(hub);
        if (player == null) return;

        if (player.CurrentItem is { Type: ItemType.Radio })
            player.CurrentItem = null;

        foreach (var item in player.Items)
            if (item.Type == ItemType.Radio)
                RadioMenuManager.MenusBySerial.Remove(item.Serial);

        RadioMenuManager.CleanupPlayer(player);
    }
}
