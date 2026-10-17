using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>An item and how many of it.</summary>
[Serializable]
public class ItemStack
{
    public ItemData item;
    [Min(1)] public int amount = 1;

    public ItemStack() { }

    public ItemStack(ItemData item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
}

/// <summary>The one place where things are paid for: shops, trades and dialogue trades all use it.</summary>
public static class TradeUtility
{
    /// <summary>
    /// The player gives money and/or items and receives money and/or items.
    /// Nothing happens unless the whole trade is possible. On failure, message says why.
    /// </summary>
    public static bool TryExecute(int giveMoney, List<ItemStack> give, int getMoney, List<ItemStack> get, out string message)
    {
        message = "";

        PlayerStats stats = PlayerStats.Instance;
        PlayerInventory inventory = PlayerInventory.Instance;
        if (stats == null || inventory == null)
        {
            message = "Something went wrong.";
            return false;
        }

        if (stats.Money < giveMoney)
        {
            message = "You don't have enough " + stats.CurrencyName + ".";
            return false;
        }

        if (give != null)
        {
            foreach (ItemStack stack in give)
            {
                if (stack == null || stack.item == null) continue;
                if (inventory.GetCount(stack.item) < stack.amount)
                {
                    message = "You don't have " + (stack.amount > 1 ? stack.amount + " " : "") + stack.item.itemName + ".";
                    return false;
                }
            }
        }

        if (!inventory.CanApply(give, get))
        {
            message = "You can't carry that: your inventory is full.";
            return false;
        }

        // Everything checks out: do it
        if (giveMoney > 0) stats.TrySpendMoney(giveMoney);

        if (give != null)
        {
            foreach (ItemStack stack in give)
            {
                if (stack != null && stack.item != null) inventory.Remove(stack.item, stack.amount);
            }
        }

        if (get != null)
        {
            foreach (ItemStack stack in get)
            {
                if (stack != null && stack.item != null) inventory.Add(stack.item, stack.amount);
            }
        }

        if (getMoney > 0) stats.AddMoney(getMoney);
        return true;
    }

    /// <summary>"Bread x2 + 3 Aureus", or "nothing".</summary>
    public static string Describe(List<ItemStack> items, int money, string currency)
    {
        List<string> parts = new List<string>();

        if (items != null)
        {
            foreach (ItemStack stack in items)
            {
                if (stack == null || stack.item == null) continue;
                parts.Add(stack.amount > 1 ? stack.item.itemName + " x" + stack.amount : stack.item.itemName);
            }
        }
        if (money > 0) parts.Add(money + " " + currency);

        return parts.Count > 0 ? string.Join(" + ", parts) : "nothing";
    }
}