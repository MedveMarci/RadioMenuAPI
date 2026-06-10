using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LabApi.Features;
using NorthwoodLib.Pools;

namespace RadioMenuAPI.ApiFeatures;

internal static class ApiManager
{
    private const string ApiBase = "https://bearmanapi.hu";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);

    private static readonly Dictionary<string, DateTime> AutoErrorLastSent = new();
    private static readonly TimeSpan DedupWindow = TimeSpan.FromSeconds(5);
    
    internal static void CheckForUpdates()
    {
        Task.Run(async () =>
        {
            var name = RadioMenuAPI.Singleton.Name;
            var current = RadioMenuAPI.Singleton.Version;

            try
            {
                var resp = await WithTimeout(
                    HttpQuery.GetAsync($"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(name)}/latest"));

                var (code, _) = ParseResponse(resp);
                if (code != HttpStatusCode.OK)
                {
                    LogManager.Error($"Version check failed: {code}");
                    return;
                }

                var root = JsonDocument.Parse(resp).RootElement;
                if (!root.TryGetProperty("version", out var vProp) || vProp.ValueKind != JsonValueKind.String ||
                    !Version.TryParse(vProp.GetString() ?? "", out var latest))
                {
                    LogManager.Error("Version check: invalid response format.");
                    return;
                }

                var verResp = await WithTimeout(
                    HttpQuery.GetAsync(
                        $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(name)}/version/{Uri.EscapeDataString(current.ToString())}"));

                var recallDoc = JsonDocument.Parse(verResp).RootElement;
                if (recallDoc.TryGetProperty("is_recalled", out var recalled) &&
                    recalled.ValueKind == JsonValueKind.True)
                {
                    var reason = recallDoc.TryGetProperty("recall_reason", out var r) &&
                                 r.ValueKind == JsonValueKind.String
                        ? r.GetString()
                        : "No reason provided.";
                    LogManager.Error(
                        $"This version of {name} has been recalled! Update to {latest} ASAP.\nReason: {reason}",
                        ConsoleColor.DarkRed);
                    return;
                }

                if (latest > current)
                    LogManager.Info(
                        $"New version of {name} available: {latest} (you have {current}). {GetDownloadUrl(root)}",
                        ConsoleColor.DarkRed);
                else
                    LogManager.Info($"Thank you for using {name} v{current}. Support: https://discord.gg/KmpA8cfaSA",
                        ConsoleColor.Blue);

                if (current > latest)
                    LogManager.Info(
                        $"You are running a newer version of {RadioMenuAPI.Singleton.Name} ({RadioMenuAPI.Singleton.Version}) than {latest}. This is a development/pre-release build and it can contain errors or bugs.",
                        ConsoleColor.DarkMagenta);
            }
            catch (TimeoutException)
            {
                LogManager.Error("Version check timed out.");
            }
            catch (Exception ex)
            {
                LogManager.Error("Version check failed.");
                LogManager.Debug($"Version check exception:\n{ex}");
            }
        });
    }

    internal static string? SendLogsAsync(string content)
    {
        try
        {
            return Task.Run(async () =>
            {
                var url = $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(RadioMenuAPI.Singleton.Name)}/log";
                var payload = JsonSerializer.Serialize(new
                {
                    content,
                    plugin_version = RadioMenuAPI.Singleton.Version.ToString(),
                    labapi_version = LabApiProperties.CurrentVersion
                });

                var resp = await WithTimeout(HttpQuery.PostAsync(url, payload, "application/json"));

                var (code, _) = ParseResponse(resp);
                if (code != HttpStatusCode.Created)
                {
                    LogManager.Error($"Failed to send logs: {code}");
                    return null;
                }

                var doc = JsonDocument.Parse(resp).RootElement;
                return doc.TryGetProperty("log_id", out var id) && id.ValueKind == JsonValueKind.String
                    ? id.GetString()
                    : null;
            }).GetAwaiter().GetResult();
        }
        catch (TimeoutException)
        {
            LogManager.Error("Log upload timed out.");
            return null;
        }
        catch (AggregateException ae) when (ae.InnerException != null)
        {
            LogManager.Error("Log upload failed.");
            LogManager.Debug($"Log upload exception:\n{ae.InnerException}");
            return null;
        }
        catch (Exception ex)
        {
            LogManager.Error("Log upload failed.");
            LogManager.Debug($"Log upload exception:\n{ex}");
            return null;
        }
    }
    
    internal static void SendAutoError(string errorMessage)
    {
        Task.Run(() =>
        {
            try
            {
                if (RadioMenuAPI.Singleton?.Config == null) return;

                var hash = ComputeShortHash(errorMessage);

                lock (AutoErrorLastSent)
                {
                    if (AutoErrorLastSent.TryGetValue(hash, out var lastSent) &&
                        DateTime.UtcNow - lastSent < DedupWindow)
                        return;

                    AutoErrorLastSent[hash] = DateTime.UtcNow;

                    var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(5);
                    var toRemove = new List<string>();
                    foreach (var kv in AutoErrorLastSent)
                        if (kv.Value < cutoff)
                            toRemove.Add(kv.Key);
                    foreach (var k in toRemove)
                        AutoErrorLastSent.Remove(k);
                }

                var content = LogManager.BuildLogContent(errorMessage);
                var url = $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(RadioMenuAPI.Singleton.Name)}/log";
                var payload = new
                {
                    content,
                    plugin_version = RadioMenuAPI.Singleton.Version.ToString(),
                    labapi_version = LabApiProperties.CurrentVersion,
                    trigger = "auto_error"
                };
                var json = JsonSerializer.Serialize(payload);
                HttpQuery.Post(url, json, "application/json");
            }
            catch (Exception e)
            {
                LogManager.Debug($"SendAutoError failed: {e.Message}");
            }
        });
    }
    
    private static string ComputeShortHash(string input)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));

        var sb = StringBuilderPool.Shared.Rent(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("X2"));

        return StringBuilderPool.Shared.ToStringReturn(sb).Substring(0, 8);
    }

    private static async Task<string> WithTimeout(Task<string> task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(RequestTimeout));
        if (completed != task)
            throw new TimeoutException();
        return await task;
    }

    private static (HttpStatusCode code, string message) ParseResponse(string json)
    {
        try
        {
            var root = JsonDocument.Parse(json).RootElement;
            var code = root.TryGetProperty("status", out var s) && s.ValueKind == JsonValueKind.Number
                ? (HttpStatusCode)s.GetInt32()
                : HttpStatusCode.InternalServerError;
            var msg = root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String
                ? m.GetString()
                : null;
            return (code, msg);
        }
        catch
        {
            return (HttpStatusCode.InternalServerError, null);
        }
    }

    private static string GetDownloadUrl(JsonElement root)
    {
        return root.TryGetProperty("download_url", out var d) && d.ValueKind == JsonValueKind.String &&
               !string.IsNullOrEmpty(d.GetString())
            ? $"Download: {d.GetString()}"
            : string.Empty;
    }
}