using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Character info: name, health, Divine Energy (DE, the mana for incantations) and money,
/// plus temporary effects from food and potions (regeneration, strength).
/// Put this on the Player. Change the starting values in the Inspector.
/// </summary>
public class PlayerStats : MonoBehaviour, IDamageable
{
    [Header("Identity")]
    [SerializeField] private string characterName = "Maor";
    [SerializeField] private string currencyName = "Aureus";

    [Header("Health")]
    [SerializeField] private int maxHealth = 10;

    [Header("Divine Energy (DE)")]
    [SerializeField] private int maxDivineEnergy = 10;
    [Tooltip("DE regained per second. Placeholder so you never get stuck while testing. Set to 0 to turn it off.")]
    [SerializeField] private float divineEnergyRegenPerSecond = 0.5f;

    [Header("Money")]
    [SerializeField] private int startingMoney = 5;

    [Header("Breathing (under water)")]
    [Tooltip("Seconds the player can stay under water.")]
    [SerializeField] private float maxBreathSeconds = 10f;
    [Tooltip("How fast the air comes back once the player can breathe.")]
    [SerializeField] private float breathRefillPerSecond = 8f;
    [Tooltip("Damage taken each time the player is drowning (no air left).")]
    [SerializeField] private int drownDamage = 1;
    [SerializeField] private float drownIntervalSeconds = 1.5f;

    [Header("Paralysis (bubbles)")]
    [Tooltip("After being paralyzed, bubbles can't paralyze the player again for this long (they still restore air).")]
    [SerializeField] private float paralysisImmunitySeconds = 0.75f;

    [Header("Taking damage")]
    [Tooltip("Seconds the player can't be hurt after taking a hit. Stops touching an enemy from hurting every frame.")]
    [SerializeField] private float invulnerabilitySeconds = 1f;
    [SerializeField] private bool blinkWhileInvulnerable = true;
    [Tooltip("Shove opposite to the way the player was moving.")]
    [SerializeField] private float knockbackHorizontal = 7f;
    [Tooltip("Small hop upward.")]
    [SerializeField] private float knockbackVertical = 6f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("Display")]
    [SerializeField] private bool showHUD = true;

    private class Regeneration
    {
        public int amount;
        public float interval;
        public float timer;
        public int ticksLeft;
    }

    private int health;
    private float divineEnergy;
    private int money;

    private readonly List<Regeneration> regenerations = new List<Regeneration>();
    private float strengthMultiplier = 1f;
    private float strengthTimer;

    private float invulnerableTimer;
    private PlayerController controller;
    private SpriteRenderer bodySprite;

    private float breath;
    private bool canBreathe = true;
    private float drownTimer;

    private float paralyzeTimer;
    private float paralysisImmuneTimer;
    private Color bodyOriginalColor = Color.white;

    public string CharacterName => characterName;
    public string CurrencyName => currencyName;
    public int Health => health;
    public int MaxHealth => maxHealth;
    public int DivineEnergy => Mathf.FloorToInt(divineEnergy);
    public int MaxDivineEnergy => maxDivineEnergy;
    public int Money => money;
    public bool IsDead => health <= 0;
    public bool IsInvulnerable => invulnerableTimer > 0f;
    public bool IsParalyzed => paralyzeTimer > 0f;

    public float Breath => breath;
    public float MaxBreath => maxBreathSeconds;
    /// <summary>True when the air is not full (the HUD only shows the air meter then).</summary>
    public bool ShowAirMeter => breath < maxBreathSeconds - 0.01f;

    /// <summary>Multiply any damage you deal by this (1 normally, higher while a strength potion lasts).</summary>
    public float DamageMultiplier => strengthTimer > 0f ? strengthMultiplier : 1f;

    /// <summary>Text for the HUD, e.g. "Regeneration   Strength x1.5". Empty when nothing is active.</summary>
    public string StatusText
    {
        get
        {
            string text = "";
            if (regenerations.Count > 0) text += "Regeneration";
            if (strengthTimer > 0f) text += (text.Length > 0 ? "   " : "") + "Strength x" + strengthMultiplier.ToString("0.#");
            if (paralyzeTimer > 0f) text += (text.Length > 0 ? "   " : "") + "Paralyzed";
            return text;
        }
    }

    public static PlayerStats Instance { get; private set; }

    /// <summary>Fired whenever health, DE, money or an effect changes.</summary>
    public event Action Changed;

    /// <summary>Fired when the player takes a hit and survives it.</summary>
    public event Action Damaged;
    public event Action Died;

    private void Awake()
    {
        Instance = this;
        controller = GetComponent<PlayerController>();
        bodySprite = GetComponentInChildren<SpriteRenderer>();
        if (bodySprite != null) bodyOriginalColor = bodySprite.color;
        health = maxHealth;
        divineEnergy = maxDivineEnergy;
        money = startingMoney;
        breath = maxBreathSeconds;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (showHUD) StatsHUD.Create(this);
        RespawnManager.EnsureExists(); // remembers where the level starts
        DeathScreen.Create(this);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return; // paused

        UpdateEffects(dt);
        UpdateInvulnerability(dt);
        UpdateBreath(dt);
        UpdateParalysis(dt);

        if (IsDead || divineEnergyRegenPerSecond <= 0f || divineEnergy >= maxDivineEnergy) return;

        int before = Mathf.FloorToInt(divineEnergy);
        divineEnergy = Mathf.Min(maxDivineEnergy, divineEnergy + divineEnergyRegenPerSecond * dt);
        if (Mathf.FloorToInt(divineEnergy) != before) Changed?.Invoke();
    }

    /// <summary>Fills the air meter completely (a bubble was touched).</summary>
    public void RefillBreath()
    {
        breath = maxBreathSeconds;
        drownTimer = 0f;
        Changed?.Invoke();
    }

    /// <summary>
    /// Freezes the player for a moment: no moving, no items. Returns false if they're already paralyzed
    /// or only just recovered (so a stream of bubbles can't lock the player in place).
    /// </summary>
    public bool Paralyze(float seconds)
    {
        if (IsDead || seconds <= 0f || paralyzeTimer > 0f || paralysisImmuneTimer > 0f) return false;

        paralyzeTimer = seconds;
        if (bodySprite != null) bodySprite.color = Color.Lerp(bodyOriginalColor, new Color(0.5f, 0.9f, 1f, bodyOriginalColor.a), 0.6f);
        if (controller != null) controller.Paralyze();
        Changed?.Invoke();
        return true;
    }

    private void UpdateParalysis(float dt)
    {
        if (paralysisImmuneTimer > 0f) paralysisImmuneTimer -= dt;
        if (paralyzeTimer <= 0f) return;

        paralyzeTimer -= dt;
        if (paralyzeTimer > 0f) return;

        // Paralysis is over
        paralysisImmuneTimer = paralysisImmunitySeconds;
        if (bodySprite != null) bodySprite.color = bodyOriginalColor;
        Changed?.Invoke();
    }

    private void ClearParalysis()
    {
        paralyzeTimer = 0f;
        paralysisImmuneTimer = 0f;
        if (bodySprite != null) bodySprite.color = bodyOriginalColor;
    }

    /// <summary>Called by the player controller every frame: false while the head is under water.</summary>
    public void SetCanBreathe(bool value)
    {
        canBreathe = value;
    }

    // Air runs out under water. With no air left, the player takes drowning damage.
    private void UpdateBreath(float dt)
    {
        if (IsDead) return;

        float before = breath;

        if (canBreathe)
        {
            breath = Mathf.Min(maxBreathSeconds, breath + breathRefillPerSecond * dt);
            drownTimer = 0f;
        }
        else
        {
            breath = Mathf.Max(0f, breath - dt);
            if (breath <= 0f)
            {
                drownTimer -= dt;
                if (drownTimer <= 0f)
                {
                    drownTimer = drownIntervalSeconds;
                    TakeEnvironmentalDamage(drownDamage);
                }
            }
        }

        // Tell the HUD about once per whole second
        if (Mathf.CeilToInt(breath) != Mathf.CeilToInt(before)) Changed?.Invoke();
    }

    // After a hit the player is briefly untouchable and blinks
    private void UpdateInvulnerability(float dt)
    {
        if (invulnerableTimer <= 0f) return;

        invulnerableTimer -= dt;
        if (bodySprite != null)
        {
            bool visible = invulnerableTimer <= 0f || !blinkWhileInvulnerable ||
                           Mathf.FloorToInt(invulnerableTimer / 0.08f) % 2 == 0;
            bodySprite.enabled = visible;
        }
    }

    private void UpdateEffects(float dt)
    {
        bool statusChanged = false;

        for (int i = regenerations.Count - 1; i >= 0; i--)
        {
            Regeneration r = regenerations[i];
            r.timer -= dt;
            if (r.timer > 0f) continue;

            r.timer += r.interval;
            r.ticksLeft--;
            Heal(r.amount);

            if (r.ticksLeft <= 0)
            {
                regenerations.RemoveAt(i);
                statusChanged = true;
            }
        }

        if (strengthTimer > 0f)
        {
            strengthTimer -= dt;
            if (strengthTimer <= 0f) statusChanged = true;
        }

        if (statusChanged) Changed?.Invoke();
    }

    // ---------------- Health ----------------

    /// <summary>
    /// Damage from any source (enemy touch, projectile, trap). Ignored while invulnerable.
    /// hitDirection is the way the hit was traveling (from the source toward the player).
    /// </summary>
    public void TakeDamage(int amount, Vector2 hitDirection)
    {
        if (IsDead || amount <= 0 || IsInvulnerable) return;

        health = Mathf.Max(0, health - amount);
        Changed?.Invoke();

        if (IsDead)
        {
            Debug.Log(characterName + " has fallen.");
            Died?.Invoke();
            return;
        }

        invulnerableTimer = invulnerabilitySeconds;
        if (controller != null)
        {
            controller.Hurt(hitDirection, knockbackHorizontal, knockbackVertical, knockbackDuration);
        }
        Damaged?.Invoke();
    }

    /// <summary>Damage that ignores invulnerability and doesn't push the player (drowning, poison...).</summary>
    public void TakeEnvironmentalDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        health = Mathf.Max(0, health - amount);
        Changed?.Invoke();

        if (IsDead)
        {
            Debug.Log(characterName + " has fallen.");
            Died?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        health = Mathf.Min(maxHealth, health + amount);
        Changed?.Invoke();
    }

    /// <summary>Instant death (death planes, traps). Does nothing if already dead.</summary>
    public void Kill()
    {
        if (IsDead) return;

        health = 0;
        ClearParalysis();
        invulnerableTimer = 0f;
        if (bodySprite != null) bodySprite.enabled = true;
        regenerations.Clear();
        strengthTimer = 0f;
        Changed?.Invoke();

        Debug.Log(characterName + " has fallen.");
        Died?.Invoke();
    }

    /// <summary>Brings the player back with full health and Divine Energy (money and items are kept).</summary>
    public void Revive()
    {
        health = maxHealth;
        divineEnergy = maxDivineEnergy;
        breath = maxBreathSeconds;
        drownTimer = 0f;
        ClearParalysis();
        invulnerableTimer = 0f;
        if (bodySprite != null) bodySprite.enabled = true;
        regenerations.Clear();
        strengthTimer = 0f;
        Changed?.Invoke();
    }

    /// <summary>The exact Divine Energy, with its fraction. Used when carrying the player to another scene.</summary>
    public float DivineEnergyExact => divineEnergy;

    /// <summary>Sets health, Divine Energy and money directly (used when arriving in a new scene).</summary>
    public void SetState(int newHealth, float newDivineEnergy, int newMoney)
    {
        health = Mathf.Clamp(newHealth, 1, maxHealth);
        divineEnergy = Mathf.Clamp(newDivineEnergy, 0f, maxDivineEnergy);
        money = Mathf.Max(0, newMoney);
        Changed?.Invoke();
    }

    // ---------------- Temporary effects ----------------

    /// <summary>Heals "amount" HP every "interval" seconds, "ticks" times. Several can run at once.</summary>
    public void StartRegeneration(int amount, float interval, int ticks)
    {
        if (IsDead || amount <= 0 || ticks <= 0) return;

        regenerations.Add(new Regeneration
        {
            amount = amount,
            interval = Mathf.Max(0.1f, interval),
            timer = Mathf.Max(0.1f, interval),
            ticksLeft = ticks
        });
        Changed?.Invoke();
    }

    /// <summary>Multiplies the damage you deal for "duration" seconds. A new potion replaces the old one.</summary>
    public void ApplyStrength(float multiplier, float duration)
    {
        if (IsDead || duration <= 0f) return;
        strengthMultiplier = Mathf.Max(1f, multiplier);
        strengthTimer = duration;
        Changed?.Invoke();
    }

    // ---------------- Divine Energy ----------------

    public bool TrySpendDivineEnergy(int cost)
    {
        if (cost <= 0) return true;
        if (divineEnergy < cost) return false;

        divineEnergy -= cost;
        Changed?.Invoke();
        return true;
    }

    public void RestoreDivineEnergy(int amount)
    {
        if (amount <= 0) return;
        divineEnergy = Mathf.Min(maxDivineEnergy, divineEnergy + amount);
        Changed?.Invoke();
    }

    // ---------------- Money ----------------

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        money += amount;
        Changed?.Invoke();
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount < 0 || money < amount) return false;
        money -= amount;
        Changed?.Invoke();
        return true;
    }

    // ---------------- Testing: use the three-dot menu of this component while in Play mode ----------------

    [ContextMenu("Test: take 1 damage")]
    private void TestDamage() { TakeDamage(1, Vector2.zero); }

    [ContextMenu("Test: die")]
    private void TestDie() { Kill(); }

    [ContextMenu("Test: heal 1")]
    private void TestHeal() { Heal(1); }

    [ContextMenu("Test: spend 1 DE")]
    private void TestSpendDE() { TrySpendDivineEnergy(1); }

    [ContextMenu("Test: add 5 coins")]
    private void TestAddMoney() { AddMoney(5); }

    [ContextMenu("Test: regeneration")]
    private void TestRegen() { StartRegeneration(1, 1f, 3); }

    [ContextMenu("Test: strength")]
    private void TestStrength() { ApplyStrength(1.5f, 10f); }
}