using System;
using System.Collections.Generic;
using System.Text;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using MEC;
using RadioMenuAPI.Events;

namespace RadioMenuAPI;

internal class RadioMenuEventHandler : CustomEventsHandler
{
    public override void OnPlayerChangingItem(PlayerChangingItemEventArgs ev)
    {
        var playerId = ev.Player.ReferenceHub.GetInstanceID();

        if (ev.OldItem is { Type: ItemType.Radio } &&
            RadioMenuManager.PlayerActiveRadio.TryGetValue(playerId, out var oldSerial) &&
            oldSerial == ev.OldItem.Serial)
            RadioMenuManager.CloseRadioMenu(ev.Player);

        if (ev.NewItem is { Type: ItemType.Radio } &&
            RadioMenuManager.MenusBySerial.TryGetValue(ev.NewItem.Serial, out var newMenu))
        {
            RadioMenuManager.PlayerActiveRadio[playerId] = ev.NewItem.Serial;
            RadioMenuManager.PlayerSelections[playerId] = 0;
            RadioMenuManager.PlayerLockedSelections.Remove(playerId);

            try
            {
                newMenu.OnOpened?.Invoke(ev.Player, newMenu);
            }
            catch (Exception ex)
            {
                Logger.Error($"[RadioMenuAPI] OnOpened error: {ex}");
            }

            RadioMenuEvents.InvokeMenuOpened(new MenuOpenedEventArgs(ev.Player, newMenu));
            ShowMenuHint(ev.Player, newMenu, 0);

            if (RadioMenuManager.PlayerHintCoroutines.TryGetValue(playerId, out var oldCoroutine))
                Timing.KillCoroutines(oldCoroutine);
            RadioMenuManager.PlayerHintCoroutines[playerId] =
                Timing.RunCoroutine(HintRefreshCoroutine(ev.Player, playerId, newMenu));
        }

        base.OnPlayerChangingItem(ev);
    }

    public override void OnPlayerChangingRadioRange(PlayerChangingRadioRangeEventArgs ev)
    {
        if (!RadioMenuManager.MenusBySerial.TryGetValue(ev.RadioItem.Serial, out var menu))
        {
            base.OnPlayerChangingRadioRange(ev);
            return;
        }

        if (menu.SuppressDefaultBehavior)
            ev.IsAllowed = false;

        if (menu.Items.Count == 0)
        {
            base.OnPlayerChangingRadioRange(ev);
            return;
        }

        var playerId = ev.Player.ReferenceHub.GetInstanceID();

        // Block navigation while an item is locked
        if (menu.LockOnSelect && RadioMenuManager.PlayerLockedSelections.ContainsKey(playerId))
        {
            base.OnPlayerChangingRadioRange(ev);
            return;
        }

        RadioMenuManager.PlayerSelections.TryGetValue(playerId, out var previousIndex);

        var newIndex = (previousIndex + 1) % menu.Items.Count;
        RadioMenuManager.PlayerSelections[playerId] = newIndex;

        RadioMenuEvents.InvokeItemChanged(new MenuItemChangedEventArgs(
            ev.Player, menu,
            menu.Items[previousIndex], previousIndex,
            menu.Items[newIndex], newIndex));

        ShowMenuHint(ev.Player, menu, newIndex);
        base.OnPlayerChangingRadioRange(ev);
    }

    public override void OnPlayerTogglingRadio(PlayerTogglingRadioEventArgs ev)
    {
        if (!RadioMenuManager.MenusBySerial.TryGetValue(ev.RadioItem.Serial, out var menu))
        {
            base.OnPlayerTogglingRadio(ev);
            return;
        }

        if (menu.SuppressDefaultBehavior)
            ev.IsAllowed = false;

        if (menu.Items.Count == 0)
        {
            base.OnPlayerTogglingRadio(ev);
            return;
        }

        var playerId = ev.Player.ReferenceHub.GetInstanceID();
        if (!RadioMenuManager.PlayerSelections.TryGetValue(playerId, out var idx))
        {
            base.OnPlayerTogglingRadio(ev);
            return;
        }

        var selectedIndex = idx % menu.Items.Count;
        var item = menu.Items[selectedIndex];

        if (!item.Enabled)
        {
            base.OnPlayerTogglingRadio(ev);
            return;
        }

        if (menu.LockOnSelect)
        {
            if (RadioMenuManager.PlayerLockedSelections.TryGetValue(playerId, out var lockedIdx) &&
                lockedIdx == selectedIndex)
            {
                RadioMenuManager.PlayerLockedSelections.Remove(playerId);
                try
                {
                    item.OnDeselected?.Invoke(ev.Player, item);
                }
                catch (Exception ex)
                {
                    Logger.Error($"[RadioMenuAPI] OnDeselected error: {ex}");
                }

                ShowMenuHint(ev.Player, menu, selectedIndex);
                base.OnPlayerTogglingRadio(ev);
                return;
            }

            RadioMenuManager.PlayerLockedSelections[playerId] = selectedIndex;
        }

        try
        {
            item.OnSelected?.Invoke(ev.Player, item);
        }
        catch (Exception ex)
        {
            Logger.Error($"[RadioMenuAPI] OnSelected error: {ex}");
        }

        RadioMenuEvents.InvokeItemSelected(new MenuItemSelectedEventArgs(ev.Player, menu, item, selectedIndex));
        ShowMenuHint(ev.Player, menu, selectedIndex);
        base.OnPlayerTogglingRadio(ev);
    }

    public override void OnPlayerUsingRadio(PlayerUsingRadioEventArgs ev)
    {
        if (!RadioMenuManager.MenusBySerial.TryGetValue(ev.RadioItem.Serial, out var menu))
        {
            base.OnPlayerUsingRadio(ev);
            return;
        }

        if (menu.SuppressDefaultBehavior)
            ev.IsAllowed = false;
        base.OnPlayerUsingRadio(ev);
    }

    public override void OnPlayerDroppingItem(PlayerDroppingItemEventArgs ev)
    {
        if (ev.Item is not { Type: ItemType.Radio })
        {
            base.OnPlayerDroppingItem(ev);
            return;
        }

        var playerId = ev.Player.ReferenceHub.GetInstanceID();
        if (RadioMenuManager.PlayerActiveRadio.TryGetValue(playerId, out var serial) &&
            serial == ev.Item.Serial)
            RadioMenuManager.CloseRadioMenu(ev.Player);

        base.OnPlayerDroppingItem(ev);
    }

    public override void OnPlayerChangedRole(PlayerChangedRoleEventArgs ev)
    {
        RadioMenuManager.CleanupPlayer(ev.Player);
        base.OnPlayerChangedRole(ev);
    }

    public override void OnPlayerLeft(PlayerLeftEventArgs ev)
    {
        RadioMenuManager.CleanupPlayer(ev.Player);
        base.OnPlayerLeft(ev);
    }

    public override void OnServerRoundRestarted()
    {
        RadioMenuManager.ClearAll();
        base.OnServerRoundRestarted();
    }

    private static IEnumerator<float> HintRefreshCoroutine(Player player, int playerId, RadioMenu menu)
    {
        while (RadioMenuManager.PlayerActiveRadio.ContainsKey(playerId))
        {
            yield return Timing.WaitForSeconds(menu.HintDuration);
            if (!RadioMenuManager.PlayerActiveRadio.ContainsKey(playerId)) break;
            if (RadioMenuManager.PlayerSelections.TryGetValue(playerId, out var idx))
                ShowMenuHint(player, menu, idx);
        }

        RadioMenuManager.PlayerHintCoroutines.Remove(playerId);
    }

    internal static void ShowMenuHint(Player player, RadioMenu menu, int selectedIndex)
    {
        if (menu.Items.Count == 0) return;

        var playerId = player.ReferenceHub.GetInstanceID();
        RadioMenuManager.PlayerLockedSelections.TryGetValue(playerId, out var lockedIdx);
        var hasLock = menu.LockOnSelect && RadioMenuManager.PlayerLockedSelections.ContainsKey(playerId);

        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(menu.Title))
            sb.AppendLine($"<b><size=28>{menu.Title}</size></b>");

        sb.AppendLine();

        if (menu.DisplayMode == MenuDisplayMode.Pager)
            ShowPagerHint(sb, menu, selectedIndex, hasLock, lockedIdx);
        else
            ShowListHint(sb, menu, selectedIndex, hasLock, lockedIdx);

        player.SendHint(sb.ToString(), menu.HintDuration);
    }

    private static void ShowListHint(StringBuilder sb, RadioMenu menu, int selectedIndex, bool hasLock, int lockedIdx)
    {
        string? description = null;
        for (var i = 0; i < menu.Items.Count; i++)
        {
            var item = menu.Items[i];
            var isSelected = i == selectedIndex;
            var isLocked = hasLock && i == lockedIdx;

            string color, prefix;
            if (!item.Enabled)
            {
                color = "#666666";
                prefix = "   ";
            }
            else if (isLocked)
            {
                color = "#00FF88";
                prefix = "✓ ";
            }
            else if (isSelected)
            {
                color = "#FFFF00";
                prefix = "► ";
            }
            else
            {
                color = "#FFFFFF";
                prefix = "   ";
            }

            sb.Append($"<color={color}>{prefix}{item.Label}");
            if (!item.Enabled)
                sb.Append(" [disabled]");
            sb.AppendLine("</color>");

            if (isSelected && !string.IsNullOrEmpty(item.Description))
                description = item.Description;
        }

        sb.AppendLine();
        if (description != null)
            sb.AppendLine($"<color=#AAAAAA><size=20>{description}</size></color>");

        var toggleHint = hasLock ? "Toggle = Unlock" : "Toggle = Select";
        sb.AppendLine($"<color=#888888><size=18>Range = Next | {toggleHint}</size></color>");
    }

    private static void ShowPagerHint(StringBuilder sb, RadioMenu menu, int selectedIndex, bool hasLock, int lockedIdx)
    {
        var item = menu.Items[selectedIndex];
        var isLocked = hasLock && selectedIndex == lockedIdx;

        string color;
        if (!item.Enabled)
            color = "#666666";
        else if (isLocked)
            color = "#00FF88";
        else
            color = "#FFFF00";

        var lockIndicator = isLocked ? " ✓" : "";
        sb.AppendLine($"<color={color}><size=26>◄  {item.Label}{lockIndicator}  ►</size></color>");

        sb.AppendLine();
        if (!string.IsNullOrEmpty(item.Description))
            sb.AppendLine($"<color=#AAAAAA><size=20>{item.Description}</size></color>");

        if (!item.Enabled)
            sb.AppendLine("<color=#666666><size=18>[disabled]</size></color>");

        sb.AppendLine();
        sb.AppendLine($"<color=#888888><size=16>{selectedIndex + 1} / {menu.Items.Count}</size></color>");

        var toggleHint = hasLock ? "Toggle = Unlock" : "Toggle = Select";
        sb.AppendLine($"<color=#888888><size=18>Range = Next | {toggleHint}</size></color>");
    }
}