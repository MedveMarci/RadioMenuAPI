using System;
using System.Collections.Generic;
using LabApi.Features.Console;
using LabApi.Loader.Features.Yaml;
using NorthwoodLib.Pools;

namespace RadioMenuAPI.ApiFeatures;

internal static class LogManager
{
    private static readonly List<LogEntry> History = [];
    private static bool DebugEnabled => RadioMenuAPI.Singleton.Config.Debug;

    public static void Debug(string message)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Debug", message));
        if (!DebugEnabled) return;
        Logger.Raw($"[DEBUG] [{RadioMenuAPI.Singleton.Name}] {message}", ConsoleColor.Green);
    }

    public static void Info(string message, ConsoleColor color = ConsoleColor.Cyan)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Info", message));
        Logger.Raw($"[INFO] [{RadioMenuAPI.Singleton.Name}] {message}", color);
    }

    public static void Warn(string message)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Warn", message));
        Logger.Warn(message);
    }

    public static void Error(string message, ConsoleColor color = ConsoleColor.Red)
    {
        History.Add(new LogEntry(DateTimeOffset.Now.ToUnixTimeMilliseconds(), "Error", message));
        Logger.Raw($"[ERROR] [{RadioMenuAPI.Singleton.Name}] {message}", color);
        ApiManager.SendAutoError(message);
    }

    public static (string logResult, bool success) GetLogHistory()
    {
        var sb = StringBuilderPool.Shared.Rent();
        foreach (var log in History)
            sb.AppendLine(
                $"[{DateTimeOffset.FromUnixTimeMilliseconds(log.Timestamp):yyyy-MM-dd HH:mm:ss}] [{log.Level}] {log.Message}");

        if (RadioMenuAPI.Singleton?.Config != null)
        {
            sb.AppendLine("\n--- RadioMenuAPI Config ---\n");
            sb.Append(YamlConfigParser.Serializer.Serialize(RadioMenuAPI.Singleton.Config));
        }

        var logId = ApiManager.SendLogsAsync(StringBuilderPool.Shared.ToStringReturn(sb));
        return logId == null
            ? ("Failed to send LogHistory.", false)
            : ($"Log history sent. ID: {logId}", true);
    }
    
    internal static string BuildLogContent(string triggerError = null)
    {
        var sb = StringBuilderPool.Shared.Rent();

        if (!string.IsNullOrEmpty(triggerError))
        {
            sb.AppendLine("--- Auto Error ---");
            sb.AppendLine(triggerError);
            sb.AppendLine();
        }

        foreach (var log in History)
            sb.AppendLine(
                $"[{DateTimeOffset.FromUnixTimeMilliseconds(log.Timestamp):yyyy-MM-dd HH:mm:ss}] [{log.Level}] {log.Message}");

        return StringBuilderPool.Shared.ToStringReturn(sb);
    }

    private sealed class LogEntry(long timestamp, string level, string message)
    {
        public long Timestamp { get; } = timestamp;
        public string Level { get; } = level;
        public string Message { get; } = message;
    }
}