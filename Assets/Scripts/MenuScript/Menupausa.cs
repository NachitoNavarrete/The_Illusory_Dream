/* Menupausa.cs: controla el menú de pausa (pausar, reanudar, volver al menú). */
using UnityEngine;
using UnityEngine;
using UnityEngine.SceneManagement;

// Control sencillo del menú de pausa.
// Comportamiento principal:
// - Cuando pulsas Escape se abre o cierra el menú de pausa.
// - Al pausar: se congela el tiempo (`Time.timeScale = 0`), se pausa la música del juego
//   y se reproduce la música del menú.
// - Al reanudar: se restaura el tiempo, se detiene la música del menú y se reanuda
//   la música del juego desde donde se quedó.
// - `Menu()` lleva al menú principal: antes de cambiar de escena nos aseguramos
//   de desactivar la pausa y pausar la música del juego para poder reanudarla
//   si volvemos a la partida.
public class Menupausa : MonoBehaviour
{
    public GameObject menuPausa;
    public bool juegoPausado = false;

    // Update se ejecuta cada frame y escucha la tecla Escape
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
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
        // Explicación "peras y manzanas":
        // 1) Quitamos la UI del menú de pausa para que el jugador vuelva a ver el juego.
        // 2) `Time.timeScale = 1f` vuelve a poner el tiempo en marcha; esto hace que
        //    los movimientos, físicas y disparos vuelvan a funcionar.
        // 3) `juegoPausado = false` es la bandera interna que usamos para saber si el
        //    juego está en pausa.
        // 4) Paramos la música del menú (si estaba sonando) y reanudamos la música del juego
        //    desde la posición en la que se pausó (no la reiniciamos).
        MusicManagerMenu.StopBackgroundMusicStatic();
        MusicManager.PlayBackgroundMusicStatic(false);
    }
    public void Pausar()
    {
        menuPausa.SetActive(true);
        Time.timeScale = 0f;
        juegoPausado = true; 
        // Explicación "peras y manzanas":
        // 1) Mostramos la UI del menú de pausa para que el jugador vea las opciones.
        // 2) `Time.timeScale = 0f` congela el tiempo del juego: esto deja al personaje
        //    y a las balas "en el aire" hasta que se reanude.
        // 3) Marcamos `juegoPausado = true` para que el Update sepa que ya está en pausa.
        // 4) Pausamos la música del juego con `MusicManager.PauseBackgroundMusicStatic()`
        //    para conservar la posición y poder reanudarla más tarde.
        // 5) Reproducimos la música del menú desde el inicio (`true`) para que suene la
        //    pista del menú mientras el jugador está en la pausa.
        MusicManager.PauseBackgroundMusicStatic();
        MusicManagerMenu.PlayBackgroundMusicStatic(true);
    }
    public void Menu()
    {
        // Explicación "peras y manzanas":
        // 1) Si el jugador elige volver al menú principal, obligamos a que el juego
        //    no quede en pausa para evitar que la siguiente vez que entre al juego
        //    empiece congelado.
        Time.timeScale = 1f;
        juegoPausado = false;

        // 2) Pausamos la música del juego para que no se solape con la música del menú.
        MusicManager.PauseBackgroundMusicStatic();

        // 3) Cargamos la escena del menú principal.
        SceneManager.LoadScene("Menu");
    }
}
