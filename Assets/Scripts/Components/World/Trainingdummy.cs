using UnityEngine;

/// <summary>Test target: flashes red, gets knocked back a little and prints a message when hit.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class TrainingDummy : MonoBehaviour, IDamageable
{
    [SerializeField] private int health = 5;
    [SerializeField] private float knockbackDistance = 0.3f;  // how far it gets pushed when hit
    [SerializeField] private float knockbackRecoverSpeed = 3f; // how fast it slides back

    private SpriteRenderer sr;
    private Color originalColor;
    private float flashTimer;
    private Vector3 restPosition;
    private Vector2 knockOffset;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
        restPosition = transform.position;
    }

    public void TakeDamage(int amount, Vector2 hitDirection)
    {
        health -= amount;
        flashTimer = 0.12f;
        knockOffset = new Vector2(hitDirection.x, 0f).normalized * knockbackDistance;
        Debug.Log(name + " hit! Health left: " + health);
        if (health <= 0) Destroy(gameObject);
    }

    private void Update()
    {
        // Flash
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            sr.color = Color.red;
        }
        else
        {
            sr.color = originalColor;
        }

        // Knockback slides back to where it started
        knockOffset = Vector2.MoveTowards(knockOffset, Vector2.zero, knockbackRecoverSpeed * Time.deltaTime);
        transform.position = restPosition + (Vector3)knockOffset;
    }
}