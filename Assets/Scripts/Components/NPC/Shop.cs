using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ShopEntry
{
    public ItemData item;
    public int price = 5;
    [Tooltip("How many are in stock. -1 = unlimited.")]
    public int stock = -1;
}

[Serializable]
public class ShopTrade
{
    [Tooltip("Short name shown in the menu. Leave empty to build one from the items.")]
    public string label;

    [Header("The player gives")]
    public int giveMoney;
    public List<ItemStack> giveItems = new List<ItemStack>();

    [Header("The player gets")]
    public int getMoney;
    public List<ItemStack> getItems = new List<ItemStack>();

    [Tooltip("How many times this trade can be done. -1 = unlimited.")]
    public int uses = -1;
}

[Serializable]
public class ShopTalkTopic
{
    public string topic;
    [TextArea(2, 5)] public string reply;
}

/// <summary>
/// Put this on an NPC (next to its NpcDialogue). Talking to the NPC opens an Undertale-style shop
/// screen with Buy, Trade, Talk and Exit. Everything it sells and says is edited in the Inspector.
/// You can also open it from a conversation: set a dialogue choice's "On Selected" to Shop.Open.
/// </summary>
public class Shop : MonoBehaviour
{
    [Header("Shopkeeper")]
    [SerializeField] private string shopName = "Shop";
    [Tooltip("The picture shown on the shop screen. Empty for now.")]
    [SerializeField] private Sprite portrait;

    [Header("What the shopkeeper says")]
    [TextArea(2, 4)] [SerializeField] private string greeting = "Welcome! Take a look around.";
    [TextArea(2, 4)] [SerializeField] private string menuLine = "Anything else?";
    [TextArea(2, 4)] [SerializeField] private string buyLine = "What would you like?";
    [TextArea(2, 4)] [SerializeField] private string tradeLine = "I'm open to a fair deal.";
    [TextArea(2, 4)] [SerializeField] private string talkLine = "Ask away.";
    [TextArea(2, 4)] [SerializeField] private string thanksLine = "Thank you! Come again.";

    [Header("Buy: what the shop sells")]
    [SerializeField] private List<ShopEntry> stock = new List<ShopEntry>();

    [Header("Trade: swap things with the shopkeeper")]
    [SerializeField] private List<ShopTrade> trades = new List<ShopTrade>();

    [Header("Talk: things you can ask about")]
    [SerializeField] private List<ShopTalkTopic> topics = new List<ShopTalkTopic>
    {
        new ShopTalkTopic { topic = "Who are you?", reply = "Just a humble merchant, travelling where the road takes me." },
        new ShopTalkTopic { topic = "Any rumors?", reply = "They say there are chests hidden in the old ruins..." },
    };

    public string ShopName => shopName;
    public Sprite Portrait => portrait;
    public string Greeting => greeting;
    public string MenuLine => menuLine;
    public string BuyLine => buyLine;
    public string TradeLine => tradeLine;
    public string TalkLine => talkLine;
    public string ThanksLine => thanksLine;
    public List<ShopEntry> Stock => stock;
    public List<ShopTrade> Trades => trades;
    public List<ShopTalkTopic> Topics => topics;

    /// <summary>Opens the shop screen.</summary>
    public void Open()
    {
        ShopMenu.Instance.Open(this);
    }
}