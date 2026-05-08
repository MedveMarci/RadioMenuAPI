namespace RadioMenuAPI;

public class Config
{
    public bool Debug { get; set; } = false;
    public string DisabledLabel { get; set; } = " [disabled]";
    public string FooterSelectHint { get; set; } = "Range: next  ·  Toggle: select";
    public string FooterUnlockHint { get; set; } = "Range: next  ·  Toggle: unlock";
}