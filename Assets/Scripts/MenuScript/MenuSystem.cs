using UnityEngine;
using UnityEngine.SceneManagement;



/// Sistema de menú muy sencillo:
/// - `Jugar()` carga la siguiente escena en el build index.
/// - `Salir()` cierra la aplicación (en el editor solo hace log).
public class MenuSystem : MonoBehaviour
{
    // Cargar la siguiente escena en el Build Settings.
    public void Jugar()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Salir de la aplicación. No provoca nada mientras estés en el Editor (solo log).
    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}