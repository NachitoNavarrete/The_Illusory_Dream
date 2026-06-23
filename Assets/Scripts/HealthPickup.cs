/*
/* HealthPickup: cura al jugador al tocarlo. */
using UnityEngine;

/// <summary>
/// Componente para pickups de vida. Al tocar al jugador (Red) cura y desaparece.
/// </summary>
public class HealthPickup : MonoBehaviour
{
    [Tooltip("Cantidad de vida que restaura al jugador al recoger este objeto.")]
    public int HealAmount = 2;

    public AudioClip pickupSound;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // OnTriggerEnter2D: se llama cuando algo entra en el trigger del pickup
        if (other == null) return;
        // Acepta colisionadores hijos: buscar el componente en los padres
        var red = other.GetComponentInParent<RedMovement>();
        if (red == null) return;
        if (!red.IsAlive) return;

        // Evitar múltiples activaciones si el jugador tiene varios colliders
        Collider2D selfCol = GetComponent<Collider2D>();
        if (selfCol != null) selfCol.enabled = false;

        // Usar el método Heal del jugador (aplica clamp a MaxHealth)
        red.Heal(HealAmount);

        // Sonido opcional
        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        // Destruir el pickup
        Destroy(gameObject);
    }
}
