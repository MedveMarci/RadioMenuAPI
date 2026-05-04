using System;
using System.Net;
using System.Text.Json;
using LabApi.Features;

namespace RadioMenuAPI.ApiFeatures;

public static class ApiManager
{
    private const string ApiBase = "https://bearmanapi.hu";

    internal static void CheckForUpdates()
    {
        try
        {
            var name = RadioMenuAPI.Singleton.Name;
            var currentVersion = RadioMenuAPI.Singleton.Version;

            string resp;
            try
            {
                resp = HttpQuery.Get($"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(name)}/latest");
            }
            catch (Exception)
            {
                LogManager.Warn("Could not reach BearmanAPI. Skipping update check.");
                return;
            }

            var (statusCode, message) = ParseApiResponse(resp);

            if (statusCode != HttpStatusCode.OK)
            {
                LogManager.Debug($"Version check failed: {statusCode} - {message ?? "(no message)"}");
                return;
            }

            var root = JsonDocument.Parse(resp).RootElement;

            if (!root.TryGetProperty("version", out var versionProp) || versionProp.ValueKind != JsonValueKind.String)
            {
                LogManager.Debug("Version check failed: 'version' field missing or invalid.");
                return;
            }

            var version = versionProp.GetString();

            if (version == null || !Version.TryParse(version, out var latestRemoteVersion))
            {
                LogManager.Debug("Version check failed: Invalid version format.");
                return;
            }

            var outdated = latestRemoteVersion > currentVersion;
            var currentIsNewerThanRemote = currentVersion > latestRemoteVersion;

            string currentVersionResp;
            try
            {
                currentVersionResp = HttpQuery.Get(
                    $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(name)}/version/{Uri.EscapeDataString(currentVersion.ToString())}");
            }
            catch (Exception)
            {
                LogManager.Debug("Could not reach BearmanAPI for recall check. Skipping.");
                currentVersionResp = null;
            }

            if (currentVersionResp != null)
            {
                var (currentStatusCode, currentMessage) = ParseApiResponse(currentVersionResp);
                if (currentStatusCode != HttpStatusCode.OK)
                {
                    LogManager.Debug($"Recall check failed: {currentStatusCode} - {currentMessage}");
                }
                else
                {
                    var recallRoot = JsonDocument.Parse(currentVersionResp).RootElement;
                    if (recallRoot.TryGetProperty("is_recalled", out var isRecalledProp) &&
                        isRecalledProp.ValueKind == JsonValueKind.True)
                    {
                        var recallReason = recallRoot.TryGetProperty("recall_reason", out var reasonProp) &&
                                           reasonProp.ValueKind == JsonValueKind.String
                            ? reasonProp.GetString()
                            : "No reason provided.";
                        LogManager.Error(
                            $"This version of {name} has been recalled.\nPlease update to {latestRemoteVersion} version as soon as possible.\nReason: {recallReason}",
                            ConsoleColor.DarkRed);
                        return;
                    }
                }
            }

            if (outdated)
            {
                var updateMsg =
                    $"A new version of {name} is available: {version} (current {currentVersion}). {GetDownloadUrl(root)}";
                LogManager.Info(updateMsg, ConsoleColor.DarkRed);
            }
            else
            {
                LogManager.Info(
                    $"Thanks for using {name} v{currentVersion}. To get support and latest news, join to my Discord Server: https://discord.gg/KmpA8cfaSA",
                    ConsoleColor.Blue);
            }

            if (!currentIsNewerThanRemote) return;
            LogManager.Info(
                $"You are running a newer version of {name} ({currentVersion}) than {latestRemoteVersion}. This is a development/pre-release build and it can contain errors or bugs.",
                ConsoleColor.DarkMagenta);
        }
        catch (Exception e)
        {
            LogManager.Error("An error occurred while checking for updates. Turn on debug for details.");
            LogManager.Debug($"CheckForUpdates failed: {e.Message}");
        }
    }

    private static string GetDownloadUrl(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) return "";
        if (root.TryGetProperty("download_url", out var d) && d.ValueKind == JsonValueKind.String)
            return string.IsNullOrEmpty(d.GetString()) ? "" : $"Download: {d.GetString()}";

        return "";
    }

    internal static string SendLogsAsync(string content)
    {
        try
        {
            var url = $"{ApiBase}/api/v1/plugin/{Uri.EscapeDataString(RadioMenuAPI.Singleton.Name)}/log";

            LogManager.Info("Sending logs to BearmanAPI...", ConsoleColor.Green);

            var payload = new
            {
                content,
                plugin_version = RadioMenuAPI.Singleton.Version.ToString(),
                labapi_version = LabApiProperties.CurrentVersion
            };
            var json = JsonSerializer.Serialize(payload);
            var resp = HttpQuery.Post(url, json, "application/json");
            var data = ParseApiResponse(resp);
            if (data.StatusCode != HttpStatusCode.Created)
            {
                LogManager.Error($"Failed to send logs: {data.StatusCode} - {data.Message ?? "(no message)"}");
                return null;
            }

            if (JsonDocument.Parse(resp).RootElement.TryGetProperty("log_id", out var logIdProp) &&
                logIdProp.ValueKind == JsonValueKind.String)
                return logIdProp.GetString();

            LogManager.Warn("Logs sent but no log_id returned.");
            return null;
        }
        catch (Exception e)
        {
            LogManager.Error($"Sending logs failed.\n{e}");
            return null;
        }
    }

    private static (HttpStatusCode StatusCode, string Message) ParseApiResponse(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var statusCode = HttpStatusCode.InternalServerError;
            string message = null;

            if (root.TryGetProperty("status", out var statusProp) && statusProp.ValueKind == JsonValueKind.Number)
                statusCode = (HttpStatusCode)statusProp.GetInt32();

            if (root.TryGetProperty("message", out var messageProp) && messageProp.ValueKind == JsonValueKind.String)
                message = messageProp.GetString();

            return (statusCode, message);
        }
        catch (Exception e)
        {
            LogManager.Error("Failed to parse API response.");
            LogManager.Debug($"ParseApiResponse failed.\n{e}");
            return (HttpStatusCode.InternalServerError, null);
        }
    }
}