using UnityEngine;

/* BossTriggerZone.cs: zona trigger que activa la pelea del jefe cuando entra el jugador. */
public class BossTriggerZone : MonoBehaviour
{
    public CrowBoss boss;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Comprobación robusta para detectar al jugador
        bool isPlayer = collision.CompareTag("Player") || 
                        collision.GetComponent<RedMovement>() != null || 
                        collision.GetComponentInParent<RedMovement>() != null || 
                        collision.name.Contains("Red") || 
                        collision.name.Contains("Player");

        if (isPlayer)
        {
            if (boss != null && boss.currentState == BossState.Inactive)
            {
                boss.ActivateBossFight();
                // Destroy the trigger zone so it only activates once
                Destroy(gameObject);
            }
        }
    }
}
