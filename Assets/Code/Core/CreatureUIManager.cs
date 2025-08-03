using UnityEngine;

public class CreatureUIManager : MonoBehaviour
{
    public static CreatureUIManager instance;

    void Awake()
    {
        // Ensure only one instance of this manager in the scene.
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }
    }

    void OnGUI()
    {
        if (Creature.currentlySelected == null)
        {
            return; // No creature selected, so do nothing.
        }

        // A creature is selected. Get its brain activity.
        float[] inputs = Creature.currentlySelected.GetLastInputs();
        float[] outputs = Creature.currentlySelected.GetLastOutputs();

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