using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Plant : WorldObject 
{
    [Header("Growth")]
    public float timeToMature = 20f;
    private float growthProgress = 0f;
    private Vector3 initialScale;

    [Header("Visuals")]
    private SpriteRenderer spriteRenderer;
    public Color sproutColor = Color.yellow;
    public Color matureColor = Color.green;

    [Header("State")]
    private bool isHarvestable = false;
    private Collider2D col; 

    [Header("Food Value")]
    public float maxFoodValue = 50f;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;
        
        // NEW: Initialize the plant's state based on how long it's existed
        InitializeState();
    }

    // NEW: This function sets the plant's initial state when it is created/loaded
    private void InitializeState()
    {
        // If the object has just been placed, timeSinceCreation will be 0.
        // If it's being loaded, it will have a value from the save file.
        
        // Prevent division by zero
        if (timeToMature <= 0) timeToMature = 0.01f;

        // Convert the saved time into a 0-1 progress value
        growthProgress = timeSinceCreation / timeToMature;

        // Check if the plant should already be mature
        if (growthProgress >= 1f)
        {
            BecomeMature();
        }
        else
        {
            // Set visuals based on initial progress
            spriteRenderer.color = Color.Lerp(sproutColor, matureColor, growthProgress);
            transform.localScale = Vector3.Lerp(initialScale * 0.1f, initialScale, growthProgress);
            isHarvestable = false;
            col.enabled = false;
        }
    }

    void Update()
    {
        if (!isHarvestable)
        {
            // Keep track of total time for saving purposes
            timeSinceCreation += Time.deltaTime;
            
            // Recalculate progress based on the updated time
            growthProgress = timeSinceCreation / timeToMature;
            
            // Update visuals
            spriteRenderer.color = Color.Lerp(sproutColor, matureColor, growthProgress);
            transform.localScale = Vector3.Lerp(initialScale * 0.1f, initialScale, growthProgress);

            if (growthProgress >= 1f)
            {
                BecomeMature();
            }
        }
    }

    public float BeEaten()
    {
        if (!isHarvestable) return 0f;
        
        float foodValue = maxFoodValue;
        ResetToSprout(); // Reset the plant
        return foodValue;
    }

    private void ResetToSprout()
    {
        timeSinceCreation = 0f; // MODIFIED: Reset the timer!
        growthProgress = 0f;
        
        isHarvestable = false;
        transform.localScale = initialScale * 0.1f;
        col.enabled = false;
        spriteRenderer.color = sproutColor;
    }

    private void BecomeMature() //to finalize the growth
    {
        growthProgress = 1f;
        timeSinceCreation = timeToMature; // Clamp the time so it doesn't grow forever
        isHarvestable = true;
        col.enabled = true;
        spriteRenderer.color = matureColor;
        transform.localScale = initialScale;
    }
}