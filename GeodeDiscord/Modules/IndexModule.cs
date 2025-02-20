using System.Text;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using JetBrains.Annotations;

using GeodeDiscord.Database;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;

using Serilog;

namespace GeodeDiscord.Modules;

[Group("index", "Interact with the Geode mod index."), UsedImplicitly]
public class IndexModule(ApplicationDbContext db) : InteractionModuleBase<SocketInteractionContext> {
    public static string GetAPIEndpoint(string path) => $"https://api.geode-sdk.org{path}";

    public static async Task<string> GetError(HttpResponseMessage response, string message) {
        if (response.IsSuccessStatusCode) return "";

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonConvert.DeserializeObject<JObject>(json);
        if (data is null || data["error"] is null) {
            return $"❌ {message}.";
        }

        return $"❌ {message}: `{data["error"]?.ToString()}`.";
    }

    [ComponentInteraction("accept"), UsedImplicitly]
    public async Task Accept() {
        var modal = new ModalBuilder()
            .WithTitle("Accept Mod")
            .WithCustomId($"index/confirm:accepted")
            .AddTextInput("Reason", "reason", TextInputStyle.Paragraph, "Leave blank for no reason", required: false)
            .Build();

        await RespondWithModalAsync(modal);
    }

    [ComponentInteraction("reject"), UsedImplicitly]
    public async Task Reject() {
        var modal = new ModalBuilder()
            .WithTitle("Reject Mod")
            .WithCustomId($"index/confirm:rejected")
            .AddTextInput("Reason", "reason", TextInputStyle.Paragraph, "Leave blank for no reason", required: false)
            .Build();

        await RespondWithModalAsync(modal);
    }

    public class ModStatusModal : IModal {
        public string Title => string.Empty;

        [ModalTextInput("reason")]
        public string? Reason { get; set; }
    }

    [ModalInteraction("confirm:*"), UsedImplicitly]
    public async Task UpdateModStatus(string status, ModStatusModal modal) {
        if (Context.Channel is not SocketThreadChannel threadChannel) return;
        if (threadChannel.IsLocked) return;

        var splitName = threadChannel.Name.Split(" ");
        var id = splitName[^2].Trim('(', ')');
        var version = splitName[^1];

        var indexToken = await db.indexTokens.FindAsync(Context.User.Id);
        if (indexToken is null) {
            await RespondAsync("❌ You must log in to your Geode account first.", ephemeral: true);
            return;
        }

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "GeodeDiscord");
        using var statusResponse = await httpClient.GetAsync(GetAPIEndpoint($"/v1/mods/{id}/versions/{version}"));
        if (!statusResponse.IsSuccessStatusCode) {
            await RespondAsync(await GetError(statusResponse,
                "An error occurred while getting the mod status"), ephemeral: true);
            return;
        }

        var statusJson = await statusResponse.Content.ReadAsStringAsync();
        var statusData = JsonConvert.DeserializeObject<JObject>(statusJson);
        if (statusData is null || statusData["payload"] is null) {
            await RespondAsync("❌ An error occurred while parsing the mod status response.", ephemeral: true);
            return;
        }

        var pendingStatus = statusData["payload"]?["status"]?.ToString();
        if (pendingStatus != "pending") {
            await RespondAsync("❌ Mod is no longer pending.", ephemeral: true);
            return;
        }

        httpClient.DefaultRequestHeaders.Authorization = new("Bearer", indexToken.accessToken);
        using var response = await httpClient.PutAsync(GetAPIEndpoint($"/v1/mods/{id}/versions/{version}"),
            new StringContent(JsonConvert.SerializeObject(new { status, info = modal.Reason }),
            Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode) {
            if (response.StatusCode == HttpStatusCode.Unauthorized) {
                using var authResponse = await httpClient.PostAsync(GetAPIEndpoint("/v1/login/refresh"),
                    new StringContent(JsonConvert.SerializeObject(new { refresh_token = indexToken.refreshToken }),
                    Encoding.UTF8, "application/json"));
                if (!authResponse.IsSuccessStatusCode) {
                    await RespondAsync(await GetError(authResponse,
                        "An error occurred while refreshing your access token"), ephemeral: true);
                    return;
                }

                var authJson = await authResponse.Content.ReadAsStringAsync();
                var authData = JsonConvert.DeserializeObject<JObject>(authJson);
                if (authData is null || authData["payload"] is null) {
                    await RespondAsync("❌ An error occurred while parsing the token refresh response.", ephemeral: true);
                    return;
                }

                indexToken.accessToken = authData["payload"]?["access_token"]?.ToString() ?? "";
                indexToken.refreshToken = authData["payload"]?["refresh_token"]?.ToString() ?? "";

                try { await db.SaveChangesAsync(); }
                catch (Exception ex) {
                    Log.Error(ex, "Failed to save index token");
                    await RespondAsync("❌ Failed to save index token.", ephemeral: true);
                    return;
                }

                httpClient.DefaultRequestHeaders.Authorization = new("Bearer", indexToken.accessToken);
                using var retryResponse = await httpClient.PutAsync(GetAPIEndpoint($"/v1/mods/{id}/versions/{version}"),
                    new StringContent(JsonConvert.SerializeObject(new { status, info = modal.Reason }),
                    Encoding.UTF8, "application/json"));
                if (!retryResponse.IsSuccessStatusCode) {
                    await RespondAsync(await GetError(retryResponse,
                        $"An error occurred while {status.Replace("ed", "ing")} the mod"), ephemeral: true);
                    return;
                }

                await RespondAsync($"✅ Successfully {status} the mod!", ephemeral: true);
            }
            else await RespondAsync(await GetError(response,
                $"An error occurred while {status.Replace("ed", "ing")} the mod"), ephemeral: true);
            return;
        }

        await RespondAsync($"✅ Successfully {status} the mod!", ephemeral: true);
    }
}
