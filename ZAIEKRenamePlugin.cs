using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using System.Text.Json;

namespace ZAIEKRenamePlugin;

public class PluginConfig
{
    public string RequiredPermission { get; set; } = "@css/generic";
    public string Language { get; set; } = "tr";
}

public class ZAIEKRenamePlugin : BasePlugin
{
    public override string ModuleName => "ZAIEKRenamePlugin";
    public override string ModuleVersion => "1.1.0";
    public override string ModuleAuthor => "ChatGPT";

    private readonly Dictionary<ulong, string> originalNames = new();
    private PluginConfig Config = new();

    public override void Load(bool hotReload)
    {
        LoadPluginConfig();

        AddCommand("rename", "Bir oyuncunun ismini değiştirir", RenameCommand);
        AddCommand("rename0", "Eski ismine döner", ResetNameCommand);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    }

    private void LoadPluginConfig()
    {
        string configPath = Path.Combine(ModuleDirectory, "config.json");
        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            Config = JsonSerializer.Deserialize<PluginConfig>(json) ?? new PluginConfig();
        }
    }

    private void RenameCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        if (!AdminManager.PlayerHasPermissions(player, Config.RequiredPermission))
        {
            command.ReplyToCommand(GetMessage("NoPermission"));
            return;
        }

        if (command.ArgCount < 2)
        {
            command.ReplyToCommand(GetMessage("UsageRename"));
            return;
        }

        string newName = string.Join(" ", Enumerable.Range(1, command.ArgCount - 1).Select(i => command.GetArg(i)));

        if (newName.Length < 2 || newName.Length > 32)
        {
            command.ReplyToCommand(GetMessage("NameLengthInvalid"));
            return;
        }

        ulong steamId = player.SteamID;
        if (!originalNames.ContainsKey(steamId))
        {
            originalNames[steamId] = player.PlayerName;
        }

        player.PlayerName = newName;
        command.ReplyToCommand(string.Format(GetMessage("NameChanged"), newName));
    }

    private void ResetNameCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid)
            return;

        if (!AdminManager.PlayerHasPermissions(player, Config.RequiredPermission))
        {
            command.ReplyToCommand(GetMessage("NoPermission"));
            return;
        }

        ulong steamId = player.SteamID;
        if (originalNames.TryGetValue(steamId, out var oldName))
        {
            player.PlayerName = oldName;
            command.ReplyToCommand(string.Format(GetMessage("NameReset"), oldName));
        }
        else
        {
            command.ReplyToCommand(GetMessage("NoOldName"));
        }
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect ev, GameEventInfo info)
    {
        var player = ev.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        ulong steamId = player.SteamID;
        originalNames.Remove(steamId);
        return HookResult.Continue;
    }

    private string GetMessage(string key)
    {
        return Config.Language switch
        {
            "tr" => key switch
            {
                "NoPermission" => "⛔ Bu komutu kullanmak için yetkin yok.",
                "UsageRename" => "❗ Kullanım: !rename <yeni_isim>",
                "NameChanged" => "✅ İsim başarıyla '{0}' olarak değiştirildi.",
                "NameReset" => "🔁 İsim eski haline döndürüldü: {0}",
                "NoOldName" => "⚠️ Önce isim değiştirmelisin.",
                "NameLengthInvalid" => "❗ İsim 2-32 karakter arasında olmalıdır.",
                _ => key
            },
            "en" => key switch
            {
                "NoPermission" => "⛔ You don't have permission to use this command.",
                "UsageRename" => "❗ Usage: !rename <new_name>",
                "NameChanged" => "✅ Name successfully changed to '{0}'.",
                "NameReset" => "🔁 Name has been reset to: {0}",
                "NoOldName" => "⚠️ You need to change your name first.",
                "NameLengthInvalid" => "❗ Name must be between 2-32 characters.",
                _ => key
            },
            _ => key
        };
    }
}