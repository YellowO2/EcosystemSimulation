// Predator.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Predator : MonoBehaviour
{
    public float speed = 2f;
    private Rigidbody2D rb;
    private Vector2 moveDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        SetNewRandomDirection(1); // Start by moving right
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveDirection * speed;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // Reverse horizontal direction and pick a new random vertical component
            SetNewRandomDirection(-Mathf.Sign(moveDirection.x));
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Creature"))
        {
            Creature creature = other.GetComponent<Creature>();
            if (creature != null)
            {
                creature.Die(); // Directly call the public Die() method
            }
        }
    }

    private void SetNewRandomDirection(float horizontalSign)
    {
        float randomY = Random.Range(-0.4f, 0.4f);
        moveDirection = new Vector2(horizontalSign, randomY).normalized;
        
        // Point the sprite in the direction of movement
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}