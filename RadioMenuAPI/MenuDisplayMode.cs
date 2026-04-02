namespace RadioMenuAPI;

public enum MenuDisplayMode
{
    /// <summary>All items are shown in a vertical list. The currently highlighted item is marked with an arrow.</summary>
    List,

    /// <summary>
    ///     Only one item is visible at a time. Use the Range button to cycle through items.
    ///     Wraps back to the first item after the last one.
    /// </summary>
    Pager
}