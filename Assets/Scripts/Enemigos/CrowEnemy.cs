using UnityEngine;

/* CrowEnemy: minion volador del jefe que persigue y daña al jugador. */
public class CrowEnemy : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject Red;

    [Header("Stats")]
    public int Health = 1;
    public float MoveSpeed = 1.8f; // Slower speed (was 3.0f)
    public int Damage = 1;

    [Header("Visión")]
    public float VisionRange = 8.0f;       // Distance at which it can spot the player
    public LayerMask ObstacleMask;         // Layers blocking line of sight
    private bool hasSpottedPlayer = false;  // Spotted flag

    [Header("Loot Drop")]
    public GameObject healthDropPrefab;
    [Range(0f, 1f)]
    public float dropProbability = 0.4f;    // 40% chance of dropping life when killed

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private bool isDead = false;

    private void Start()
    {
        // Start: cachea componentes, ajusta escala y busca referencias por defecto
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (Red == null)
        {
            Red = GameObject.Find("Red_0");
            if (Red == null) Red = GameObject.Find("Red");
        }

        // Increase scale by 1.5x to make them bigger as requested
        transform.localScale = new Vector3(transform.localScale.x * 1.5f, transform.localScale.y * 1.5f, 1f);

        // Load health drop prefab dynamically if not assigned
#if UNITY_EDITOR
        if (healthDropPrefab == null)
        {
            healthDropPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animations/Prefabs/lifedrop_0.prefab");
        }
#endif
    }

    private bool CanSeePlayer()
    {
        // CanSeePlayer: devuelve true si el jugador está en rango y no hay obstáculos
        if (Red == null) return false;
        float dist = Vector2.Distance(transform.position, Red.transform.position);
        if (dist > VisionRange) return false;

        // If no obstacle mask set, check collision against default walls/ground
        int mask = ObstacleMask.value;
        if (mask == 0)
        {
            mask = LayerMask.GetMask("Ground", "Default");
        }

        Vector2 dir = (Red.transform.position - transform.position).normalized;

        // Raycast and look for blocking colliders (ignoring triggers)
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, dir, dist, mask);
        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger)
            {
                if (hit.collider.gameObject != Red && !hit.collider.transform.IsChildOf(Red.transform))
                {
                    // Hit a solid obstacle before the player
                    return false;
                }
            }
        }
        return true;
    }

    private void Update()
    {
        // Update: IA básica del minion (seguir al jugador si lo vio)
        if (isDead) return;

        if (Red == null)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        var playerComp = Red.GetComponent<RedMovement>();
        if (playerComp != null && !playerComp.IsAlive)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        // Follow player only when spotted
        if (!hasSpottedPlayer)
        {
            if (CanSeePlayer())
            {
                hasSpottedPlayer = true;
            }
            else
            {
                if (rb != null) rb.linearVelocity = Vector2.zero;
                return; // Wait at starting point
            }
        }

        Vector3 direction = (Red.transform.position - transform.position).normalized;
        if (rb != null)
        {
            rb.linearVelocity = direction * MoveSpeed;
        }

        // Flip sprite to face player, keeping the 1.5x bigger scale
        if (direction.x != 0f)
        {
            transform.localScale = new Vector3(direction.x >= 0f ? 1.5f : -1.5f, 1.5f, 1f);
        }
    }

    public void TakeDamage(int damage)
    {
        // TakeDamage: restar vida y comprobar muerte
        if (isDead) return;

        Health -= damage;
        if (Health <= 0)
        {
            Die();
        }
    }

    public void ApplyParryEffects(Vector2 direction, float force, float stunDuration)
    {
        // For a 1-HP minion, parrying should just kill it
        TakeDamage(1);
    }

    private void Die()
    {
        // Die: desactivar física y colisiones, soltar loot y destruir
        if (isDead) return;
        isDead = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        var colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // Spawns health pickup with probability when killed by player
        if (healthDropPrefab != null && Random.value < dropProbability)
        {
            Instantiate(healthDropPrefab, transform.position, Quaternion.identity);
            Debug.Log("CrowEnemy: Dropped life item!");
        }

        // Play standard death effects if needed, or simply destroy
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // OnTriggerEnter2D: detectar colisiones con jugador y otros objetos
        if (isDead) return;

        // Ignore collision with other enemies or boss or projectiles
        if (collision.gameObject.CompareTag("Enemy") || collision.GetComponent<CrowBoss>() != null || collision.GetComponent<CrowProjectile>() != null)
        {
            return;
        }

        // Hit player
        var player = collision.GetComponent<RedMovement>();
        if (player != null)
        {
            if (player.IsAlive)
            {
                Vector2 pushDir = (collision.transform.position - transform.position).normalized;
                player.Hit(pushDir, Damage, 1f, gameObject);
            }
            Die(); // Minion destroys itself upon hitting the player
        }
    }
}