using UnityEngine;
using UnityEngine;
using UnityEngine.UI;

// Gestor de la música del menú principal/pausa.
// - Funciona muy parecido a `MusicManager` pero con su propia pista de menu.
// - También es singleton y persiste entre escenas para que la música del menú
//   pueda seguir sonando al navegar por las pantallas del menú.
public class MusicManagerMenu : MonoBehaviour
{
    private static MusicManagerMenu Instance;

    [SerializeField] private AudioClip BackgroundMusicMenu; // ✅ Ahora visible en el Inspector
    [SerializeField] private Slider musicSlider;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
            // No sobrescribimos el clip aquí: dejamos al inspector decidir.
            // Al usar DontDestroyOnLoad evitamos que el objeto se destruya al cambiar de escena.
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (BackgroundMusicMenu != null)
        {
            audioSource.clip = BackgroundMusicMenu;
            audioSource.Play();
        }

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(delegate { SetVolume(musicSlider.value); });
    }

    public static void SetVolume(float volume)
    {
        if (Instance != null && Instance.audioSource != null)
            Instance.audioSource.volume = volume;
    }

    public void PlayBackgroundMusic(bool resetSong, AudioClip audioClip = null)
    {
        // Comprobaciones y explicación simple:
        // - Si no hay AudioSource, no podemos reproducir audio.
        // - Si se pasa un clip, lo usamos; si no, usamos el clip ya configurado.
        if (audioSource == null)
        {
            Debug.LogWarning("MusicManagerMenu: no hay AudioSource asignado. No se puede reproducir música de menú.");
            return;
        }

        if (audioClip != null)
            audioSource.clip = audioClip;

        if (audioSource.clip == null)
        {
            Debug.LogWarning("MusicManagerMenu: no hay AudioClip asignado al AudioSource del menú.");
            return;
        }

        // Si resetSong es true, reiniciamos la pista desde el principio.
        if (resetSong)
            audioSource.Stop();

        audioSource.Play();
    }

    public void PauseBackgroundMusic()
    {
        if (audioSource != null)
            audioSource.Pause();
    }

    // ------------------------------------------------------------------------
    // Métodos estáticos de conveniencia (envoltorios del singleton)
    // ------------------------------------------------------------------------
    // Estos métodos permiten que otros scripts (por ejemplo, el controlador de pausa)
    // paren, pausen o inicien la música del menú sin obtener la instancia manualmente.
    // Uso típico:
    // - `MusicManagerMenu.PlayBackgroundMusicStatic(true)` -> reproducir desde el inicio.
    // - `MusicManagerMenu.StopBackgroundMusicStatic()` -> parar totalmente la pista.
    // - `MusicManagerMenu.PauseBackgroundMusicStatic()` -> pausar y poder reanudar.
    // ------------------------------------------------------------------------
    public static void PauseBackgroundMusicStatic()
    {
        if (Instance != null && Instance.audioSource != null)
            Instance.audioSource.Pause();
    }

    public static void StopBackgroundMusicStatic()
    {
        if (Instance != null && Instance.audioSource != null)
            Instance.audioSource.Stop();
    }

    public static void PlayBackgroundMusicStatic(bool resetSong = false, AudioClip audioClip = null)
    {
        if (Instance != null)
            Instance.PlayBackgroundMusic(resetSong, audioClip);
    }
}