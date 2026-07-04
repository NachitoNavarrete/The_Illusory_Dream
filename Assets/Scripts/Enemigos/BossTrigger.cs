using UnityEngine;

// BossTrigger: objeto simple que activa la introducción del jefe cuando el jugador entra.
//
// Explicación para jóvenes (12-13 años):
// - Este script está en una zona invisible del nivel. Cuando el jugador la toca,
//   le dice al jefe que empiece su escena de introducción (diálogo o música).
// - Después de activarlo, desactiva el trigger para que no se vuelva a activar.
public class BossTrigger : MonoBehaviour
{
    public RobotBoss boss;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Si el que entra es el jugador, pedimos al boss que empiece la intro
        if (other.CompareTag("Player") || other.GetComponent<RedMovement>() != null)
        {
            if (boss != null && (boss.currentState == RobotBossState.Inactive))
            {
                boss.StartIntro();
                // Desactivamos este objeto para que solo dispare una vez
                gameObject.SetActive(false);
            }
        }
    }
}
