using System.Collections;
/* GruntEnemy: soldado enemigo que dispara ráfagas y salta para esquivar obstáculos. */
using UnityEngine;

public class GruntEnemy : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject Red;
    public GameObject BulletPrefab;

    [Header("Stats")]
    public int Health = 3;
    public float MoveSpeed = 1.5f;
    public float BulletSpeed = 10f;
    public float FireCooldown = 3.0f;
    public float VisionRange = 12.0f;

    [Header("Burst Shooting")]
    public int BurstCount = 3;
    public float TimeBetweenBurstShots = 0.4f;

    [Header("Visual Scale")]
    public float scaleMultiplier = 0.65f; // Escala visual para ajustar al tamaño del goblin

    [Header("Salto")]
    public float JumpForce = 5.5f;
    public float JumpCooldown = 3.0f;
    public float JumpHeightThreshold = 0.5f;
    private float lastJumpTime = -100f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Collider2D cachedCollider;
    private bool isDead = false;
    private float nextFireTime = 0f;
    private bool isShooting = false;

    private void Start()
    {
        // Start: cachea componentes, configura físicas y referencia al jugador
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        cachedCollider = GetComponent<Collider2D>();

        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (Red == null)
        {
            Red = GameObject.Find("Red_0");
            if (Red == null) Red = GameObject.Find("Red");
        }

        // Get BulletPrefab from player if not assigned
        if (BulletPrefab == null && Red != null)
        {
            var pm = Red.GetComponent<RedMovement>();
            if (pm != null)
            {
                BulletPrefab = pm.BulletPrefab;
            }
        }
    }

    private void Update()
    {
        // Update: lógica por frame (enfocar, disparar, moverse y saltar si conviene)
        if (isDead) return;

        if (Red == null)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        var playerComp = Red.GetComponent<RedMovement>();
        if (playerComp != null && !playerComp.IsAlive)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        Vector3 toPlayer = Red.transform.position - transform.position;
        float distance = toPlayer.magnitude;

        // Mirar al jugador
        if (toPlayer.x != 0f)
        {
            transform.localScale = new Vector3(toPlayer.x >= 0f ? scaleMultiplier : -scaleMultiplier, scaleMultiplier, 1f);
        }

        // Comportamiento: disparar si está en rango y el cooldown ha terminado
        if (distance <= VisionRange)
        {
            if (Time.time >= nextFireTime && !isShooting)
            {
                StartCoroutine(ShootBurstRoutine());
            }

            // Move slightly towards player if too far, or keep distance
            if (distance > 4f && !isShooting)
            {
                float dir = Mathf.Sign(toPlayer.x);
                if (rb != null) rb.linearVelocity = new Vector2(dir * MoveSpeed, rb.linearVelocity.y);
                if (animator != null) animator.SetBool("Movimiento", true);

                // Saltos inteligentes (como los goblins) para evitar quedarse atascado
                TryJumpIfNeeded(toPlayer);
            }
            else
            {
                if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                if (animator != null) animator.SetBool("Movimiento", false);
            }
        }
        else
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (animator != null) animator.SetBool("Movimiento", false);
        }
    }

    private bool IsGroundedGrunt()
    {
        // IsGroundedGrunt: comprueba si el grunt está apoyado en el suelo mediante un raycast
        float extraDistance = 0.05f;
        Vector2 origin = cachedCollider != null ? cachedCollider.bounds.center : (Vector2)transform.position;
        float downDistance = (cachedCollider != null ? cachedCollider.bounds.extents.y : 0.5f) + extraDistance;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, downDistance, 1 << 0); // Layer 0 (Default / Solid tilemaps)
        return hit.collider != null;
    }

    private void TryJumpIfNeeded(Vector3 toPlayer)
    {
        // TryJumpIfNeeded: decide si saltar cuando el jugador está encima o hay un obstáculo
        if (rb == null) return;
        if (Time.time < lastJumpTime + JumpCooldown) return;
        if (!IsGroundedGrunt()) return;

        // Si el jugador está por encima verticalmente
        if (Red != null && Red.transform.position.y > transform.position.y + JumpHeightThreshold)
        {
            rb.AddForce(Vector2.up * JumpForce, ForceMode2D.Impulse);
            lastJumpTime = Time.time;
            return;
        }

        // Raycast horizontal para comprobar si hay un obstáculo/pared justo delante del grunt
        float checkDistance = 0.6f;
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.1f;
        float dirX = Mathf.Sign(toPlayer.x != 0f ? toPlayer.x : 1f);
        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(dirX, 0f), checkDistance, 1 << 0); // Default layer
        
        if (hit.collider != null && !hit.collider.isTrigger && hit.collider.gameObject != gameObject)
        {
            rb.AddForce(Vector2.up * JumpForce, ForceMode2D.Impulse);
            lastJumpTime = Time.time;
        }
    }

    private IEnumerator ShootBurstRoutine()
    {
        // ShootBurstRoutine: controla el disparo en ráfaga (varias balas con un intervalo)
        isShooting = true;
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // Detenerse para disparar
        if (animator != null) animator.SetBool("Movimiento", false);

        for (int i = 0; i < BurstCount; i++)
        {
            if (isDead || Red == null) break;

            // Fire projectile
            FireBullet();

            yield return new WaitForSeconds(TimeBetweenBurstShots);
        }

        nextFireTime = Time.time + FireCooldown;
        isShooting = false;
    }

    private void FireBullet()
    {
        // FireBullet: instancia la bala, la configura y la lanza
        if (BulletPrefab == null || Red == null) return;

        Vector3 aim = (Red.transform.position - transform.position).normalized;
        
        // Spawn offset
        Vector3 spawnPos = transform.position + aim * 0.6f;

        GameObject bullet = Instantiate(BulletPrefab, spawnPos, Quaternion.identity);
        var b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            b.Damage = 1;
            b.Speed = BulletSpeed;
            b.LifeTime = 2.0f;
            b.SetDirection(aim);
            b.SetOwner(gameObject); // Asigna al grunt como propietario para ignorar colisiones con sí mismo
        }
    }

    public void TakeDamage(int damage)
    {
        // TakeDamage: restar vida y reproducir animación de daño
        if (isDead) return;

        Health -= damage;
        if (animator != null) animator.Play("daño"); // Reproducir animación de daño si existe

        if (Health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Die: ejecutar la muerte (desactivar física/colisiones y reproducir animación)
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

        if (animator != null)
        {
            animator.Play("death"); // Reproducir animación de muerte
            Destroy(gameObject, 0.8f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // OnTriggerEnter2D: daño por contacto al jugador
        var player = collision.GetComponent<RedMovement>();
        if (player != null && player.IsAlive && !isDead)
        {
            Vector2 pushDir = (collision.transform.position - transform.position).normalized;
            player.Hit(pushDir, 1, 1f, gameObject);
        }
    }
}