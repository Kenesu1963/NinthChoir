using UnityEngine;

/// <summary>A glowing diamond on the player that grows while a spell charges and pulses when it's ready.</summary>
public class ChargeGlow : MonoBehaviour
{
    private Transform owner;
    private Vector2 offset;
    private SpriteRenderer sr;
    private float fraction;
    private bool full;

    public static ChargeGlow Create(Transform owner, Vector2 offset)
    {
        GameObject go = new GameObject("ChargeGlow");
        ChargeGlow glow = go.AddComponent<ChargeGlow>();
        glow.owner = owner;
        glow.offset = offset;
        glow.sr = go.AddComponent<SpriteRenderer>();
        glow.sr.sprite = DebugVisuals.White;
        glow.sr.sortingOrder = 25;
        glow.Apply();
        return glow;
    }

    public void SetOrigin(Vector2 newOffset) { offset = newOffset; }

    public void SetCharge(float newFraction, bool isFull)
    {
        fraction = newFraction;
        full = isFull;
    }

    private void Update()
    {
        if (owner == null) { Destroy(gameObject); return; }
        Apply();
    }

    private void Apply()
    {
        if (owner == null) return;

        transform.position = (Vector2)owner.position + offset;
        transform.rotation = Quaternion.Euler(0f, 0f, 45f);

        float size = Mathf.Lerp(0.12f, 0.6f, fraction);
        if (full) size += 0.06f * Mathf.Sin(Time.time * 25f);
        transform.localScale = new Vector3(size, size, 1f);

        Color dim = new Color(1f, 0.85f, 0.4f, 0.55f);
        Color bright = new Color(1f, 1f, 0.85f, 1f);
        sr.color = full ? Color.Lerp(dim, bright, 0.5f + 0.5f * Mathf.Sin(Time.time * 25f))
                        : Color.Lerp(dim, bright, fraction * 0.6f);
    }
}