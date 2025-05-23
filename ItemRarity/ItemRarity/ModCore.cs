using System;
using System.Linq;
using HarmonyLib;
using ItemRarity.Config;
using ItemRarity.Packets;
using ItemRarity.Server;
using ItemRarity.Server.Commands;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ItemRarity;

/// <summary>
/// Mod Entry point.
/// </summary>
[HarmonyPatch]
public sealed class ModCore : ModSystem
{
    public const string HarmonyId = "itemrarity.patches";
    public const string ConfigFileName = "itemrarity.json";
    public const string ConfigSyncNetChannel = "itemrarity.configsync";

    public static ModConfig Config = ModConfig.GetDefaultConfig();
    public static Harmony HarmonyInstance = null!;
    public static ICoreClientAPI? ClientApi;
    public static ICoreServerAPI? ServerApi;
    public static WeatherSystemServer? WeatherSystemServer;

    public override void Start(ICoreAPI api)
    {
        if (!Harmony.HasAnyPatches(HarmonyId))
        {
            HarmonyInstance = new Harmony(HarmonyId);
            HarmonyInstance.PatchAll();
        }

        LoadConfig(api);

        GlobalConstants.IgnoredStackAttributes = GlobalConstants.IgnoredStackAttributes.Append(ModAttributes.Guid); // Importang for TreasureTrader

        api.Network.RegisterChannel(ConfigSyncNetChannel).RegisterMessageType<ServerConfigMessage>();

        RegisterToolkitItemsAndRecipes(api); // Call the new method

        api.Logger.Notification("[ItemRarity] Mod loaded.");
    }

    private EnumItemClass GetEnumItemClassFromString(string type)
    {
        if (string.IsNullOrEmpty(type)) return EnumItemClass.Item; // Default to item if null or empty
        return type.ToLowerInvariant() == "block" ? EnumItemClass.Block : EnumItemClass.Item;
    }

    private void RegisterToolkitItemsAndRecipes(ICoreAPI api)
    {
        if (Config.Toolkits == null || !Config.Toolkits.Any())
        {
            api.Logger.Notification($"[{Mod.Info.ModID}] No toolkits defined in config. Skipping item and recipe registration for toolkits.");
            return;
        }

        api.Logger.Notification($"[{Mod.Info.ModID}] Starting toolkit item and recipe registration...");

        // Register ItemToolkits
        foreach (var toolkitConfigPair in Config.Toolkits) // Iterate over KeyValuePair to access the key easily for logging
        {
            var toolkitKey = toolkitConfigPair.Key; // e.g. "stone"
            var toolkitConfig = toolkitConfigPair.Value;

            if (string.IsNullOrEmpty(toolkitConfig.Code))
            {
                api.Logger.Warning($"[{Mod.Info.ModID}] ToolkitConfig for key '{toolkitKey}' found with empty code. Skipping item registration.");
                continue;
            }

            // Ensure toolkitConfig.Code matches the key for consistency, or use toolkitConfig.Code directly if it's the source of truth.
            // Assuming toolkitConfig.Code is the defining part like "toolkit-stone"
            string fullItemCode = $"{Mod.Info.ModID}:{toolkitConfig.Code}"; 
            AssetLocation itemAssetLocation = new AssetLocation(fullItemCode);

            if (api.World.GetItemType(itemAssetLocation) == null)
            {
                api.RegisterItemClass(fullItemCode, typeof(ItemToolkit)); // Register with the fully qualified code
                api.Logger.Notification($"ItemToolkit: Registered item class '{fullItemCode}'. (Toolkit Key: '{toolkitKey}')");
            }
            else
            {
                api.Logger.Debug($"ItemToolkit: Item class '{fullItemCode}' already registered. Skipping. (Toolkit Key: '{toolkitKey}')");
            }
        }

        // Register Crafting Recipes for Toolkits
        // Check if recipe already exists - Vintagestory's API handles this by logging a warning if a recipe with the same name is re-registered.
        // We can add an explicit check if we maintain a list of registered recipe names, but for now, rely on VS behavior.
        foreach (var toolkitConfigPair in Config.Toolkits)
        {
            var toolkitKey = toolkitConfigPair.Key;
            var toolkitConfig = toolkitConfigPair.Value;

            if (string.IsNullOrEmpty(toolkitConfig.Code) || toolkitConfig.Recipe == null)
            {
                api.Logger.Warning($"ItemToolkit: [{Mod.Info.ModID}] ToolkitConfig for key '{toolkitKey}' (Code: {toolkitConfig.Code}) has no recipe defined or code is empty. Skipping recipe registration.");
                continue;
            }
            
            GridRecipe recipe = new GridRecipe();
            recipe.Name = new AssetLocation(Mod.Info.ModID, $"recipe-{toolkitConfig.Code}"); 

            if (toolkitConfig.Recipe.Pattern == null || !toolkitConfig.Recipe.Pattern.Any())
            {
                api.Logger.Warning($"ItemToolkit: [{Mod.Info.ModID}] Recipe for {toolkitConfig.Code} has null or empty pattern. Skipping recipe.");
                continue;
            }
            recipe.Pattern = toolkitConfig.Recipe.Pattern.ToArray();
            recipe.Ingredients = new Dictionary<string, CraftingIngredient>();

            bool skipRecipe = false;
            foreach (var entry in toolkitConfig.Recipe.Ingredients)
            {
                var igConf = entry.Value;
                if (string.IsNullOrEmpty(igConf.Code) || string.IsNullOrEmpty(igConf.Type))
                {
                    api.Logger.Error($"ItemToolkit: [{Mod.Info.ModID}] Invalid ingredient in recipe for {toolkitConfig.Code}: Code or Type is null/empty for key '{entry.Key}'. Skipping this recipe.");
                    skipRecipe = true;
                    break; 
                }
                recipe.Ingredients.Add(entry.Key, new CraftingIngredient
                {
                    Type = GetEnumItemClassFromString(igConf.Type),
                    Code = new AssetLocation(igConf.Code), 
                    Quantity = igConf.Quantity
                });
            }

            if (skipRecipe) continue;

            if (toolkitConfig.Recipe.Output == null || string.IsNullOrEmpty(toolkitConfig.Recipe.Output.Code) || string.IsNullOrEmpty(toolkitConfig.Recipe.Output.Type))
            {
                 api.Logger.Error($"ItemToolkit: [{Mod.Info.ModID}] Invalid output in recipe for {toolkitConfig.Code}: Output or its Code/Type is null/empty. Skipping this recipe.");
                 continue;
            }
            
            recipe.Output = new CraftingRecipeIngredient
            {
                Type = GetEnumItemClassFromString(toolkitConfig.Recipe.Output.Type),
                Code = new AssetLocation(Mod.Info.ModID, toolkitConfig.Code), 
                Quantity = toolkitConfig.Recipe.Output.Quantity
            };
            
            recipe.RecipeGroup = 0;

            // It's good practice to check if a recipe with the same name is already registered,
            // though VS API might just log a warning and overwrite or ignore.
            // For simplicity here, we'll proceed with registration.
            // If api.World.GridRecipes contains the recipe.Name, then it's a duplicate.
            // However, direct access to check existence before registration isn't straightforward without iterating all existing recipes.
            api.RegisterCraftingRecipe(recipe);
            api.Logger.Notification($"ItemToolkit: Registered recipe '{recipe.Name}' outputting '{recipe.Output.Code}'. (Toolkit Key: '{toolkitKey}')");
        }
        api.Logger.Notification($"[{Mod.Info.ModID}] Toolkit item and recipe registration process complete.");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        ClientApi = api;

        api.Network.GetChannel(ConfigSyncNetChannel).SetMessageHandler<ServerConfigMessage>(packet =>
        {
            try
            {
                var config = JsonSerializer.Deserialize<ModConfig>(packet.SerializedConfig);
                if (config != null)
                {
                    Config = config;
                    api.Logger.Notification("[ItemRarity] Received config from server.");
                }
            }
            catch (Exception e)
            {
                api.Logger.Error(e);
            }
        });
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        ServerApi = api;

        api.Event.OnEntitySpawn += ServerEventsHandlers.OnEntitySpawn;
        api.Event.PlayerJoin += ServerEventsHandlers.OnPlayerJoin;

        WeatherSystemServer = api.ModLoader.GetModSystem<WeatherSystemServer>();

        var parsers = api.ChatCommands.Parsers;

        var mainCommand = api.ChatCommands.Create("rarity")
            .WithDescription("Commands related to ItemRarity mod")
            .RequiresPrivilege(Privilege.root)
            .RequiresPlayer();

        mainCommand.BeginSubCommand("set")
            .WithDescription("Change the currently held item rarity.")
            .WithArgs(parsers.Word("rarity", Config.Rarities.Keys.ToArray()))
            .HandleWith(CommandsHandlers.HandleSetRarityCommand)
            .EndSubCommand();

        mainCommand.BeginSubCommand("reload")
            .WithDescription("Reload the configuration")
            .HandleWith(del => CommandsHandlers.HandleReloadConfigCommand(api, del))
            .EndSubCommand();

        mainCommand.BeginSubCommand("test")
            .WithDescription("Run the random rarity generator.")
            .WithArgs(parsers.Int("times"))
            .HandleWith(del => CommandsHandlers.HandleTestRarityCommand(api, del))
            .EndSubCommand();

        mainCommand.BeginSubCommand("itemdebug")
            .WithDescription("Dev debug command")
            .HandleWith(del => CommandsHandlers.HandleDebugItemAttributesCommand(api, del))
            .EndSubCommand();
    }


    public override void Dispose()
    {
        HarmonyInstance.UnpatchAll(HarmonyId);
    }

    public static void LogWarning(string message)
    {
        ServerApi?.Logger.Warning(message);
        ClientApi?.Logger.Warning(message);
    }
    
    public static void LogError(string message)
    {
        ServerApi?.Logger.Error(message);
        ClientApi?.Logger.Error(message);
    }

    /// <summary>
    /// Loads the configuration for the mod from the configuration file or generates a default configuration if none is found or if an error occurs.
    /// </summary>
    /// <param name="api">The core API instance used to load the configuration.</param>
    public static void LoadConfig(ICoreAPI api)
    {
        try
        {
            Config = api.LoadModConfig<ModConfig>(ConfigFileName);
            if (Config != null && Config.Rarities.Any())
            {
                api.StoreModConfig(Config, ConfigFileName); // Store it again in game the mod added new properties
                api.Logger.Notification("[ItemRarity] Configuration loaded.");
                return;
            }

            Config = ModConfig.GetDefaultConfig();
            api.StoreModConfig(Config, ConfigFileName);
            api.Logger.Notification("[ItemRarity] Configuration not found. Generating default configuration.");
        }
        catch
        {
            api.Logger.Warning("[ItemRarity] Failed to load configuration. Falling back to the default configuration (Will not overwrite existing configuration).");
            Config = ModConfig.GetDefaultConfig();
        }
    }
}