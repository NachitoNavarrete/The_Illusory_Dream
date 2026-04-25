using UnityEngine;


/// Bala que aplica daño + knockback solo si el objetivo está vivo.
/// - La bala se mueve con Rigidbody2D.linearVelocity.
/// - Configura el Owner para evitar colisión con quien la disparó.
/// Red y Goblin ya tienen un método `Hit` que acepta daño + knockback, así que la bala solo necesita llamar a ese método (si el componente existe) y luego destruirse a sí misma.
/// Viva cave story!
public class Bullet : MonoBehaviour
{
    public float Speed = 8f;         // Velocidad de la bala
    public int Damage;           // Daño que causa
    public float ForceMultiplier = 1f; // Multiplicador de knockback

    private Rigidbody2D rb;
    private Vector3 direction;
    public GameObject Owner;         // Propietario (para ignorar colisiones)

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) Debug.LogWarning("Bullet: falta Rigidbody2D.");
    }

    private void FixedUpdate()
    {
        if (rb != null) rb.linearVelocity = direction * Speed;
    }

    // Ajusta la dirección (normaliza internamente)
    public void SetDirection(Vector3 dir) => direction = dir.normalized;

    
    /// Establece el owner y evita colisión entre la bala y el owner (si ambos tienen Collider2D).
  
    public void SetOwner(GameObject owner)
    {
        Owner = owner;
        if (Owner == null) return;
        var ownerCol = Owner.GetComponent<Collider2D>();
        var myCol = GetComponent<Collider2D>();
        if (ownerCol != null && myCol != null) Physics2D.IgnoreCollision(ownerCol, myCol);
    }

    public void DestroyBullet() => Destroy(gameObject);

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == Owner) return; // ignorar al propietario

        var red = collision.GetComponent<RedMovement>();
        if (red != null)
        {
            // Solo aplicar daño/knockback si el jugador está vivo
            if (red.IsAlive)
            {
                red.Hit(direction.normalized, Damage, ForceMultiplier);
            }
            DestroyBullet();
            return;
        }
        //tenia puesto el gob.hit no en TakeDamage codigo redundante.
        var gob = collision.GetComponent<GoblinScript>();
        if (gob != null)
        {
            gob.TakeDamage(Damage);
            DestroyBullet();
            return;
        }

        // Destruir contra cualquier otro collider (pared, suelo, etc.)
        DestroyBullet();
    }
}