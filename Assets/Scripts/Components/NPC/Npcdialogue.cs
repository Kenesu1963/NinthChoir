using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An NPC you can talk to. The conversation is a list of nodes you edit in the Inspector.
/// The FIRST node is where it starts. Nodes jump to each other by their Id.
/// </summary>
public class NpcDialogue : Interactable
{
    [Header("NPC")]
    [SerializeField] private string npcName = "Villager";

    [Header("Shop (optional)")]
    [Tooltip("If this object also has a Shop component, talking to the NPC opens the shop screen.")]
    [SerializeField] private Shop shop;
    [Tooltip("Untick to start the normal conversation instead. You can still open the shop from a conversation choice (On Selected -> Shop.Open).")]
    [SerializeField] private bool openShopDirectly = true;

    [SerializeField] private List<DialogueNode> nodes = new List<DialogueNode>
    {
        new DialogueNode
        {
            id = "start",
            text = "Hello, traveler! What brings you to these parts?",
            choices = new DialogueChoice[]
            {
                new DialogueChoice { text = "Just looking around.", goTo = "looking" },
                new DialogueChoice { text = "Any advice for me?",   goTo = "advice" },
                new DialogueChoice { text = "Goodbye.",             goTo = "" },
            }
        },
        new DialogueNode
        {
            id = "looking",
            text = "Take your time. The ruins to the east are worth a visit.",
            next = ""
        },
        new DialogueNode
        {
            id = "advice",
            text = "Keep moving, and don't be afraid to dash through danger.",
            next = "start"
        },
    };

    private void Reset()
    {
        prompt = "Talk";
    }

    private void Awake()
    {
        if (shop == null) shop = GetComponent<Shop>();
    }

    public override void Interact()
    {
        if (shop != null && openShopDirectly)
        {
            shop.Open();
            return;
        }

        DialogueManager.Instance.ShowConversation(npcName, nodes);
    }
}