namespace RadioMenuAPI;

internal readonly struct RememberedSelection(int index, int lockedIndex)
{
    public int Index { get; } = index;

    public int LockedIndex { get; } = lockedIndex;
}
