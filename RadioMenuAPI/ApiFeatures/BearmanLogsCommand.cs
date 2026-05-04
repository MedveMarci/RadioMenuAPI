using System;
using CommandSystem;

namespace RadioMenuAPI.ApiFeatures;

[CommandHandler(typeof(GameConsoleCommandHandler))]
public class BearmanLogsEvent : ICommand
{
    public string Command => "bearmanlogsRadio";

    public string[] Aliases { get; } = ["bmlogsRadio"];

    public string Description => "Sends collected plugin logs to the log server and returns the log id.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        var getLogHistory = LogManager.GetLogHistory();
        response = getLogHistory.logResult;
        return getLogHistory.success;
    }
}