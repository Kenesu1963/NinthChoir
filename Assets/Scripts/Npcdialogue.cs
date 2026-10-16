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

    public override void Interact()
    {
        DialogueManager.Instance.ShowConversation(npcName, nodes);
    }
}