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

    public string CharacterName => characterName;
    public string CurrencyName => currencyName;
    public int Health => health;
    public int MaxHealth => maxHealth;
    public int DivineEnergy => Mathf.FloorToInt(divineEnergy);
    public int MaxDivineEnergy => maxDivineEnergy;
    public int Money => money;
    public bool IsDead => health <= 0;

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
            return text;
        }
    }

    public static PlayerStats Instance { get; private set; }

    /// <summary>Fired whenever health, DE, money or an effect changes.</summary>
    public event Action Changed;
    public event Action Died;

    private void Awake()
    {
        Instance = this;
        health = maxHealth;
        divineEnergy = maxDivineEnergy;
        money = startingMoney;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (showHUD) StatsHUD.Create(this);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return; // paused

        UpdateEffects(dt);

        if (IsDead || divineEnergyRegenPerSecond <= 0f || divineEnergy >= maxDivineEnergy) return;

        int before = Mathf.FloorToInt(divineEnergy);
        divineEnergy = Mathf.Min(maxDivineEnergy, divineEnergy + divineEnergyRegenPerSecond * dt);
        if (Mathf.FloorToInt(divineEnergy) != before) Changed?.Invoke();
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

    public void TakeDamage(int amount, Vector2 hitDirection)
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