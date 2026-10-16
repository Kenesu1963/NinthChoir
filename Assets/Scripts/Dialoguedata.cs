using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>One answer option the player can pick during a conversation.</summary>
[Serializable]
public class DialogueChoice
{
    public string text;

    [Tooltip("Id of the node to jump to when this answer is picked. Leave empty to end the conversation.")]
    public string goTo;

    [Tooltip("Optional: things to trigger when this answer is picked (open a door, give an item...).")]
    public UnityEvent onSelected;
}

/// <summary>One line of an NPC conversation.</summary>
[Serializable]
public class DialogueNode
{
    [Tooltip("The name other nodes use to jump here. The FIRST node in the list is always where the conversation starts.")]
    public string id;

    [Tooltip("Leave empty to use the NPC's name.")]
    public string speaker;

    [TextArea(2, 6)]
    public string text;

    [Tooltip("Answer options. Leave empty for a normal line with no choices.")]
    public DialogueChoice[] choices;

    [Tooltip("Only for lines WITHOUT choices: id of the next node. Leave empty to end the conversation.")]
    public string next;
}