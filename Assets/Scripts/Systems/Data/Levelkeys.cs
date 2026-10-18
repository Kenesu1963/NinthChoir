using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Counts the keys the player has collected in the current level, by Key Id (for example "RedKey" or "Gem").
/// It lives only inside the scene, so keys are forgotten when the scene is reloaded or left. Creates itself.
/// </summary>
public class LevelKeys : MonoBehaviour
{
    private static LevelKeys instance;

    public static LevelKeys Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("LevelKeys");
                instance = go.AddComponent<LevelKeys>();
            }
            return instance;
        }
    }

    private readonly Dictionary<string, int> counts = new Dictionary<string, int>();

    public event Action Changed;

    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public int Count(string id)
    {
        int count;
        return !string.IsNullOrEmpty(id) && counts.TryGetValue(id, out count) ? count : 0;
    }

    public void Add(string id, int amount = 1)
    {
        if (string.IsNullOrEmpty(id) || amount <= 0) return;

        counts[id] = Count(id) + amount;
        Changed?.Invoke();
    }

    public bool Remove(string id, int amount = 1)
    {
        if (string.IsNullOrEmpty(id) || amount <= 0 || Count(id) < amount) return false;

        counts[id] = Count(id) - amount;
        Changed?.Invoke();
        return true;
    }
}