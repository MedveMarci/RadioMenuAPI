using System;
using LabApi.Features.Wrappers;
using RadioMenuAPI.Events;

namespace RadioMenuAPI.Extensions;

public static class PlayerExtensions
{
    extension(Player player)
    {
        /// <summary>Gives the player a radio and assigns a new menu to it.</summary>
        /// <param name="title">Optional title displayed as the menu header.</param>
        /// <returns>The created <see cref="RadioMenu"/>, or <c>null</c> if the radio could not be added.</returns>
        public RadioMenu? GiveRadioMenu(string? title = null)
            => RadioMenuManager.GiveRadioMenu(player, title);

        /// <summary>Gives the player a radio and assigns an existing menu to it.</summary>
        /// <param name="menu">The menu to assign.</param>
        /// <returns><c>true</c> if the radio was successfully added.</returns>
        public bool GiveRadioMenu(RadioMenu menu)
            => RadioMenuManager.GiveRadioMenu(player, menu);

        /// <summary>Gets the <see cref="RadioMenu"/> the player currently has open, or <c>null</c>.</summary>
        public RadioMenu? GetActiveRadioMenu()
        {
            var id = player.ReferenceHub.GetInstanceID();
            if (!RadioMenuManager.PlayerActiveRadio.TryGetValue(id, out var serial)) return null;
            RadioMenuManager.MenusBySerial.TryGetValue(serial, out var menu);
            return menu;
        }

        /// <summary>Gets the currently highlighted <see cref="RadioMenuItem"/>, or <c>null</c>.</summary>
        public RadioMenuItem? GetSelectedRadioMenuItem()
            => RadioMenuManager.GetSelectedItem(player);

        /// <summary>Gets the index of the currently highlighted item. Returns -1 if no active menu.</summary>
        public int GetSelectedRadioMenuIndex()
            => RadioMenuManager.GetSelectedIndex(player);

        /// <summary>Returns <c>true</c> if the player currently has a radio menu open.</summary>
        public bool HasActiveRadioMenu()
            => RadioMenuManager.PlayerActiveRadio.ContainsKey(player.ReferenceHub.GetInstanceID());

        /// <summary>
        ///     Closes the player's active radio menu, fires <see cref="RadioMenu.OnClosed"/>, and clears the hint.
        ///     Does not remove the radio item from inventory.
        /// </summary>
        public void CloseRadioMenu()
        {
            player.CurrentItem = null;
            RadioMenuManager.CloseRadioMenu(player);
        }

        /// <summary>Removes all radio menus from the player. Does not remove radio items from inventory.</summary>
        public void ClearRadioMenus()
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
        public Action<MenuOpenedEventArgs> SubscribeMenuOpened(Action<MenuOpenedEventArgs> handler)
        {
            Action<MenuOpenedEventArgs> filtered = ev => { if (ev.Player == player) handler(ev); };
            RadioMenuEvents.MenuOpened += filtered;
            return filtered;
        }

        /// <summary>Unsubscribes a handler returned by <see cref="SubscribeMenuOpened"/>.</summary>
        public void UnsubscribeMenuOpened(Action<MenuOpenedEventArgs> filteredHandler)
            => RadioMenuEvents.MenuOpened -= filteredHandler;

        /// <summary>
        ///     Subscribes a handler that fires only when <b>this</b> player closes a radio menu.
        ///     Store the returned delegate to unsubscribe later.
        /// </summary>
        public Action<MenuClosedEventArgs> SubscribeMenuClosed(Action<MenuClosedEventArgs> handler)
        {
            Action<MenuClosedEventArgs> filtered = ev => { if (ev.Player == player) handler(ev); };
            RadioMenuEvents.MenuClosed += filtered;
            return filtered;
        }

        /// <summary>Unsubscribes a handler returned by <see cref="SubscribeMenuClosed"/>.</summary>
        public void UnsubscribeMenuClosed(Action<MenuClosedEventArgs> filteredHandler)
            => RadioMenuEvents.MenuClosed -= filteredHandler;

        /// <summary>
        ///     Subscribes a handler that fires only when <b>this</b> player cycles menu items.
        ///     Store the returned delegate to unsubscribe later.
        /// </summary>
        public Action<MenuItemChangedEventArgs> SubscribeItemChanged(Action<MenuItemChangedEventArgs> handler)
        {
            Action<MenuItemChangedEventArgs> filtered = ev => { if (ev.Player == player) handler(ev); };
            RadioMenuEvents.ItemChanged += filtered;
            return filtered;
        }

        /// <summary>Unsubscribes a handler returned by <see cref="SubscribeItemChanged"/>.</summary>
        public void UnsubscribeItemChanged(Action<MenuItemChangedEventArgs> filteredHandler)
            => RadioMenuEvents.ItemChanged -= filteredHandler;

        /// <summary>
        ///     Subscribes a handler that fires only when <b>this</b> player confirms a menu item.
        ///     Store the returned delegate to unsubscribe later.
        /// </summary>
        public Action<MenuItemSelectedEventArgs> SubscribeItemSelected(Action<MenuItemSelectedEventArgs> handler)
        {
            Action<MenuItemSelectedEventArgs> filtered = ev => { if (ev.Player == player) handler(ev); };
            RadioMenuEvents.ItemSelected += filtered;
            return filtered;
        }

        /// <summary>Unsubscribes a handler returned by <see cref="SubscribeItemSelected"/>.</summary>
        public void UnsubscribeItemSelected(Action<MenuItemSelectedEventArgs> filteredHandler)
            => RadioMenuEvents.ItemSelected -= filteredHandler;
    }
}
