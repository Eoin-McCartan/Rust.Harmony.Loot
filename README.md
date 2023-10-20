# Rust.Harmony.Loot

Very simple, only supports multplication of scrap, items, blacklisting of multiplication of categories / items, and modifying the entire loot min/max respawn values.

## Hardcoded Configuration
```cs
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
```

## TODO
- Add blacklisted items from spawning and replace them with an allowed item.
