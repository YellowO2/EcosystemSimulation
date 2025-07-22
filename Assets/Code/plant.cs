using UnityEngine;

public class Plant : MonoBehaviour
{
    [Header("Growth")]
    public float timeToMature = 20f; // Time in seconds to reach full size
    private float growthProgress = 0f;
    private Vector3 initialScale;

    [Header("Food Value")]
    public float maxFoodValue = 50f; // Energy provided when fully grown

    void Start()
    {
        // Start as a small sprout
        initialScale = transform.localScale;
        transform.localScale = initialScale * 0.1f;
    }

    void Update()
    {
        // Grow over time until mature
        if (growthProgress < 1f)
        {
            growthProgress += Time.deltaTime / timeToMature;
            transform.localScale = Vector3.Lerp(initialScale * 0.1f, initialScale, growthProgress);
        }
    }

    // This will be called by the creature
    public float BeEaten()
    {
        Debug.Log("Plant eaten, providing food value.");
        // The food value is proportional to how much it has grown
        float currentFoodValue = maxFoodValue * growthProgress;
        Destroy(gameObject);
        return currentFoodValue;
    }
}