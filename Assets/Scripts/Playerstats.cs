using System;
using UnityEngine;

/// <summary>
/// Character info: name, health, Divine Energy (DE, the mana for incantations) and money.
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

    private int health;
    private float divineEnergy;
    private int money;

    public string CharacterName => characterName;
    public string CurrencyName => currencyName;
    public int Health => health;
    public int MaxHealth => maxHealth;
    public int DivineEnergy => Mathf.FloorToInt(divineEnergy);
    public int MaxDivineEnergy => maxDivineEnergy;
    public int Money => money;
    public bool IsDead => health <= 0;

    public static PlayerStats Instance { get; private set; }

    /// <summary>Fired whenever health, DE or money changes.</summary>
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
        if (IsDead || divineEnergyRegenPerSecond <= 0f || divineEnergy >= maxDivineEnergy) return;

        int before = Mathf.FloorToInt(divineEnergy);
        divineEnergy = Mathf.Min(maxDivineEnergy, divineEnergy + divineEnergyRegenPerSecond * Time.deltaTime);
        if (Mathf.FloorToInt(divineEnergy) != before) Changed?.Invoke();
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

    [ContextMenu("Test: add 1 coin")]
    private void TestAddMoney() { AddMoney(1); }
}