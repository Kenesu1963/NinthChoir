using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds the menu  Items -> Create Default Item Assets.
/// It creates an asset for every item in Assets/Items, so you can drag them into chests,
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

        Create<MeleeWeaponItem>(folder, "Rapier");
        Create<SaberItem>(folder, "Saber");
        Create<ShieldToolItem>(folder, "Shield");
        Create<PickToolItem>(folder, "Pick");
        Create<ProjectileIncantationItem>(folder, "DivineBolt");
        Create<LightBeamIncantationItem>(folder, "LightBeam");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Item assets are in " + folder);
    }

    private static void Create<T>(string folder, string fileName) where T : ItemData
    {
        string path = folder + "/" + fileName + ".asset";
        if (AssetDatabase.LoadAssetAtPath<T>(path) != null) return;

        T item = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(item, path);
    }
}