namespace RadioMenuAPI;

public class Config
{
    public bool Debug { get; set; } = false;
    public string DisabledLabel { get; set; } = " [disabled]";
    public string FooterSelectHint { get; set; } = "Range = Next | Toggle = Select";
    public string FooterUnlockHint { get; set; } = "Range = Next | Toggle = Unlock";
}