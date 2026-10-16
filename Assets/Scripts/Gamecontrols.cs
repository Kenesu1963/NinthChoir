using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The three action buttons are neutral: they don't do anything by themselves.
/// What each one does depends on the item equipped in that slot (see PlayerCombat).
/// </summary>
public enum GameAction { Action1, Action2, Action3, Crouch, Inventory }

/// <summary>
/// Central place for the action keys. Defaults: J = action 1, K = action 2, L = action 3, S / Down = crouch.
/// To reassign a key (e.g. from a future settings menu):
///     GameControls.Rebind(GameAction.Action1, KeyCode.U);
/// The new key is saved automatically and loaded the next time the game starts.
/// </summary>
public static class GameControls
{
    private static readonly Dictionary<GameAction, KeyCode> primary = new Dictionary<GameAction, KeyCode>
    {
        { GameAction.Action1, KeyCode.J },
        { GameAction.Action2, KeyCode.K },
        { GameAction.Action3, KeyCode.L },
        { GameAction.Crouch,  KeyCode.S },
        { GameAction.Inventory, KeyCode.F },
    };

    private static readonly Dictionary<GameAction, KeyCode> alternate = new Dictionary<GameAction, KeyCode>
    {
        { GameAction.Crouch, KeyCode.DownArrow },
    };

    static GameControls()
    {
        foreach (GameAction action in System.Enum.GetValues(typeof(GameAction)))
        {
            string saveKey = "key_" + action;
            if (PlayerPrefs.HasKey(saveKey))
            {
                primary[action] = (KeyCode)PlayerPrefs.GetInt(saveKey);
                alternate.Remove(action);
            }
        }
    }

    public static bool Pressed(GameAction action)
    {
        return Input.GetKeyDown(primary[action]) ||
               (alternate.TryGetValue(action, out KeyCode alt) && Input.GetKeyDown(alt));
    }

    public static bool Held(GameAction action)
    {
        return Input.GetKey(primary[action]) ||
               (alternate.TryGetValue(action, out KeyCode alt) && Input.GetKey(alt));
    }

    public static KeyCode GetKey(GameAction action)
    {
        return primary[action];
    }

    public static void Rebind(GameAction action, KeyCode newKey)
    {
        primary[action] = newKey;
        alternate.Remove(action);
        PlayerPrefs.SetInt("key_" + action, (int)newKey);
        PlayerPrefs.Save();
    }
}