using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds the menu  Items -> Create Default Item Assets.
/// It creates an asset for every item in Assets/Items, so you can drag them into shops, chests,
/// pickups, the starting items list or the equipped slots. Run it again any time: it never overwrites.
/// This file must stay inside a folder called "Editor".
/// </summary>
public static class ItemAssetCreator
{
    [MenuItem("Items/Create Default Item Assets")]
    private static void CreateAll()
    {
        const string folder = "Assets/Items";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "Items");

        // --- Equipment ---
        Create<MeleeWeaponItem>(folder, "Rapier");
        Create<SaberItem>(folder, "Saber");
        Create<ShieldToolItem>(folder, "Shield");
        Create<PickToolItem>(folder, "Pick");
        Create<ProjectileIncantationItem>(folder, "DivineBolt");
        Create<LightBeamIncantationItem>(folder, "LightBeam");

        // --- Food (Body page): regeneration ---
        Create<FoodItem>(folder, "Bread", f =>
        {
            f.itemName = "Bread";
            f.description = "A plain loaf. It restores a little health, slowly.";
            f.healPerTick = 1; f.tickInterval = 2f; f.ticks = 3;
        });
        Create<FoodItem>(folder, "Tilapia", f =>
        {
            f.itemName = "Tilapia";
            f.description = "Grilled tilapia, fresh from the river. A steady recovery.";
            f.healPerTick = 1; f.tickInterval = 1.5f; f.ticks = 5;
        });
        Create<FoodItem>(folder, "NilePerch", f =>
        {
            f.itemName = "Nile Perch";
            f.description = "A thick cut of Nile perch. Rich, filling and very restorative.";
            f.healPerTick = 2; f.tickInterval = 2f; f.ticks = 4;
        });

        // --- Potions (Mind page) ---
        Create<PotionItem>(folder, "HealingPotion", p =>
        {
            p.itemName = "Healing Potion";
            p.description = "A bitter red draught. It closes wounds at once.";
            p.effect = PotionEffect.InstantHealing; p.healAmount = 5;
        });
        Create<PotionItem>(folder, "StrengthPotion", p =>
        {
            p.itemName = "Strength Potion";
            p.description = "A thick amber brew. Your blows land harder for a while.";
            p.effect = PotionEffect.Strength; p.damageMultiplier = 1.5f; p.duration = 30f;
        });

        // --- Wine (Soul page): Divine Energy ---
        Create<WineItem>(folder, "Wine", w =>
        {
            w.itemName = "Wine";
            w.description = "A deep red wine blessed by the temple. It restores Divine Energy.";
            w.divineEnergyRestored = 5;
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Item assets are in " + folder);
    }

    private static void Create<T>(string folder, string fileName, Action<T> setup = null) where T : ItemData
    {
        string path = folder + "/" + fileName + ".asset";
        if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return;

        T item = ScriptableObject.CreateInstance<T>();
        if (setup != null) setup(item);
        AssetDatabase.CreateAsset(item, path);
    }
}