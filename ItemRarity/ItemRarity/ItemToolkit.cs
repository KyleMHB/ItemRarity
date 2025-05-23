using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Config;
using ItemRarity.Config; // Assuming ModCore and ToolkitConfig are in this namespace or accessible
using ItemRarity.Patches; // For Rarity class
using System.Linq;
using System.Collections.Generic; // For List and Dictionary

namespace ItemRarity
{
    public class ItemToolkit : Item
    {
        ModCore modSystem;
        ICoreAPI api; // Store API for logging

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            this.api = api; // Store API
            modSystem = api.ModLoader.GetModSystem<ModCore>();
            if (modSystem == null)
            {
                api.Logger.Error("[ItemRarity] ModCore system not found!");
            }
        }

        public string ToolkitIdentifier => this.Code.Path; // Use the item's code path as the identifier for config lookup

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstTick, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefault; // Prevent default interaction
            api.Logger.Debug($"ItemToolkit: Using toolkit '{ToolkitIdentifier}'. Player: {byEntity.GetName()}");

            if (!(byEntity is EntityPlayer player))
            {
                return;
            }

            IPlayer byPlayer = player.Player;
            if (byPlayer == null) return;

            // Get the slot to the right of the active hotbar slot
            ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (activeHotbarSlot == null)
            {
                api.Logger.Debug($"ItemToolkit: No active hotbar slot for player {byPlayer.PlayerUID}.");
                return;
            }

            int targetSlotIndex = activeHotbarSlot.SlotID + 1;
            if (targetSlotIndex >= byPlayer.InventoryManager.GetHotbarInventory().Count)
            {
                api.Logger.Debug($"ItemToolkit: Target slot index {targetSlotIndex} is out of bounds for hotbar size {byPlayer.InventoryManager.GetHotbarInventory().Count}.");
                if (modSystem.Api.Side == EnumAppSide.Client)
                {
                    (byPlayer as IClientPlayer)?.ShowChatNotification("Place an item to the right of the toolkit in the hotbar.");
                }
                return;
            }

            ItemSlot targetSlot = byPlayer.InventoryManager.GetHotbarInventory()[targetSlotIndex];

            if (targetSlot == null || targetSlot.Empty)
            {
                api.Logger.Debug($"ItemToolkit: No item in target slot index {targetSlotIndex}.");
                if (modSystem.Api.Side == EnumAppSide.Client)
                {
                    (byPlayer as IClientPlayer)?.ShowChatNotification("No item to the right of the toolkit to modify.");
                }
                return;
            }

            ItemStack targetItemStack = targetSlot.Itemstack;
            api.Logger.Debug($"ItemToolkit: Targeting item '{targetItemStack.GetName()}' (Code: {targetItemStack.ItemAttributes?["rarityGuid"]?.ToString() ?? targetItemStack.Collectible.Code.ToString()}) in slot {targetSlotIndex}.");


            if (!Rarity.IsSuitableFor(targetItemStack, false)) // false allows re-rolling existing rarity
            {
                api.Logger.Debug($"ItemToolkit: Item '{targetItemStack.GetName()}' (Code: {targetItemStack.Collectible.Code}) is not suitable for toolkit modification.");
                if (modSystem.Api.Side == EnumAppSide.Client)
                {
                    (byPlayer as IClientPlayer)?.ShowChatNotification("This item cannot be modified with this toolkit.");
                }
                return;
            }

            // Get Toolkit Configuration
            if (modSystem.Config.Toolkits == null || !modSystem.Config.Toolkits.TryGetValue(ToolkitIdentifier, out ToolkitConfig toolkitConfig))
            {
                api.Logger.Error($"[ItemRarity] Toolkit configuration not found for {ToolkitIdentifier}. Cannot apply rarity.");
                if (modSystem.Api.Side == EnumAppSide.Client)
                {
                    (byPlayer as IClientPlayer)?.ShowChatNotification("Error: Toolkit configuration missing.");
                }
                return;
            }
            api.Logger.Debug($"ItemToolkit: Found configuration for toolkit '{ToolkitIdentifier}': Name='{toolkitConfig.Name}'.");

            // Select Rarity
            string selectedRarityKey = GetRandomRarityFromPool(toolkitConfig, modSystem.Config, api); // Pass api from instance field
            api.Logger.Debug($"ItemToolkit: GetRandomRarityFromPool returned '{selectedRarityKey}' for toolkit '{ToolkitIdentifier}'.");

            if (string.IsNullOrEmpty(selectedRarityKey) || !modSystem.Config.Rarities.ContainsKey(selectedRarityKey))
            {
                api.Logger.Error($"[ItemRarity] Could not select a valid rarity for {ToolkitIdentifier} (selected: '{selectedRarityKey}'). Check RarityPool, RarityWeights, and global Rarity definitions.");
                 if (modSystem.Api.Side == EnumAppSide.Client)
                {
                    (byPlayer as IClientPlayer)?.ShowChatNotification("Error: Could not determine rarity to apply.");
                }
                return;
            }

            // Apply Rarity
            Rarity.SetRarity(targetItemStack, selectedRarityKey, api); // Pass api from instance field
            targetSlot.MarkDirty();
            api.Logger.Notification($"ItemToolkit: Successfully applied rarity '{selectedRarityKey}' to item '{targetItemStack.GetName()}' (Original code: {targetItemStack.Collectible.Code}) using toolkit '{ToolkitIdentifier}'. New item name: {targetItemStack.GetName()}");


            // Consume Toolkit
            slot.TakeOut(1);
            slot.MarkDirty();

            // Provide Feedback (Client-side only for chat)
            if (api.Side == EnumAppSide.Client)
            {
                string rarityName = modSystem.Config.Rarities[selectedRarityKey].Name;
                string itemName = targetItemStack.GetName();
                (byPlayer as IClientPlayer)?.ShowChatNotification($"Applied {rarityName} to {itemName}!");
            }
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            string baseName = base.GetHeldItemName(itemStack);
            // Ensure modSystem and its Config are loaded before trying to access them
            if (modSystem?.Config?.Toolkits != null && modSystem.Config.Toolkits.TryGetValue(ToolkitIdentifier, out ToolkitConfig toolkitConfig))
            {
                if (!string.IsNullOrEmpty(toolkitConfig.Name))
                {
                    return $"{toolkitConfig.Name} ({baseName})";
                }
            }
            return baseName;
        }
        
        public static string GetRandomRarityFromPool(ToolkitConfig toolkitConfig, ModConfig globalConfig, ICoreAPI api)
        {
            api.Logger.Debug($"ItemToolkit: Calculating random rarity for toolkit '{toolkitConfig.Name}' (Code: {toolkitConfig.Code}).");

            if (toolkitConfig.RarityPool == null || !toolkitConfig.RarityPool.Any())
            {
                api.Logger.Warning($"[ItemRarity] Toolkit '{toolkitConfig.Code}': RarityPool is null or empty.");
                return null; 
            }
            if (toolkitConfig.RarityWeights == null || !toolkitConfig.RarityWeights.Any())
            {
                 api.Logger.Warning($"[ItemRarity] Toolkit '{toolkitConfig.Code}': RarityWeights is null or empty.");
                return null;
            }

            List<string> validPool = new List<string>();
            foreach (string rarityKeyInPool in toolkitConfig.RarityPool)
            {
                if (!globalConfig.Rarities.ContainsKey(rarityKeyInPool))
                {
                    api.Logger.Warning($"[ItemRarity] Toolkit '{toolkitConfig.Code}': Rarity key '{rarityKeyInPool}' in RarityPool is not defined in globalConfig.Rarities. Skipping.");
                    continue;
                }
                if (!toolkitConfig.RarityWeights.ContainsKey(rarityKeyInPool))
                {
                    api.Logger.Warning($"[ItemRarity] Toolkit '{toolkitConfig.Code}': Rarity key '{rarityKeyInPool}' in RarityPool does not have a corresponding weight in RarityWeights. Skipping.");
                    continue;
                }
                if (toolkitConfig.RarityWeights[rarityKeyInPool] <= 0)
                {
                     api.Logger.Warning($"[ItemRarity] Toolkit '{toolkitConfig.Code}': Rarity key '{rarityKeyInPool}' has a weight of '{toolkitConfig.RarityWeights[rarityKeyInPool]}', which is <= 0. Skipping.");
                    continue;
                }
                validPool.Add(rarityKeyInPool);
            }


            if (!validPool.Any())
            {
                api.Logger.Warning($"[ItemRarity] Toolkit '{toolkitConfig.Code}': No valid rarities found in RarityPool after filtering against global rarities and weights. Pool was: [{string.Join(", ", toolkitConfig.RarityPool)}]");
                return null;
            }
            api.Logger.Debug($"ItemToolkit: Toolkit '{toolkitConfig.Code}'. Validated RarityPool: [{string.Join(", ", validPool)}].");

            float totalWeight = validPool.Sum(rarityKey => toolkitConfig.RarityWeights[rarityKey]);
            api.Logger.Debug($"ItemToolkit: Toolkit '{toolkitConfig.Code}'. Calculated totalWeight for valid pool: {totalWeight}.");


            if (totalWeight <= 0) // This check might be redundant if individual non-positive weights are skipped, but kept for safety.
            {
                api.Logger.Warning($"[ItemRarity] Toolkit '{toolkitConfig.Code}': Total weight for valid pool is {totalWeight}, which is zero or negative. Cannot select rarity.");
                // Fallback: return the first valid rarity if total weight is invalid, though this case should ideally be prevented by earlier checks.
                return validPool.FirstOrDefault(); 
            }

            double roll = api.World.Rand.NextDouble() * totalWeight;
            float currentWeightSum = 0;
            string selectedRarityKey = null;

            foreach (string rarityKey in validPool)
            {
                currentWeightSum += toolkitConfig.RarityWeights[rarityKey];
                if (roll < currentWeightSum)
                {
                    selectedRarityKey = rarityKey;
                    break;
                }
            }
            
            if (selectedRarityKey == null) // Fallback, should ideally not be reached if logic is correct
            {
                 api.Logger.Warning($"[ItemRarity] GetRandomRarityFromPool fell through for toolkit '{toolkitConfig.Code}'. This might indicate an issue with weight calculation or random roll. Returning first valid rarity from pool: '{validPool.FirstOrDefault()}'. Roll was {roll}, totalWeight {totalWeight}.");
                selectedRarityKey = validPool.FirstOrDefault();
            }

            api.Logger.Debug($"ItemToolkit: Toolkit '{toolkitConfig.Code}'. Selected rarity key: '{selectedRarityKey}'.");
            return selectedRarityKey;
        }
    }
}
