using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Each scene has its own Player, so when you change scenes the new Player starts with default values.
/// This remembers the old Player's health, Divine Energy, money, items and equipment when a scene
/// transition starts, and gives them to the new Player when it appears. It also moves the new Player to the
/// Entry Point. The snapshot is kept, so "Restart Level" returns you to the way you entered the scene.
/// </summary>
public static class PlayerCarryOver
{
    private class Snapshot
    {
        public int health;
        public float divineEnergy;
        public int money;
        public List<ItemStack> items = new List<ItemStack>();
        public readonly ItemData[] equipped = new ItemData[3];
    }

    private static readonly GameAction[] Slots = { GameAction.Action1, GameAction.Action2, GameAction.Action3 };

    private static Snapshot snapshot;
    private static string entryId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        snapshot = null;
        entryId = null;
    }

    public static void Clear()
    {
        snapshot = null;
        entryId = null;
    }

    /// <summary>Called when a scene transition starts: remembers the player and where they should appear.</summary>
    public static void Capture(string entryIdForNextScene)
    {
        entryId = entryIdForNextScene;

        PlayerStats stats = PlayerStats.Instance;
        if (stats == null)
        {
            snapshot = null;
            return;
        }

        Snapshot s = new Snapshot
        {
            health = stats.Health,
            divineEnergy = stats.DivineEnergyExact,
            money = stats.Money
        };

        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory != null)
        {
            s.items = inventory.Snapshot();

            PlayerCombat combat = inventory.Combat;
            if (combat != null)
            {
                for (int i = 0; i < Slots.Length; i++) s.equipped[i] = combat.GetEquipped(Slots[i]);
            }
        }

        snapshot = s;
    }

    /// <summary>Called when the new Player has started: gives it the carried values and moves it to the entry point.</summary>
    public static void ApplyToNewScene(PlayerStats stats, PlayerInventory inventory, PlayerCombat combat)
    {
        if (snapshot != null)
        {
            if (stats != null) stats.SetState(snapshot.health, snapshot.divineEnergy, snapshot.money);
            if (inventory != null) inventory.Load(snapshot.items);

            if (combat != null)
            {
                for (int i = 0; i < Slots.Length; i++) combat.Equip(Slots[i], snapshot.equipped[i]);
            }
        }

        if (!string.IsNullOrEmpty(entryId)) MoveToEntry(stats);
    }

    private static void MoveToEntry(PlayerStats stats)
    {
        if (stats == null) return;

        EntryPoint entry = null;
        foreach (EntryPoint candidate in Object.FindObjectsByType<EntryPoint>(FindObjectsSortMode.None))
        {
            if (candidate.Id == entryId)
            {
                entry = candidate;
                break;
            }
        }

        if (entry == null)
        {
            Debug.LogWarning("PlayerCarryOver: no EntryPoint with the Id '" + entryId + "' in this scene. The player stays where it is.");
            return;
        }

        // Let the respawn manager note where the scene itself starts, before the player moves
        RespawnManager respawn = RespawnManager.Instance;

        PlayerController controller = stats.GetComponent<PlayerController>();
        if (controller != null) controller.TeleportTo(entry.Position);
        else stats.transform.position = entry.Position;

        respawn.ArrivedAt(entry.Position);

        CameraFollow cam = Object.FindFirstObjectByType<CameraFollow>();
        if (cam != null) cam.SnapToTarget();
    }
}