using UnityEngine;

/// <summary>A readable sign. Write the text for each page in the Inspector.</summary>
public class Sign : Interactable
{
    [Header("Sign")]
    [Tooltip("Optional heading shown above the text (e.g. 'Warning'). Leave empty for none.")]
    [SerializeField] private string title = "";

    [Tooltip("Each element is one page. Press the action key to go to the next page.")]
    [TextArea(2, 6)]
    [SerializeField] private string[] pages = { "Write your sign text here." };

    private void Reset()
    {
        prompt = "Read";
    }

    public override void Interact()
    {
        DialogueManager.Instance.ShowSign(title, pages);
    }
}