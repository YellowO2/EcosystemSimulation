// StatusBar.cs
using UnityEngine;
using UnityEngine.UI; // <-- Don't forget this! You need it for UI components like Image.

public class StatusBar : MonoBehaviour
{
    // Drag the Bar_Fill object's Image component here in the Inspector
    public Image barFill; 

    public void UpdateBar(float currentValue, float maxValue)
    {
        // Clamp the values to ensure they are valid
        currentValue = Mathf.Clamp(currentValue, 0f, maxValue);

        // The fillAmount property is a value between 0 and 1, perfect for a percentage.
        barFill.fillAmount = currentValue / maxValue;
    }
}