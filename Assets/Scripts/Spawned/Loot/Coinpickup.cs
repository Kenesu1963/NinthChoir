using UnityEngine;

/// <summary>A coin that jumps out of a chest, spins, lands, and flies to the player when they get close.</summary>
public class CoinPickup : MonoBehaviour
{
    private const float PickupDelay = 0.45f;   // can't be collected right as it leaves the chest
    private const float CollectDistance = 0.7f;
    private const float MagnetDistance = 2.6f;
    private const float MagnetSpeed = 11f;
    private const float BaseSize = 0.3f;

    private int value;
    private LootBody body;
    private float age;

    public static CoinPickup Spawn(Vector2 position, int value, Vector2 launchVelocity, Transform ignoreRoot)
    {
        GameObject go = new GameObject("Coin");
        go.transform.position = position;
        go.transform.localScale = new Vector3(BaseSize, BaseSize, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = DebugVisuals.White;
        sr.color = new Color(1f, 0.82f, 0.2f, 1f);
        sr.sortingOrder = 15;

        LootBody body = go.AddComponent<LootBody>();
        body.ignoreRoot = ignoreRoot;
        body.radius = BaseSize * 0.5f;
        body.bounciness = 0.55f;

        CoinPickup coin = go.AddComponent<CoinPickup>();
        coin.value = value;
        coin.body = body;

        body.Launch(launchVelocity);
        return coin;
    }

    private void Update()
    {
        age += Time.deltaTime;

        // Spin while flying
        float width = body.IsLanded ? BaseSize : BaseSize * Mathf.Abs(Mathf.Cos(age * 10f)) + 0.04f;
        transform.localScale = new Vector3(width, BaseSize, 1f);

        PlayerStats player = PlayerStats.Instance;
        if (player == null || age < PickupDelay) return;

        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        float distance = toPlayer.magnitude;

        if (distance <= CollectDistance)
        {
            player.AddMoney(value);
            Destroy(gameObject);
            return;
        }

        if (body.IsLanded && distance <= MagnetDistance)
        {
            transform.position += (Vector3)(toPlayer.normalized * MagnetSpeed * Time.deltaTime);
        }
    }
}