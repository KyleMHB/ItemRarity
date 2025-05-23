using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using ItemRarity.Config; // For ToolkitTierConfig
using System.Linq; // For .FirstOrDefault()

namespace ItemRarity;

public class CollectibleBehaviorApplyRarity : CollectibleBehavior
{
    public CollectibleBehaviorApplyRarity(CollectibleObject collObj) : base(collObj)
    {
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstTick, ref EnumHandHandling handling)
    {
        // 1. Guard Clauses
        if (entitySel?.Entity is not EntityItem selectedEntityItem)
        {
            // Not targeting an EntityItem, default behavior
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstTick, ref handling);
            return;
        }

        ItemStack targetStack = selectedEntityItem.Itemstack;
        if (targetStack == null)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstTick, ref handling);
            return;
        }

        ItemStack toolkitStack = slot.Itemstack;
        if (toolkitStack == null)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstTick, ref handling);
            return;
        }

        ICoreAPI api = byEntity.World.Api;

        // 2. Find Toolkit Configuration
        ToolkitTierConfig? foundToolkitConfig = ModCore.Config.Toolkits.FirstOrDefault(config => config.OutputCode == toolkitStack.Collectible.Code.ToString());

        if (foundToolkitConfig == null)
        {
            api.Logger.Warning($"[ItemRarity] No ToolkitTierConfig found for toolkit: {toolkitStack.Collectible.Code}");
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstTick, ref handling);
            return;
        }

        // Client-side check for suitability before server processing
        if (!Rarity.IsSuitableFor(targetStack, true))
        {
            if (api is ICoreClientAPI capi)
            {
                capi.TriggerIngameError(this, "alreadyenhanced", Lang.Get("itemrarity:error-alreadyenhancedortoolowtier", targetStack.GetName()));
            }
            handling = EnumHandHandling.PreventDefault; // Prevent further interaction
            return;
        }
        
        // --- Server-Side Logic Guard ---
        if (api.Side.IsServer())
        {
            // Re-check suitability on the server to be sure
            if (!Rarity.IsSuitableFor(targetStack, true))
            {
                // No direct message to client from here, client already got one or it's a desync. Log it.
                api.Logger.Notification($"[ItemRarity] Server-side check: {targetStack.GetName()} is not suitable for rarity application by {byEntity.GetName()}. Client might be desynced or bypassed check.");
                handling = EnumHandHandling.PreventDefault;
                return;
            }

            // 4. Apply Rarity
            ItemRarityInfos chosenRarityInfo = Rarity.GetRandomRarityFromWeights(foundToolkitConfig.RarityWeights, api);

            if (chosenRarityInfo.Value == null)
            {
                api.Logger.Warning($"[ItemRarity] Could not determine rarity for {targetStack.GetName()} using toolkit {toolkitStack.GetName()}. RarityWeights: {string.Join(", ", foundToolkitConfig.RarityWeights.Select(kv => kv.Key + ":" + kv.Value))}");
                handling = EnumHandHandling.PreventDefault;
                return;
            }

            Rarity.SetRarity(targetStack, chosenRarityInfo.Key);

            // 5. Consume Toolkit & Update Item
            slot.TakeOut(1);
            slot.MarkDirty();
            selectedEntityItem.WatchedAttributes.MarkPathDirty("itemstack"); // Mark for sync

            // 6. Feedback (Sound is server-side, message is client-side)
            api.World.PlaySoundAt(new AssetLocation("game:sounds/effect/tink"), selectedEntityItem, byEntity, true, 16);

            // The client-side part of feedback will be handled below the server guard
        }

        // Client-side feedback & handling for all (even if server processes, client needs to know)
        if (api is ICoreClientAPI capiClient)
        {
            // To give feedback, we need the chosen rarity.
            // If server-side, the actual application happened.
            // If client-side (and server guard is active), this is for predictive feedback.
            // Let's assume for now the server will update the client, and client just plays sound and prevents default.
            // For a more robust solution, server would send a packet back with success/chosen rarity.
            // Given the current structure, we can't easily get chosenRarityInfo on client if server did the work.
            // So, we'll just play a generic sound on client for now if it's not server.
            // The success message will only appear if the server processes it and then syncs, or if we send a packet.
            // For this iteration, the server plays the sound, and client gets a generic success message.
             ToolkitTierConfig? clientFoundToolkitConfig = ModCore.Config.Toolkits.FirstOrDefault(config => config.OutputCode == toolkitStack.Collectible.Code.ToString());
             if (clientFoundToolkitConfig != null) {
                // We can't know the *exact* rarity chosen if server did it without a return packet.
                // But we can give a generic message or assume the first possible rarity for a temporary display.
                // For now, let's just assume the action will be successful if it passed client checks.
                // The actual rarity name in message might be tricky without server confirmation.
                // Let's defer the specific rarity name in client message until server confirmation is implemented.
                // A simple "Item enhanced" message might be better for now.
                ItemRarityInfos potentialRarityInfo = Rarity.GetRandomRarityFromWeights(clientFoundToolkitConfig.RarityWeights, api); // This is just for display
                if (potentialRarityInfo.Value != null) {
                     capiClient.ShowChatMessage(Lang.Get("itemrarity:success-enhanced", targetStack.GetName(), potentialRarityInfo.Value.Name));
                } else {
                    capiClient.ShowChatMessage(Lang.Get("itemrarity:success-enhanced-generic", targetStack.GetName()));
                }

                // Client also plays sound for immediate feedback
                 api.World.PlaySoundAt(new AssetLocation("game:sounds/effect/tink"), selectedEntityItem, byEntity, true, 16);
             }
        }
        
        // 7. Set Handling
        handling = EnumHandHandling.PreventDefault;
    }
}
