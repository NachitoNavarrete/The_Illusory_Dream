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
            Debug.Log("Teletransportando al jugador a Nivel 2...");
            SceneManager.LoadScene("Nivel2");
        }
    }
}
