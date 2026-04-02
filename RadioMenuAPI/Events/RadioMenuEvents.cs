using System;
using LabApi.Features.Console;

namespace RadioMenuAPI.Events;

public static class RadioMenuEvents
{
    /// <summary>
    ///     Fired when a player equips a radio that has a menu assigned to it.
    /// </summary>
    public static event Action<MenuOpenedEventArgs>? MenuOpened;

    /// <summary>
    ///     Fired when a player unequips a radio that had a menu open.
    /// </summary>
    public static event Action<MenuClosedEventArgs>? MenuClosed;

    /// <summary>
    ///     Fired when a player presses Range to navigate to a different menu item.
    /// </summary>
    public static event Action<MenuItemChangedEventArgs>? ItemChanged;

    /// <summary>
    ///     Fired when a player presses Toggle to confirm/select the currently highlighted item.
    ///     Only fires if the item is enabled.
    /// </summary>
    public static event Action<MenuItemSelectedEventArgs>? ItemSelected;

    internal static void InvokeMenuOpened(MenuOpenedEventArgs ev)
    {
        if (MenuOpened != null)
            SafeInvoke(MenuOpened, ev, nameof(MenuOpened));
    }

    internal static void InvokeMenuClosed(MenuClosedEventArgs ev)
    {
        if (MenuClosed != null)
            SafeInvoke(MenuClosed, ev, nameof(MenuClosed));
    }

    internal static void InvokeItemChanged(MenuItemChangedEventArgs ev)
    {
        if (ItemChanged != null)
            SafeInvoke(ItemChanged, ev, nameof(ItemChanged));
    }

    internal static void InvokeItemSelected(MenuItemSelectedEventArgs ev)
    {
        if (ItemSelected != null)
            SafeInvoke(ItemSelected, ev, nameof(ItemSelected));
    }

    private static void SafeInvoke<T>(Action<T> action, T args, string eventName)
    {
        foreach (var handler in action.GetInvocationList())
            try
            {
                ((Action<T>)handler)(args);
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"[RadioMenuAPI] Exception in {eventName} handler '{handler.Method.DeclaringType?.Name}.{handler.Method.Name}': {ex}");
            }
    }
}