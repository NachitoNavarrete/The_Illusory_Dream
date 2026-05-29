using UnityEngine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject GameOverPanel;
    public TextMeshProUGUI gameOverText;
    public Button reiniciarButton;
    public Button Menu;
    [Header("Audio")]
    [Tooltip("Asignar aquí el AudioClip que sonará en la pantalla de Game Over.")]
    public AudioClip gameOverClip;

    // - `gameOverClip` es la canción que sonará cuando el jugador muera.
    // - Arrástrala desde la carpeta Assets hasta este campo en el Inspector.

    private bool gameOverActivo = false;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (GameOverPanel != null)
            GameOverPanel.SetActive(false);

        if (reiniciarButton != null)
            reiniciarButton.onClick.AddListener(ReiniciarEscena);

        if (Menu != null)
            Menu.onClick.AddListener(menu);


    }

    // Update is called once per frame
    void Update()
    {
        if (gameOverActivo)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                ReiniciarEscena();
            }
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M))
            {
                menu();
            }
        }
    }
    public void GameOver()
    {
        if (gameOverActivo) return;

        gameOverActivo = true;

        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(true);

        }
        if (gameOverText != null)
        {
            gameOverText.text = "has muerto..." ;
        }
        // Parar la música de juego para evitar que siga sonando sobre la pista de GameOver
        MusicManager.StopBackgroundMusicStatic();
        // Reproducir la música de Game Over.
        // Explicación "peras y manzanas": primero paramos la música del juego para
        // que no se mezclen las pistas. Luego pedimos al gestor de GameOver que reproduzca
        // la canción que hayas puesto en `gameOverClip`. Si ese gestor no existe, se
        // creará automáticamente en tiempo de ejecución.
        MusicManagerGameOver.PlayBackgroundMusicStatic(true, gameOverClip);
    }
    public void ReiniciarEscena()
    {
        Time.timeScale = 1f; // Asegura que el tiempo esté normalizado al reiniciar
        // Si estaba sonando la música de GameOver, detenerla antes de reiniciar
        MusicManagerGameOver.StopBackgroundMusicStatic();
        // Reiniciar la música del juego desde el principio
        MusicManager.PlayBackgroundMusicStatic(true);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void menu()
    {
        Time.timeScale = 1f; // Asegura que el tiempo esté normalizado al ir al menú
        // Nos aseguramos de que la música de Game Over (si está sonando) pare y
        // también paramos la música del juego por si queda alguna pista activa.
        MusicManagerGameOver.StopBackgroundMusicStatic();
        MusicManager.StopBackgroundMusicStatic();

        SceneManager.LoadScene("Menu");
    }

}