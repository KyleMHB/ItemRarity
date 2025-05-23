using System.Collections.Generic;
using System.Linq;

namespace ItemRarity.Config;

/// <summary>
/// Represents the configuration.
/// </summary>
public sealed class ModConfig
{
    public Dictionary<string, ItemRarityConfig> Rarities { get; init; } = new();
    public List<ToolkitTierConfig> Toolkits { get; init; } = new();
    
    public ItemRarityInfos this[string rarity]
    {
        get
        {
            if (Rarities.TryGetValue(rarity, out var result))
                return (rarity, result);

            var first = Rarities.First();
            return (first.Key, first.Value);
        }
    }

    public static ModConfig GetDefaultConfig()
    {
        var defaultConfig = new ModConfig
        {
            Rarities = new()
            {
                {
                    "cursed", new()
                    {
                        Name = "Cursed",
                        Color = "#606060",
                        Rarity = 8,
                        DurabilityMultiplier = 0.5F,
                        MiningSpeedMultiplier = 0.5F,
                        AttackPowerMultiplier = 0.5F,
                        PiercingPowerMultiplier = 0.5F,
                        ArmorFlatDamageReductionMultiplier = 0.5F,
                        ArmorPerTierFlatDamageProtectionLossMultiplier = 0.5F,
                        ArmorRelativeProtectionMultiplier = 0.5F,
                        ArmorPerTierRelativeProtectionLossMultiplier = 0.5F,
                        ShieldProtectionMultiplier = 0.5F
                    }
                },

                {
                    "common", new()
                    {
                        Name = "Common",
                        Color = "#FFFFFF",
                        Rarity = 40,
                        DurabilityMultiplier = 1F,
                        MiningSpeedMultiplier = 1F,
                        AttackPowerMultiplier = 1F,
                        PiercingPowerMultiplier = 1F,
                        ArmorFlatDamageReductionMultiplier = 1F,
                        ArmorPerTierFlatDamageProtectionLossMultiplier = 1F,
                        ArmorRelativeProtectionMultiplier = 1F,
                        ArmorPerTierRelativeProtectionLossMultiplier = 1F,
                        ShieldProtectionMultiplier = 1F
                    }
                },

                {
                    "uncommon", new()
                    {
                        Name = "Uncommon",
                        Color = "#36FF00",
                        Rarity = 30,
                        DurabilityMultiplier = 1.1F,
                        MiningSpeedMultiplier = 1.1F,
                        AttackPowerMultiplier = 1.1F,
                        PiercingPowerMultiplier = 1.1F,
                        ArmorFlatDamageReductionMultiplier = 1.1F,
                        ArmorPerTierFlatDamageProtectionLossMultiplier = 1.1F,
                        ArmorRelativeProtectionMultiplier = 1.1F,
                        ArmorPerTierRelativeProtectionLossMultiplier = 1.1F,
                        ShieldProtectionMultiplier = 1.1F
                    }
                },

                {
                    "rare", new()
                    {
                        Name = "Rare",
                        Color = "#13DBE8",
                        Rarity = 20,
                        DurabilityMultiplier = 1.2F,
                        MiningSpeedMultiplier = 1.2F,
                        AttackPowerMultiplier = 1.2F,
                        PiercingPowerMultiplier = 1.2F,
                        ArmorFlatDamageReductionMultiplier = 1.2F,
                        ArmorPerTierFlatDamageProtectionLossMultiplier = 1.2F,
                        ArmorRelativeProtectionMultiplier = 1.2F,
                        ArmorPerTierRelativeProtectionLossMultiplier = 1.2F,
                        ShieldProtectionMultiplier = 1.2F
                    }
                },

                {
                    "epic", new()
                    {
                        Name = "Epic",
                        Color = "#8413E8",
                        Rarity = 12,
                        DurabilityMultiplier = 1.4F,
                        MiningSpeedMultiplier = 1.3F,
                        AttackPowerMultiplier = 1.3F,
                        PiercingPowerMultiplier = 1.3F,
                        ArmorFlatDamageReductionMultiplier = 1.3F,
                        ArmorPerTierFlatDamageProtectionLossMultiplier = 1.3F,
                        ArmorRelativeProtectionMultiplier = 1.3F,
                        ArmorPerTierRelativeProtectionLossMultiplier = 1.3F,
                        ShieldProtectionMultiplier = 1.3F
                    }
                },

                {
                    "legendary", new()
                    {
                        Name = "Legendary",
                        Color = "#E08614",
                        Rarity = 8,
                        DurabilityMultiplier = 1.6F,
                        MiningSpeedMultiplier = 1.5F,
                        AttackPowerMultiplier = 1.5F,
                        PiercingPowerMultiplier = 1.5F,
                        ArmorFlatDamageReductionMultiplier = 1.5F,
                        ArmorPerTierFlatDamageProtectionLossMultiplier = 1.5F,
                        ArmorRelativeProtectionMultiplier = 1.5F,
                        ArmorPerTierRelativeProtectionLossMultiplier = 1.5F,
                        ShieldProtectionMultiplier = 1.6F
                    }
                },

                {
                    "unique", new()
                    {
                        Name = "Unique",
                        Color = "#EC290E",
                        Rarity = 2,
                        DurabilityMultiplier = 2F,
                        MiningSpeedMultiplier = 1.9F,
                        AttackPowerMultiplier = 1.9F,
                        PiercingPowerMultiplier = 1.9F,
                        ArmorFlatDamageReductionMultiplier = 1.9F,
                        ArmorPerTierFlatDamageProtectionLossMultiplier = 1.9F,
                        ArmorRelativeProtectionMultiplier = 1.9F,
                        ArmorPerTierRelativeProtectionLossMultiplier = 1.9F,
                        ShieldProtectionMultiplier = 1.9F,
                        Effects = ["Thor"]
                    }
                }
            },
            Toolkits = new List<ToolkitTierConfig>
            {
                new ToolkitTierConfig
                {
                    TierName = "Stone Age Kit",
                    OutputCode = "itemrarity:toolkit-tier1",
                    ToolInputs = new List<ToolInput>
                    {
                        new ToolInput { ItemCode = "game:flint", Quantity = 5 },
                        new ToolInput { ItemCode = "game:stone", Quantity = 10 }
                    },
                    PossibleRarities = new List<string> { "common", "uncommon" },
                    RarityWeights = new Dictionary<string, float>
                    {
                        { "common", 75 },
                        { "uncommon", 25 }
                    }
                },
                new ToolkitTierConfig
                {
                    TierName = "Copper Age Kit",
                    OutputCode = "itemrarity:toolkit-tier2",
                    ToolInputs = new List<ToolInput>
                    {
                        new ToolInput { ItemCode = "game:ingot-copper", Quantity = 2 },
                        new ToolInput { ItemCode = "game:resin", Quantity = 3 }
                    },
                    PossibleRarities = new List<string> { "uncommon", "rare" },
                    RarityWeights = new Dictionary<string, float>
                    {
                        { "uncommon", 70 },
                        { "rare", 30 }
                    }
                },
                new ToolkitTierConfig
                {
                    TierName = "Bronze Age Kit",
                    OutputCode = "itemrarity:toolkit-tier3",
                    ToolInputs = new List<ToolInput>
                    {
                        new ToolInput { ItemCode = "game:ingot-bronze", Quantity = 2 },
                        new ToolInput { ItemCode = "game:metalparts", Quantity = 4 }
                    },
                    PossibleRarities = new List<string> { "uncommon", "rare", "epic", "legendary" },
                    RarityWeights = new Dictionary<string, float>
                    {
                        { "uncommon", 60 },
                        { "rare", 25 },
                        { "epic", 10 },
                        { "legendary", 5 }
                    }
                },
                new ToolkitTierConfig
                {
                    TierName = "Iron Age Kit",
                    OutputCode = "itemrarity:toolkit-tier4",
                    ToolInputs = new List<ToolInput>
                    {
                        new ToolInput { ItemCode = "game:ingot-iron", Quantity = 2 },
                        new ToolInput { ItemCode = "game:metalparts", Quantity = 6 }
                    },
                    PossibleRarities = new List<string> { "rare", "epic", "legendary" },
                    RarityWeights = new Dictionary<string, float>
                    {
                        { "rare", 60 },
                        { "epic", 30 },
                        { "legendary", 10 }
                    }
                },
                new ToolkitTierConfig
                {
                    TierName = "Steel Age Kit",
                    OutputCode = "itemrarity:toolkit-tier5",
                    ToolInputs = new List<ToolInput>
                    {
                        new ToolInput { ItemCode = "game:ingot-steel", Quantity = 2 },
                        new ToolInput { ItemCode = "game:gear-temporal", Quantity = 1 }
                    },
                    PossibleRarities = new List<string> { "rare", "epic", "legendary", "unique" },
                    RarityWeights = new Dictionary<string, float>
                    {
                        { "rare", 50 },
                        { "epic", 25 },
                        { "legendary", 20 },
                        { "unique", 5 }
                    }
                }
            }
        };
        return defaultConfig;
    }
}