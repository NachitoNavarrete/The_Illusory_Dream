using UnityEngine;
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
        // ¿Por qué esto? "Peras y manzanas": si el juego estaba en pausa y no
        // restauramos `Time.timeScale`, al cargar la escena el juego podría quedar
        // congelado (sin poder moverse). Por eso forzamos `Time.timeScale = 1f`.
        Time.timeScale = 1f;

        // Nos aseguramos de que la música del menú deje de sonar y la música del
        // juego esté preparada para sonar. Llamamos a los métodos estáticos que
        // creamos en los gestores de audio para no tener que buscar las instancias.
        // - `MusicManagerMenu.StopBackgroundMusicStatic()` para parar la pista del menú.
        // - `MusicManager.PlayBackgroundMusicStatic(true)` para pedir al MusicManager
        //   que (re)inicie la música del juego desde el principio al empezar la partida.
        MusicManagerMenu.StopBackgroundMusicStatic();
        MusicManager.PlayBackgroundMusicStatic(true);

        // Finalmente cargamos la escena de juego (siguiente en el Build Settings).
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Salir de la aplicación. No provoca nada mientras estés en el Editor (solo log).
    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}
