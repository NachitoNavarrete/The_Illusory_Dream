using UnityEngine;
/* DoorToLevel2.cs: portal/puerta que carga la escena del Nivel 2 al entrar el jugador. */
using UnityEngine.SceneManagement;

public class DoorToLevel2 : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Comprobar si el collider pertenece al jugador
        if (collision.CompareTag("Player") || collision.GetComponent<RedMovement>() != null)
        {
            var redMove = collision.GetComponent<RedMovement>();
            if (redMove == null) redMove = collision.GetComponentInParent<RedMovement>();
            
            if (redMove != null)
            {
                // Save current state so it transfers to Level 2
                PlayerPrefs.SetInt("CheckpointWeapon", (int)redMove.CurrentWeapon);
                PlayerPrefs.SetInt("CheckpointWeaponUnlocked", redMove.RedWeapon2Unlocked ? 1 : 0);
                PlayerPrefs.SetInt("CheckpointFlowers", redMove.CollectedFlowersCount);
                PlayerPrefs.Save();
                
                // Clear old save position so they start at the level entrance
                Checkpoint.ResetCheckpoint();
            }

            Debug.Log("Teletransportando al jugador a Nivel 2...");
            SceneManager.LoadScene("Nivel2");
        }
    }
}
