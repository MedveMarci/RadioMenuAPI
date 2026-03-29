using System;
using LabApi.Features.Wrappers;
using RadioMenuAPI.Events;

namespace RadioMenuAPI.Extensions;

public static class PlayerExtensions
{
    /// <summary>Gives the player a radio and assigns a new menu to it.</summary>
    /// <param name="player">The player to give the menu to.</param>
    /// <param name="title">Optional title displayed as the menu header.</param>
    /// <returns>The created <see cref="RadioMenu"/>, or <c>null</c> if the radio could not be added.</returns>
    public static RadioMenu? GiveRadioMenu(this Player player, string? title = null)
        => RadioMenuManager.GiveRadioMenu(player, title);

    /// <summary>Gives the player a radio and assigns an existing menu to it.</summary>
    /// <param name="player">The player to give the menu to.</param>
    /// <param name="menu">The menu to assign.</param>
    /// <returns><c>true</c> if the radio was successfully added.</returns>
    public static bool GiveRadioMenu(this Player player, RadioMenu menu)
        => RadioMenuManager.GiveRadioMenu(player, menu);

    /// <summary>Gets the <see cref="RadioMenu"/> the player currently has open, or <c>null</c>.</summary>
    /// <param name="player">The player to check.</param>
    public static RadioMenu? GetActiveRadioMenu(this Player player)
    {
        var id = player.ReferenceHub.GetInstanceID();
        if (!RadioMenuManager.PlayerActiveRadio.TryGetValue(id, out var serial)) return null;
        RadioMenuManager.MenusBySerial.TryGetValue(serial, out var menu);
        return menu;
    }

    /// <summary>Gets the currently highlighted <see cref="RadioMenuItem"/>, or <c>null</c>.</summary>
    /// <param name="player">The player to check.</param>
    public static RadioMenuItem? GetSelectedRadioMenuItem(this Player player)
        => RadioMenuManager.GetSelectedItem(player);

    /// <summary>Gets the index of the currently highlighted item. Returns -1 if no active menu.</summary>
    /// <param name="player">The player to check.</param>
    public static int GetSelectedRadioMenuIndex(this Player player)
        => RadioMenuManager.GetSelectedIndex(player);

    /// <summary>Returns <c>true</c> if the player currently has a radio menu open.</summary>
    /// <param name="player">The player to check.</param>
    public static bool HasActiveRadioMenu(this Player player)
        => RadioMenuManager.PlayerActiveRadio.ContainsKey(player.ReferenceHub.GetInstanceID());

    /// <summary>
    ///     Closes the player's active radio menu, fires <see cref="RadioMenu.OnClosed"/>, and clears the hint.
    ///     Does not remove the radio item from inventory.
    /// </summary>
    /// <param name="player">The player whose menu to close.</param>
    public static void CloseRadioMenu(this Player player)
    {
        player.CurrentItem = null;
        RadioMenuManager.CloseRadioMenu(player);
    }

    /// <summary>Removes all radio menus from the player. Does not remove radio items from inventory.</summary>
    /// <param name="player">The player whose menus to clear.</param>
    public static void ClearRadioMenus(this Player player)
    {
        if (player.CurrentItem is { Type: ItemType.Radio })
            player.CurrentItem = null;

        foreach (var item in player.Items)
            if (item.Type == ItemType.Radio)
                RadioMenuManager.MenusBySerial.Remove(item.Serial);

        RadioMenuManager.CleanupPlayer(player);
    }

    /// <summary>
    ///     Subscribes a handler that fires only when <b>this</b> player opens a radio menu.
    ///     Store the returned delegate to unsubscribe later.
    /// </summary>
    /// <param name="player">The player to filter events for.</param>
    /// <param name="handler">The handler to invoke.</param>
    /// <returns>The filtered delegate to pass to <see cref="UnsubscribeMenuOpened"/>.</returns>
    public static Action<MenuOpenedEventArgs> SubscribeMenuOpened(this Player player, Action<MenuOpenedEventArgs> handler)
    {
        Action<MenuOpenedEventArgs> filtered = ev => { if (ev.Player == player) handler(ev); };
        RadioMenuEvents.MenuOpened += filtered;
        return filtered;
    }

    /// <summary>Unsubscribes a handler returned by <see cref="SubscribeMenuOpened"/>.</summary>
    /// <param name="player">The player (unused, for extension method chaining).</param>
    /// <param name="filteredHandler">The filtered delegate returned by <see cref="SubscribeMenuOpened"/>.</param>
    public static void UnsubscribeMenuOpened(this Player player, Action<MenuOpenedEventArgs> filteredHandler)
        => RadioMenuEvents.MenuOpened -= filteredHandler;

    /// <summary>
    ///     Subscribes a handler that fires only when <b>this</b> player closes a radio menu.
    ///     Store the returned delegate to unsubscribe later.
    /// </summary>
    /// <param name="player">The player to filter events for.</param>
    /// <param name="handler">The handler to invoke.</param>
    /// <returns>The filtered delegate to pass to <see cref="UnsubscribeMenuClosed"/>.</returns>
    public static Action<MenuClosedEventArgs> SubscribeMenuClosed(this Player player, Action<MenuClosedEventArgs> handler)
    {
        Action<MenuClosedEventArgs> filtered = ev => { if (ev.Player == player) handler(ev); };
        RadioMenuEvents.MenuClosed += filtered;
        return filtered;
    }

    /// <summary>Unsubscribes a handler returned by <see cref="SubscribeMenuClosed"/>.</summary>
    /// <param name="player">The player (unused, for extension method chaining).</param>
    /// <param name="filteredHandler">The filtered delegate returned by <see cref="SubscribeMenuClosed"/>.</param>
    public static void UnsubscribeMenuClosed(this Player player, Action<MenuClosedEventArgs> filteredHandler)
        => RadioMenuEvents.MenuClosed -= filteredHandler;

    /// <summary>
    ///     Subscribes a handler that fires only when <b>this</b> player cycles menu items.
    ///     Store the returned delegate to unsubscribe later.
    /// </summary>
    /// <param name="player">The player to filter events for.</param>
    /// <param name="handler">The handler to invoke.</param>
    /// <returns>The filtered delegate to pass to <see cref="UnsubscribeItemChanged"/>.</returns>
    public static Action<MenuItemChangedEventArgs> SubscribeItemChanged(this Player player, Action<MenuItemChangedEventArgs> handler)
    {
        Action<MenuItemChangedEventArgs> filtered = ev => { if (ev.Player == player) handler(ev); };
        RadioMenuEvents.ItemChanged += filtered;
        return filtered;
    }

    /// <summary>Unsubscribes a handler returned by <see cref="SubscribeItemChanged"/>.</summary>
    /// <param name="player">The player (unused, for extension method chaining).</param>
    /// <param name="filteredHandler">The filtered delegate returned by <see cref="SubscribeItemChanged"/>.</param>
    public static void UnsubscribeItemChanged(this Player player, Action<MenuItemChangedEventArgs> filteredHandler)
        => RadioMenuEvents.ItemChanged -= filteredHandler;

    /// <summary>
    ///     Subscribes a handler that fires only when <b>this</b> player confirms a menu item.
    ///     Store the returned delegate to unsubscribe later.
    /// </summary>
    /// <param name="player">The player to filter events for.</param>
    /// <param name="handler">The handler to invoke.</param>
    /// <returns>The filtered delegate to pass to <see cref="UnsubscribeItemSelected"/>.</returns>
    public static Action<MenuItemSelectedEventArgs> SubscribeItemSelected(this Player player, Action<MenuItemSelectedEventArgs> handler)
    {
        Action<MenuItemSelectedEventArgs> filtered = ev => { if (ev.Player == player) handler(ev); };
        RadioMenuEvents.ItemSelected += filtered;
        return filtered;
    }

    /// <summary>Unsubscribes a handler returned by <see cref="SubscribeItemSelected"/>.</summary>
    /// <param name="player">The player (unused, for extension method chaining).</param>
    /// <param name="filteredHandler">The filtered delegate returned by <see cref="SubscribeItemSelected"/>.</param>
    public static void UnsubscribeItemSelected(this Player player, Action<MenuItemSelectedEventArgs> filteredHandler)
        => RadioMenuEvents.ItemSelected -= filteredHandler;
}
