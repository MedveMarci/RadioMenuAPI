using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using MEC;
using RadioMenuAPI.Events;

namespace RadioMenuAPI;

public static class RadioMenuManager
{
    internal static Dictionary<ushort, RadioMenu> MenusBySerial { get; } = new();
    internal static Dictionary<int, int> PlayerSelections { get; } = new();
    internal static Dictionary<int, int> PlayerLockedSelections { get; } = new();
    internal static Dictionary<int, ushort> PlayerActiveRadio { get; } = new();
    internal static Dictionary<int, CoroutineHandle> PlayerHintCoroutines { get; } = new();

    /// <summary>Assigns a <see cref="RadioMenu" /> to a radio item by serial number.</summary>
    /// <param name="radioSerial">The serial number of the radio item.</param>
    /// <param name="menu">The menu to assign.</param>
    public static void AssignMenu(ushort radioSerial, RadioMenu menu)
    {
        MenusBySerial[radioSerial] = menu;
    }

    /// <summary>Removes the menu assigned to the given radio serial and closes it for any player currently using it.</summary>
    /// <returns>True if a menu was removed.</returns>
    public static bool RemoveMenu(ushort radioSerial)
    {
        if (!MenusBySerial.ContainsKey(radioSerial))
            return false;

        foreach (var entry in PlayerActiveRadio.Where(e => e.Value == radioSerial).ToList())
        {
            var player = Player.ReadyList.FirstOrDefault(p => p.ReferenceHub.GetInstanceID() == entry.Key);
            if (player != null)
            {
                CloseRadioMenu(player);
            }
            else
            {
                var id = entry.Key;
                PlayerActiveRadio.Remove(id);
                PlayerSelections.Remove(id);
                PlayerLockedSelections.Remove(id);
                if (PlayerHintCoroutines.TryGetValue(id, out var handle))
                {
                    Timing.KillCoroutines(handle);
                    PlayerHintCoroutines.Remove(id);
                }
            }
        }

        return MenusBySerial.Remove(radioSerial);
    }

    /// <summary>Tries to get the menu assigned to a radio item.</summary>
    /// <param name="radioSerial">The serial number of the radio item.</param>
    /// <param name="menu">The menu, if found.</param>
    /// <returns>True if a menu is assigned to this radio.</returns>
    public static bool TryGetMenu(ushort radioSerial, out RadioMenu menu)
    {
        return MenusBySerial.TryGetValue(radioSerial, out menu);
    }

    /// <summary>Creates a new <see cref="RadioMenu" />, assigns it to a radio item, and returns it.</summary>
    /// <param name="radioSerial">The serial number of the radio item.</param>
    /// <param name="title">Optional display title shown as the menu header.</param>
    public static RadioMenu CreateMenu(ushort radioSerial, string? title = null)
    {
        var menu = new RadioMenu { Title = title };
        MenusBySerial[radioSerial] = menu;
        return menu;
    }

    /// <summary>
    ///     Gives the player a radio item and creates a new menu for it.
    /// </summary>
    /// <param name="player">The player to give the radio to.</param>
    /// <param name="title">Optional display title shown as the menu header.</param>
    /// <returns>The created <see cref="RadioMenu" />, or <c>null</c> if the radio could not be added.</returns>
    public static RadioMenu? GiveRadioMenu(Player player, string? title = null)
    {
        var item = player.AddItem(ItemType.Radio);
        if (item == null) return null;

        var menu = new RadioMenu { Title = title };
        MenusBySerial[item.Serial] = menu;
        return menu;
    }

    /// <summary>
    ///     Gives the player a radio item and assigns an existing menu to it.
    /// </summary>
    /// <param name="player">The player to give the radio to.</param>
    /// <param name="menu">The menu to assign.</param>
    /// <returns>True if the radio was successfully added.</returns>
    public static bool GiveRadioMenu(Player player, RadioMenu menu)
    {
        var item = player.AddItem(ItemType.Radio);
        if (item == null) return false;

        MenusBySerial[item.Serial] = menu;
        return true;
    }

    /// <summary>Returns all menus that have the given <see cref="RadioMenu.Tag" />.</summary>
    public static IReadOnlyList<RadioMenu> GetMenusByTag(string tag)
    {
        return MenusBySerial.Values.Where(m => m.Tag == tag).ToList();
    }

    /// <summary>Gets the first menu with the given <see cref="RadioMenu.Tag" />.</summary>
    /// <returns>True if a matching menu was found.</returns>
    public static bool TryGetMenuByTag(string tag, out RadioMenu? menu)
    {
        menu = MenusBySerial.Values.FirstOrDefault(m => m.Tag == tag);
        return menu != null;
    }

    /// <summary>Gets the currently highlighted item index for a player. Returns -1 if no active menu.</summary>
    public static int GetSelectedIndex(Player player)
    {
        return PlayerSelections.TryGetValue(player.ReferenceHub.GetInstanceID(), out var idx) ? idx : -1;
    }

    /// <summary>
    ///     Returns the index of the item the player has locked via <see cref="RadioMenu.LockOnSelect" />,
    ///     or -1 if nothing is locked.
    /// </summary>
    public static int GetLockedIndex(Player player)
    {
        return PlayerLockedSelections.TryGetValue(player.ReferenceHub.GetInstanceID(), out var idx) ? idx : -1;
    }

    /// <summary>
    ///     Returns the locked <see cref="RadioMenuItem" /> for a player, or <c>null</c> if nothing is locked.
    /// </summary>
    public static RadioMenuItem? GetLockedItem(Player player)
    {
        var id = player.ReferenceHub.GetInstanceID();
        if (!PlayerLockedSelections.TryGetValue(id, out var lockedIdx)) return null;
        if (!PlayerActiveRadio.TryGetValue(id, out var serial)) return null;
        if (!MenusBySerial.TryGetValue(serial, out var menu)) return null;
        if (lockedIdx < 0 || lockedIdx >= menu.Items.Count) return null;
        return menu.Items[lockedIdx];
    }

    /// <summary>Gets the currently highlighted <see cref="RadioMenuItem" /> for a player, or <c>null</c>.</summary>
    public static RadioMenuItem? GetSelectedItem(Player player)
    {
        var id = player.ReferenceHub.GetInstanceID();
        if (!PlayerActiveRadio.TryGetValue(id, out var serial)) return null;
        if (!MenusBySerial.TryGetValue(serial, out var menu)) return null;
        if (!PlayerSelections.TryGetValue(id, out var idx)) return null;
        if (menu.Items.Count == 0) return null;
        return menu.Items[idx % menu.Items.Count];
    }

    /// <summary>Returns true if the player currently has a radio menu open (i.e. is holding a radio with a menu).</summary>
    public static bool IsMenuOpen(Player player)
    {
        return PlayerActiveRadio.ContainsKey(player.ReferenceHub.GetInstanceID());
    }

    /// <summary>Removes all menus and player state. Call this on round restart or plugin disable.</summary>
    public static void ClearAll()
    {
        MenusBySerial.Clear();
        PlayerSelections.Clear();
        PlayerLockedSelections.Clear();
        PlayerActiveRadio.Clear();
        foreach (var handle in PlayerHintCoroutines.Values)
            Timing.KillCoroutines(handle);
        PlayerHintCoroutines.Clear();
    }

    /// <summary>Removes all state for a specific player. Called automatically on disconnect.</summary>
    public static void CleanupPlayer(Player player)
    {
        var id = player.ReferenceHub.GetInstanceID();
        PlayerSelections.Remove(id);
        PlayerLockedSelections.Remove(id);
        PlayerActiveRadio.Remove(id);
        if (!PlayerHintCoroutines.TryGetValue(id, out var handle))
            return;
        Timing.KillCoroutines(handle);
        PlayerHintCoroutines.Remove(id);
    }

    /// <summary>
    ///     Closes the active radio menu for a player: fires <see cref="RadioMenu.OnClosed" />,
    ///     clears internal state, and clears the on-screen hint.
    ///     Does not remove the radio item from the player's inventory.
    /// </summary>
    public static void CloseRadioMenu(Player player)
    {
        var id = player.ReferenceHub.GetInstanceID();

        if (PlayerActiveRadio.TryGetValue(id, out var serial) &&
            MenusBySerial.TryGetValue(serial, out var menu))
        {
            try
            {
                menu.OnClosed?.Invoke(player, menu);
            }
            catch (Exception ex)
            {
                Logger.Error($"[RadioMenuAPI] OnClosed error: {ex}");
            }

            RadioMenuEvents.InvokeMenuClosed(new MenuClosedEventArgs(player, menu));
        }

        PlayerActiveRadio.Remove(id);
        PlayerSelections.Remove(id);
        PlayerLockedSelections.Remove(id);

        if (PlayerHintCoroutines.TryGetValue(id, out var handle))
        {
            Timing.KillCoroutines(handle);
            PlayerHintCoroutines.Remove(id);
        }

        player.SendHint("", 0.1f);
    }

    internal static void BumpPlayersOffItem(RadioMenuItem disabledItem)
    {
        foreach (var entry in PlayerActiveRadio)
        {
            var playerId = entry.Key;
            var serial = entry.Value;
            if (!MenusBySerial.TryGetValue(serial, out var menu)) continue;
            if (!PlayerSelections.TryGetValue(playerId, out var idx)) continue;
            if (idx >= menu.Items.Count || menu.Items[idx] != disabledItem) continue;

            var next = FindNextEnabled(menu, idx);
            PlayerSelections[playerId] = next;

            var player = Player.ReadyList.FirstOrDefault(p => p.ReferenceHub.GetInstanceID() == playerId);
            if (player != null)
                RadioMenuEventHandler.ShowMenuHint(player, menu, next);
        }
    }

    private static int FindNextEnabled(RadioMenu menu, int startIndex)
    {
        var count = menu.Items.Count;
        for (var i = 1; i < count; i++)
        {
            var candidate = (startIndex + i) % count;
            if (menu.Items[candidate].Enabled)
                return candidate;
        }

        return startIndex;
    }

    /// <summary>Removes menus assigned to radio items that no longer exist in any player's inventory.</summary>
    public static void RemoveInactiveMenus()
    {
        var allSerials = new HashSet<ushort>();
        foreach (var p in Player.ReadyList)
        foreach (var item in p.Items)
            if (item.Type == ItemType.Radio)
                allSerials.Add(item.Serial);

        foreach (var serial in MenusBySerial.Keys.Where(s => !allSerials.Contains(s)).ToList())
            MenusBySerial.Remove(serial);
    }
}