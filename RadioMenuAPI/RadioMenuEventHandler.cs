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
        {
            // CloseRadioMenu handles OnClosed, MenuClosed event, state cleanup, and hint clear
            RadioMenuManager.CloseRadioMenu(ev.Player);
        }

        if (ev.NewItem is { Type: ItemType.Radio } &&
            RadioMenuManager.MenusBySerial.TryGetValue(ev.NewItem.Serial, out var newMenu))
        {
            RadioMenuManager.PlayerActiveRadio[playerId] = ev.NewItem.Serial;
            RadioMenuManager.PlayerSelections[playerId] = 0;

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
            RadioMenuManager.PlayerHintCoroutines[playerId] = Timing.RunCoroutine(HintRefreshCoroutine(ev.Player, playerId, newMenu));
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

        try
        {
            item.OnSelected?.Invoke(ev.Player, item);
        }
        catch (Exception ex)
        {
            Logger.Error($"[RadioMenuAPI] OnSelected error: {ex}");
        }

        RadioMenuEvents.InvokeItemSelected(new MenuItemSelectedEventArgs(ev.Player, menu, item, selectedIndex));
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

        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(menu.Title))
            sb.AppendLine($"<b><size=28>{menu.Title}</size></b>");

        sb.AppendLine();

        string? description = null;
        for (var i = 0; i < menu.Items.Count; i++)
        {
            var item = menu.Items[i];
            var isSelected = i == selectedIndex;
            var color = !item.Enabled ? "#666666" : isSelected ? "#FFFF00" : "#FFFFFF";
            var prefix = isSelected ? "► " : "   ";

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
        sb.AppendLine("<color=#888888><size=18>Range = Next | Toggle = Select</size></color>");

        player.SendHint(sb.ToString(), menu.HintDuration);
    }
}
