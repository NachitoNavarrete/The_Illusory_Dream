using UnityEngine;

/* CrowProjectile: proyectil lanzado por el jefe. */
public class CrowProjectile : MonoBehaviour
{
    public GameObject healthDropPrefab;

    private Vector2 direction;
    private float speed;
    private int damage;
    private GameObject owner;

    public void Setup(Vector2 dir, float spd, int dmg, GameObject own)
    {
        // Setup: configura direcci�n, velocidad, da�o y due�o
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        owner = own;

        // Rotar el proyectil para que mire en la direcci�n de movimiento
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Start()
    {
        // Start: lanza el proyectil y programa su autodestrucci�n
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = direction * speed;
        }

        // Autodestruir tras 5 segundos para evitar acumulaci�n
        Destroy(gameObject, 5f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // OnTriggerEnter2D: manejar impactos con balas, jugador o paredes
        // Ignorar colisi�n con el propio boss
        if (collision.gameObject == owner) return;

        // Detectar si la bala del jugador choca con el proyectil de pluma
        var bullet = collision.GetComponent<Bullet>();
        if (bullet != null)
        {
            // Destruir tanto la bala como el proyectil
            bullet.DestroyBullet();

            // Generar posible gota de vida con 30% de probabilidad
            if (Random.value <= 0.30f && healthDropPrefab != null)
            {
                Instantiate(healthDropPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
            return;
        }

        // Impacto con el jugador
        var player = collision.GetComponent<RedMovement>();
        if (player != null)
        {
            // Respetar invulnerabilidad / modo admin de Red: no aplicar daño ni knockback.
            if (player.IsAlive && !player.IsInvulnerable && !player.IsAdminModeActive)
            {
                player.Hit(direction, damage, 1.0f);
            }
            Destroy(gameObject);
            return;
        }
        // Ignorar triggers como checkpoints o monedas
        if (collision.isTrigger) return;
        // Destruir al chocar con paredes o suelo
        Destroy(gameObject);
    }
}
