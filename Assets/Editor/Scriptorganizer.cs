using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds the menu  Items -> Organize Scripts.
/// It moves every script into a folder by purpose (components, base classes, derived items, systems...).
/// Scripts are moved with Unity's own tools, so nothing attached in your scenes or prefabs breaks.
/// Nothing is renamed or deleted, and you can run it again any time. You can delete this file afterwards.
/// This file must stay inside a folder called "Editor".
/// </summary>
public static class ScriptOrganizer
{
    private const string Root = "Assets/Scripts";

    // folder (inside Assets/Scripts)  ->  scripts that go there
    private static readonly Dictionary<string, string[]> Layout = new Dictionary<string, string[]>
    {
        // Scripts you add to objects in the scene
        { "Components/Player",  new[] { "PlayerController", "PlayerCombat", "PlayerStats", "PlayerInventory" } },
        { "Components/Camera",  new[] { "CameraFollow" } },
        { "Components/NPC",     new[] { "NpcDialogue", "Shop" } },
        { "Components/Enemies", new[] { "WalkerEnemy", "RiflerEnemy", "FlyingImpEnemy", "FlyingRiflerEnemy" } },
        { "Components/World",   new[] { "Sign", "Chest", "ItemPickup", "SimpleTrade", "TrainingDummy", "DeathZone", "Checkpoint", "RespawnPoint", "Teleporter", "SceneTransition", "EntryPoint", "KeyGate", "KeyPickup" } },

        // Base classes and interfaces that other scripts extend
        { "Base",               new[] { "ItemData", "WeaponItem", "ToolItem", "IncantationItem", "ConsumableItem", "Interactable", "IDamageable", "EnemyBase" } },

        // Derived classes: the actual items
        { "Items/Weapons",      new[] { "MeleeWeaponItem", "SaberItem" } },
        { "Items/Tools",        new[] { "ShieldToolItem", "PickToolItem" } },
        { "Items/Incantations", new[] { "ProjectileIncantationItem", "LightBeamIncantationItem" } },
        { "Items/Consumables",  new[] { "FoodItem", "PotionItem", "WineItem" } },

        // Systems that create themselves: you never attach these
        { "Systems/UI",         new[] { "InventoryMenu", "ShopMenu", "StatsHUD", "DialogueManager", "DeathScreen", "ScreenFader" } },
        { "Systems/Respawn",    new[] { "RespawnManager" } },
        { "Systems/Input",      new[] { "GameControls", "InputLock" } },
        { "Systems/Data",       new[] { "DialogueData", "ItemPages", "TradeUtility", "PatrolPath", "PlayerCarryOver", "LevelKeys", "EnemyShooting", "TriggerInteract" } },

        // Objects the game creates while playing
        { "Spawned/Combat",     new[] { "Hitbox", "Projectile", "LightBeam", "ChargeGlow" } },
        { "Spawned/Loot",       new[] { "LootBody", "CoinPickup" } },
    };

    [MenuItem("Items/Organize Scripts")]
    private static void Organize()
    {
        int moved = 0;
        List<string> notFound = new List<string>();

        foreach (KeyValuePair<string, string[]> group in Layout)
        {
            string folder = EnsureFolder(Root + "/" + group.Key);

            foreach (string scriptName in group.Value)
            {
                string path = FindScript(scriptName);
                if (path == null)
                {
                    notFound.Add(scriptName);
                    continue;
                }

                string target = folder + "/" + Path.GetFileName(path); // keeps the file's own name
                if (path == target) continue;

                string error = AssetDatabase.MoveAsset(path, target);
                if (string.IsNullOrEmpty(error)) moved++;
                else Debug.LogWarning("Couldn't move " + scriptName + ": " + error);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message = "Organize Scripts: moved " + moved + " scripts.";
        if (notFound.Count > 0) message += " Not found (skipped): " + string.Join(", ", notFound);
        Debug.Log(message);
    }

    // Finds a script by file name anywhere in the project, except in Editor folders.
    private static string FindScript(string scriptName)
    {
        foreach (string guid in AssetDatabase.FindAssets(scriptName + " t:MonoScript"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/")) continue;
            if (path.Contains("/Editor/")) continue;

            string fileName = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(fileName, scriptName, System.StringComparison.OrdinalIgnoreCase)) return path;
        }
        return null;
    }

    // Creates a folder path like "Assets/Scripts/Components/Player", one level at a time.
    private static string EnsureFolder(string fullPath)
    {
        string[] parts = fullPath.Split('/');
        string current = parts[0]; // "Assets"

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
        return current;
    }
}