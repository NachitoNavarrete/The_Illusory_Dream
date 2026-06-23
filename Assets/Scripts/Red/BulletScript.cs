/* Bullet: mueve la bala, aplica daño y knockback al impactar. */
using UnityEngine;
public class Bullet : MonoBehaviour
{
    public float Speed = 8f;         // Velocidad de la bala
    public int Damage;           // Daño que causa
    public float ForceMultiplier = 1f; // Multiplicador de knockback
    public float LifeTime = 2f;      // Tiempo de vida de la bala (determina el alcance máximo)

    private Rigidbody2D rb;
    private Vector3 direction;
    public GameObject Owner;         // Propietario (para ignorar colisiones)

    void Start()
    {
        // Start: cachea el Rigidbody y programa autodestrucción
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) Debug.LogWarning("Bullet: falta Rigidbody2D.");

        Destroy(gameObject, LifeTime);
    }

    private void FixedUpdate()
    {
        // FixedUpdate: mueve la bala cada frame físico
        if (rb != null) rb.linearVelocity = direction * Speed;
    }

    // SetDirection: define la dirección de movimiento (normalizada)
    public void SetDirection(Vector3 dir) => direction = dir.normalized;

    
    // SetOwner: asigna el propietario y evita colisión entre la bala y quien la disparó
    public void SetOwner(GameObject owner)
    {
        Owner = owner;
        if (Owner == null) return;
        var ownerCol = Owner.GetComponent<Collider2D>();
        var myCol = GetComponent<Collider2D>();
        if (ownerCol != null && myCol != null) Physics2D.IgnoreCollision(ownerCol, myCol);
    }

    // DestroyBullet: destruye la bala
    public void DestroyBullet() => Destroy(gameObject);

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // OnTriggerEnter2D: manejar colisiones con objetivos
        if (collision.gameObject == Owner) return; // ignorar al propietario

        var red = collision.GetComponent<RedMovement>();
        if (red != null)
        {
            // Solo aplicar daño/knockback si el jugador está vivo
            if (red.IsAlive)
            {
                if (red.IsParrying)
                {
                    // PARRY / REBOUND MECHANIC:
                    // 1. Invert the bullet direction
                    direction = -direction;

                    // 2. Change the owner to the player so it hurts enemies
                    Owner = red.gameObject;

                    // 3. Make sure the physics system ignores collisions between player and bullet
                    var playerCol = red.GetComponent<Collider2D>();
                    var myCol = GetComponent<Collider2D>();
                    if (playerCol != null && myCol != null)
                    {
                        Physics2D.IgnoreCollision(playerCol, myCol, true);
                    }

                    // 4. Trigger the player's parry success visual/audio feedback
                    red.TriggerParrySuccessFeedback();

                    return; // Do NOT destroy the bullet, it rebounded!
                }
                else
                {
                    red.Hit(direction.normalized, Damage, ForceMultiplier);
                }
            }
            DestroyBullet();
            return;
        }

        // Check if Admin Mode is active on the owner player to enable one-shot kills
        bool isAdminOneShot = false;
        if (Owner != null && Owner.CompareTag("Player"))
        {
            var pm = Owner.GetComponent<RedMovement>();
            if (pm != null && pm.IsAdminModeActive)
            {
                isAdminOneShot = true;
            }
        }

        int finalDamage = isAdminOneShot ? 9999 : Damage;

        // Deal damage to various enemy types
        var robot = collision.GetComponent<RobotEnemy>();
        if (robot != null)
        {
            robot.TakeDamage(finalDamage);
            DestroyBullet();
            return;
        }

        var grunt = collision.GetComponent<GruntEnemy>();
        if (grunt != null)
        {
            grunt.TakeDamage(finalDamage);
            DestroyBullet();
            return;
        }

        var gob = collision.GetComponent<GoblinScript>();
        if (gob != null)
        {
            gob.TakeDamage(finalDamage);
            DestroyBullet();
            return;
        }

        var boss = collision.GetComponent<CrowBoss>();
        if (boss != null)
        {
            boss.TakeDamage(finalDamage);
            DestroyBullet();
            return;
        }

        var crow = collision.GetComponent<CrowEnemy>();
        if (crow != null)
        {
            crow.TakeDamage(finalDamage);
            DestroyBullet();
            return;
        }

        // Destruir contra cualquier otro collider (pared, suelo, etc.), pero ignorar triggers (como checkpoints)
        if (!collision.isTrigger)
        {
            DestroyBullet();
        }
    }
}