/* Menupausa.cs: controla el men� de pausa (pausar, reanudar, volver al men�). */
using UnityEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

// Control sencillo del men� de pausa.
// Comportamiento principal:
// - Cuando pulsas Escape se abre o cierra el men� de pausa.
// - Al pausar: se congela el tiempo (`Time.timeScale = 0`), se pausa la m�sica del juego
//   y se reproduce la m�sica del men�.
// - Al reanudar: se restaura el tiempo, se detiene la m�sica del men� y se reanuda
//   la m�sica del juego desde donde se qued�.
// - `Menu()` lleva al men� principal: antes de cambiar de escena nos aseguramos
//   de desactivar la pausa y pausar la m�sica del juego para poder reanudarla
//   si volvemos a la partida.
public class Menupausa : MonoBehaviour
{
    public GameObject menuPausa;
    public bool juegoPausado = false;

    // Update se ejecuta cada frame y escucha la tecla Escape o el botón de pausa móvil
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || MobileInput.GetKeyDown("Pause"))
        {
            if (juegoPausado)
            {
                Reanudar();
            }
            else
            {
                Pausar();
            }
        }
    }
    public void Reanudar()
    {
        menuPausa.SetActive(false);
        Time.timeScale = 1f;
        juegoPausado = false;
        // Explicaci�n "peras y manzanas":
        // 1) Quitamos la UI del men� de pausa para que el jugador vuelva a ver el juego.
        // 2) `Time.timeScale = 1f` vuelve a poner el tiempo en marcha; esto hace que
        //    los movimientos, f�sicas y disparos vuelvan a funcionar.
        // 3) `juegoPausado = false` es la bandera interna que usamos para saber si el
        //    juego est� en pausa.
        // 4) Paramos la m�sica del men� (si estaba sonando) y reanudamos la m�sica del juego
        //    desde la posici�n en la que se paus� (no la reiniciamos).
        // Si suena una pista especial (jefe/persecución/robot final), NO la tocamos:
        // sigue sonando bajo el menú de pausa y no hay música de menú que detener.
        if (!MusicManager.IsSpecialTrackPlaying())
        {
            MusicManagerMenu.StopBackgroundMusicStatic();
            MusicManager.PlayBackgroundMusicStatic(false);
        }
    }
    public void Pausar()
    {
        menuPausa.SetActive(true);
        Time.timeScale = 0f;
        juegoPausado = true; 
        // Explicaci�n "peras y manzanas":
        // 1) Mostramos la UI del men� de pausa para que el jugador vea las opciones.
        // 2) `Time.timeScale = 0f` congela el tiempo del juego: esto deja al personaje
        //    y a las balas "en el aire" hasta que se reanude.
        // 3) Marcamos `juegoPausado = true` para que el Update sepa que ya est� en pausa.
        // 4) Pausamos la m�sica del juego con `MusicManager.PauseBackgroundMusicStatic()`
        //    para conservar la posici�n y poder reanudarla m�s tarde.
        // 5) Reproducimos la m�sica del men� desde el inicio (`true`) para que suene la
        //    pista del men� mientras el jugador est� en la pausa.
        // Si suena una pista especial (jefe/persecución/robot final), la dejamos sonar:
        // no la pausamos ni arrancamos la música del menú por encima.
        if (!MusicManager.IsSpecialTrackPlaying())
        {
            MusicManager.PauseBackgroundMusicStatic();
            MusicManagerMenu.PlayBackgroundMusicStatic(true);
        }
    }
    public void Menu()
    {
        // Explicaci�n "peras y manzanas":
        // 1) Si el jugador elige volver al men� principal, obligamos a que el juego
        //    no quede en pausa para evitar que la siguiente vez que entre al juego
        //    empiece congelado.
        Time.timeScale = 1f;
        juegoPausado = false;

        // 2) Pausamos la m�sica del juego para que no se solape con la m�sica del men�.
        MusicManager.PauseBackgroundMusicStatic();

        // 3) Cargamos la escena del men� principal.
        SceneManager.LoadScene("Menu");
    }
}
