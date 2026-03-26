using System;
using System.Text;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
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

        if (menu.Items.Count == 0) return;

        var playerId = ev.Player.ReferenceHub.GetInstanceID();
        RadioMenuManager.PlayerSelections.TryGetValue(playerId, out var previousIndex);

        var newIndex = (previousIndex + 1) % menu.Items.Count;
        RadioMenuManager.PlayerSelections[playerId] = newIndex;

        RadioMenuEvents.InvokeItemChanged(new MenuItemChangedEventArgs(
            ev.Player, menu,
            menu.Items[previousIndex], previousIndex,
            menu.Items[newIndex], newIndex));

        ShowMenuHint(ev.Player, menu, newIndex);
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

        if (menu.Items.Count == 0) return;

        var playerId = ev.Player.ReferenceHub.GetInstanceID();
        if (!RadioMenuManager.PlayerSelections.TryGetValue(playerId, out var idx))
            return;

        var selectedIndex = idx % menu.Items.Count;
        var item = menu.Items[selectedIndex];

        if (!item.Enabled) return;

        try
        {
            item.OnSelected?.Invoke(ev.Player, item);
        }
        catch (Exception ex)
        {
            Logger.Error($"[RadioMenuAPI] OnSelected error: {ex}");
        }

        RadioMenuEvents.InvokeItemSelected(new MenuItemSelectedEventArgs(ev.Player, menu, item, selectedIndex));
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

    internal static void ShowMenuHint(Player player, RadioMenu menu, int selectedIndex)
    {
        if (menu.Items.Count == 0) return;

        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(menu.Title))
            sb.AppendLine($"<b><size=28>{menu.Title}</size></b>");

        sb.AppendLine();

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
                sb.AppendLine($"<color=#AAAAAA><size=20>   {item.Description}</size></color>");
        }

        sb.AppendLine();
        sb.AppendLine("<color=#888888><size=18>Range = Next | Toggle = Select</size></color>");

        player.SendHint(sb.ToString(), menu.HintDuration);
    }
}
