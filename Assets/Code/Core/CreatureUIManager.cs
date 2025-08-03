using UnityEngine;

public class CreatureUIManager : MonoBehaviour
{
    // No longer needs a public static instance.
    private Creature currentlySelected; // This manager's private reference.

    // A new public method for the Controller to call.
    public void SelectCreature(Creature creature)
    {
        this.currentlySelected = creature;
    }

    // Deselects the creature if we click elsewhere.
    public void Deselect()
    {
        this.currentlySelected = null;
    }

    void OnGUI()
    {
        if (currentlySelected == null)
        {
            return; // No creature selected, do nothing.
        }

        // A creature is selected. Get its brain activity.
       float[] inputs = currentlySelected.GetLastInputs();
        float[] outputs = currentlySelected.GetLastOutputs();

        if (inputs == null || outputs == null) return;

        GUI.color = Color.white;
        GUI.Box(new Rect(5, 5, 220, 30 + (inputs.Length * 20)), ""); // Background box
        GUI.Label(new Rect(10, 10, 100, 20), "INPUTS");
        GUI.Label(new Rect(115, 10, 100, 20), "OUTPUTS");

        for (int i = 0; i < inputs.Length; i++)
        {
            GUI.color = Color.Lerp(Color.red, Color.green, (inputs[i] + 1f) / 2f);
            GUI.Box(new Rect(10, 30 + (i * 20), 100, 18), inputs[i].ToString("F2"));
        }

        for (int i = 0; i < outputs.Length; i++)
        {
            GUI.color = Color.Lerp(Color.red, Color.green, (outputs[i] + 1f) / 2f);
            GUI.Box(new Rect(115, 30 + (i * 20), 100, 18), outputs[i].ToString("F2"));
        }
    }
}