using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using UnityEngine;
using Random = UnityEngine.Random;

// TODO: Add blacklisted items from spawning and replace them with an allowed item.

namespace Rust.Harmony.LootTables;

public class Manager : IHarmonyModHooks
{
    public const string Name = "LootTables";
    public const string Author = "Strobez";

    private const int scrapMultiplier = 2;
    private const int defaultMultiplier = 2;

    private const float minRespawnTime = 3600f;
    private const float maxRespawnTime = 7200f;

    private static readonly List<ItemCategory> BlacklistedMultipliersCategories = new()
    {
        ItemCategory.Weapon,
        ItemCategory.Construction,
        ItemCategory.Items,
        ItemCategory.Medical,
        ItemCategory.Food,
        ItemCategory.Ammunition,
        ItemCategory.Common,
        ItemCategory.Component
    };

    private static readonly List<string> BlacklistedMultipliersShortnames = new()
    {
        "fuse"
    };

    private static Version Version => Assembly.GetExecutingAssembly().GetName().Version;

    void IHarmonyModHooks.OnLoaded(OnHarmonyModLoadedArgs args)
    {
        Debug.LogWarning($"[Harmony] Loaded: {Name} {Version} by {Author}");
    }

    void IHarmonyModHooks.OnUnloaded(OnHarmonyModUnloadedArgs args)
    {
        Debug.LogWarning($"[Harmony] Unloaded: {Name} {Version} by {Author}");
    }

    private static IEnumerable<CodeInstruction> HandleGenerateScrap(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> list = new(instructions);

        int idx = list.FindLastIndex(x => x.opcode == OpCodes.Ldloc_0);

        if (idx == -1)
        {
            Debug.LogWarning("[UFO.Harmony.Loot] Failed to find idx of Ldloc_0 in LootContainer.GenerateScrap");
            return list;
        }

        list.InsertRange(idx + 1, new[]
        {
            new CodeInstruction(OpCodes.Ldc_I4, scrapMultiplier),
            new CodeInstruction(OpCodes.Mul)
        });

        return list;
    }

    private static bool HandleSpawnLoot(LootContainer lootContainer)
    {
        if (lootContainer.inventory == null)
        {
            Debug.Log("CONTACT DEVELOPERS! LootContainer::PopulateLoot has null inventory!!!");
        }
        else
        {
            lootContainer.inventory.Clear();
            ItemManager.DoRemoves();

            PopulateLoot(lootContainer);

            if (!lootContainer.shouldRefreshContents) return false;

            lootContainer.Invoke(lootContainer.SpawnLoot,
                Random.Range(minRespawnTime, maxRespawnTime));

            return false;
        }

        return true;
    }

    internal static void PopulateLoot(LootContainer container)
    {
        if (container.LootSpawnSlots.Length != 0)
            foreach (LootContainer.LootSpawnSlot lootSpawnSlot in container.LootSpawnSlots)
            {
                for (int idx = 0; idx < lootSpawnSlot.numberToSpawn; ++idx)
                    if (Random.Range(0.0f, 1) <= (double)lootSpawnSlot.probability)
                    {
                        foreach (Item obj in container.inventory.itemList) HandleLootMultiplication(obj);

                        lootSpawnSlot.definition.SpawnIntoContainer(container.inventory);
                    }
            }
        else if (container.lootDefinition != null)
            for (int idx = 0; idx < container.maxDefinitionsToSpawn; ++idx)
            {
                foreach (Item obj in container.inventory.itemList) HandleLootMultiplication(obj);

                container.lootDefinition.SpawnIntoContainer(container.inventory);
            }

        if (container.SpawnType is LootContainer.spawnType.ROADSIDE or LootContainer.spawnType.TOWN)
            foreach (Item obj in container.inventory.itemList)
            {
                HandleLootMultiplication(obj);

                if (obj.hasCondition)
                    obj.condition = Random.Range(obj.info.condition.foundCondition.fractionMin,
                                        obj.info.condition.foundCondition.fractionMax) *
                                    obj.info.condition.max;
            }

        container.GenerateScrap();
    }

    internal static void HandleLootMultiplication(Item item)
    {
        if (!BlacklistedMultipliersCategories.Contains(item.info.category) ||
            BlacklistedMultipliersShortnames.Contains(item.info.shortname))
            return;

        item.amount *= defaultMultiplier;
    }

    [HarmonyPatch(typeof(LootContainer), nameof(LootContainer.SpawnLoot))]
    internal static class LootContainer_SpawnLoot_Patch
    {
        [HarmonyPrefix]
        internal static bool Prefix(LootContainer __instance)
        {
            return HandleSpawnLoot(__instance);
        }
    }

    [HarmonyPatch(typeof(LootContainer), nameof(LootContainer.GenerateScrap))]
    internal static class LootContainer_GenerateScrap_Patch
    {
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return HandleGenerateScrap(instructions);
        }
    }
}