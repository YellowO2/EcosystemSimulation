using UnityEngine;

public class Plankton : MonoBehaviour
{
    public static int maxPopulation = 50;
    public static int currentPopulation = 0;

    [Header("Lifecycle")]
    private float lifetime = 20f;
    private float reproductionTime = 10f;

    [Header("Movement")]
    private float moveForce = 1f;
    private float moveInterval = 3f;

    [Header("Spawning")]
    private float spawnRadius = 0.5f;
    private float planktonSize = 0.5f;
    public LayerMask solidLayer;
    
    private Rigidbody2D rb;
    private float moveTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (gameObject.layer != 0)
        {
            Physics2D.IgnoreLayerCollision(gameObject.layer, gameObject.layer);
        }
    }

    void Start()
    {
        currentPopulation++;
        reproductionTime = Random.Range(0.5f, 1f) * reproductionTime;
        lifetime = Random.Range(0.5f, 1f) * lifetime;
        
        Invoke(nameof(DieOfOldAge), lifetime);
        Invoke(nameof(Reproduce), reproductionTime);
    }

    void FixedUpdate()
    {
        moveTimer += Time.fixedDeltaTime;
        if (moveTimer >= moveInterval)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            rb.AddForce(randomDirection * moveForce, ForceMode2D.Impulse);
            moveTimer = 0f;
        }
    }

    private void Reproduce()
    {
        if (currentPopulation >= maxPopulation)
        {
            Invoke(nameof(Reproduce), reproductionTime);
            return;
        }

        int maxTries = 10;
        for (int i = 0; i < maxTries; i++)
        {
            Vector2 spawnPos = (Vector2)transform.position + (Random.insideUnitCircle * spawnRadius);
            Collider2D hit = Physics2D.OverlapCircle(spawnPos, planktonSize, solidLayer);

            if (hit == null)
            {
                Instantiate(gameObject, spawnPos, Quaternion.identity);
                break;
            }
        }
        
        Invoke(nameof(Reproduce), reproductionTime);
    }

    private void DieOfOldAge()
    {
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        currentPopulation--;
    }
}