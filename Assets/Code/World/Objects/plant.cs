using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))] // Good practice to ensure it has a sprite
public class Plant : MonoBehaviour
{
    [Header("Growth")]
    public float timeToMature = 20f;
    private float growthProgress = 0f;
    private Vector3 initialScale;

    // --- NEW: Visuals ---
    private SpriteRenderer spriteRenderer;
    public Color sproutColor = Color.yellow;      // Color when growing
    public Color matureColor = Color.green;     // Color when harvestable

    [Header("State")]
    private bool isHarvestable = false;
    private Collider2D col; 

    [Header("Food Value")]
    public float maxFoodValue = 50f;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // --- NEW ---
        initialScale = transform.localScale;
        
        // Start as a small, non-interactable sprout
        ResetToSprout(); // --- NEW: Centralized reset logic ---
    }

    void Update()
    {
        if (!isHarvestable)
        {
            growthProgress += Time.deltaTime / timeToMature;
            // --- NEW: Smoothly blend color as it grows ---
            spriteRenderer.color = Color.Lerp(sproutColor, matureColor, growthProgress);
            transform.localScale = Vector3.Lerp(initialScale * 0.1f, initialScale, growthProgress);

            if (growthProgress >= 1f)
            {
                // Become mature
                growthProgress = 1f;
                isHarvestable = true;
                col.enabled = true;
                spriteRenderer.color = matureColor; // Ensure it's the final color
            }
        }
    }

    public float BeEaten()
    {
        if (!isHarvestable)
        {
            return 0f;
        }

        ResetToSprout(); // --- NEW: Use the reset function ---
        
        return maxFoodValue;
    }

    // --- NEW: A helper function to keep our code clean ---
    private void ResetToSprout()
    {
        isHarvestable = false;
        growthProgress = 0f;
        transform.localScale = initialScale * 0.1f;
        col.enabled = false;
        spriteRenderer.color = sproutColor; // Set to sprout color
    }
}