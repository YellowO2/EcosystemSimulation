// StatusBar.cs
using UnityEngine;

public class StatusBar : MonoBehaviour
{
    public Transform barFill; // Drag the Bar_Fill object here in the Inspector

    public void UpdateBar(float currentValue, float maxValue)
    {
        float fillPercent = currentValue / maxValue;
        barFill.localScale = new Vector3(fillPercent, 1f, 1f);
    }
}