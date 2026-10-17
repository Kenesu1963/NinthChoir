using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A simple trade you can trigger from a normal conversation: it takes money (and/or items) and gives items.
/// Use it like this: put this on any object, then in an NPC's dialogue choice, under "On Selected",
/// press + and drag the object in, then pick  SimpleTrade -> Execute.
/// </summary>
public class SimpleTrade : MonoBehaviour
{
    [Header("The player pays")]
    [SerializeField] private int cost = 5;
    [SerializeField] private List<ItemStack> itemsTaken = new List<ItemStack>();

    [Header("The player receives")]
    [SerializeField] private List<ItemStack> itemsGiven = new List<ItemStack>();
    [SerializeField] private int moneyGiven = 0;

    [Header("Options")]
    [SerializeField] private bool onlyOnce = false;
    [Tooltip("Shown after a successful trade. Leave empty for an automatic message.")]
    [SerializeField] private string successMessage = "";

    [Header("Events (optional)")]
    [SerializeField] private UnityEvent onSuccess;
    [SerializeField] private UnityEvent onFailed;

    private bool done;

    /// <summary>Call this from a dialogue choice's "On Selected".</summary>
    public void Execute()
    {
        if (onlyOnce && done)
        {
            StatsHUD.Show("This deal is already done.");
            if (onFailed != null) onFailed.Invoke();
            return;
        }

        string failMessage;
        if (TradeUtility.TryExecute(cost, itemsTaken, moneyGiven, itemsGiven, out failMessage))
        {
            done = true;

            string currency = PlayerStats.Instance != null ? PlayerStats.Instance.CurrencyName : "Aureus";
            StatsHUD.Show(string.IsNullOrEmpty(successMessage)
                ? "Trade done: got " + TradeUtility.Describe(itemsGiven, moneyGiven, currency)
                : successMessage);

            if (onSuccess != null) onSuccess.Invoke();
        }
        else
        {
            StatsHUD.Show(failMessage);
            if (onFailed != null) onFailed.Invoke();
        }
    }
}