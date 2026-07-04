using UnityEngine;
/* MusicManager.cs: gestiona la música de fondo del juego (singleton que persiste entre escenas). */
using UnityEngine;
using UnityEngine.UI;

// - Este script gestiona la música de fondo del juego (la pista que suena mientras juegas).
// - Es un singleton: solo debe haber uno y persiste entre escenas para que la música no se corte.

public class MusicManager : MonoBehaviour
{
    private static MusicManager Instance;

    [SerializeField] private AudioClip BackgroundMusic; // ✅ Ahora visible en el Inspector
    [SerializeField] private AudioClip[] BackgroundMusicList; // ✅ Array para selección aleatoria de canciones
    [SerializeField] private Slider musicSlider;

    private AudioSource audioSource;

    // true = la pista actual es una pista especial (jefe/persecución) que NO debe
    // detenerse al pausar. false = es la música normal del mapa.
    private bool isSpecialTrack;

    private void Awake()
    {
        if (BackgroundMusicList != null && BackgroundMusicList.Length > 0)
        {
            BackgroundMusic = BackgroundMusicList[Random.Range(0, BackgroundMusicList.Length)];
        }

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.loop = true;
            }
            // No sobrescribimos el clip aquí para que el Inspector decida qué pista usar.
            // Nota: `DontDestroyOnLoad` hace que este objeto persista entre escenas,
            // por eso aplicamos el patrón singleton para que no haya duplicados.
        }
        else
        {
            // SEGURIDAD SINGLETON: si ya existe un gestor de música, NO debemos interrumpir
            // la música si es una pista especial (jefe, persecución) o si ya está sonando
            // la misma música de fondo normal.
            if (BackgroundMusic != null && Instance != null)
            {
                bool isPlayingSame = Instance.audioSource != null && Instance.audioSource.clip == BackgroundMusic && Instance.audioSource.isPlaying;
                if (!Instance.isSpecialTrack && !isPlayingSame)
                {
                    Instance.BackgroundMusic = BackgroundMusic;
                    Instance.PlayBackgroundMusic(true, BackgroundMusic);
                }
            }
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (audioSource != null)
        {
            audioSource.loop = true;
        }

        // Si ya hay un clip asignado (como la música especial del jefe iniciada por RobotBoss.Start()),
        // no la sobrescribimos con el tema de nivel global.
        if (BackgroundMusic != null && audioSource != null && (audioSource.clip == null || !isSpecialTrack))
        {
            audioSource.clip = BackgroundMusic;
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
        // Seguridad: comprobar que hay un AudioSource (la caja que reproduce sonido)
        if (audioSource == null)
        {
            Debug.LogWarning("MusicManager: no hay AudioSource asignado. No se puede reproducir música.");
            return;
        }

        audioSource.loop = true;

        // Tres casos:
        // 1) Nos pasan un clip explícito → usarlo. Es "especial" si difiere de la música normal.
        // 2) audioClip==null && resetSong → volver intencionadamente a la música normal (respawn).
        // 3) audioClip==null && !resetSong → REANUDAR tras pausa: NO sobrescribir el clip actual,
        //    así una pista de jefe/persecución sigue sonando. Solo caemos a BackgroundMusic si no hay clip.
        if (audioClip != null)
        {
            audioSource.clip = audioClip;
            isSpecialTrack = (audioClip != BackgroundMusic);
        }
        else if (resetSong)
        {
            audioSource.clip = BackgroundMusic;
            isSpecialTrack = false;
        }
        else
        {
            if (audioSource.clip == null)
                audioSource.clip = BackgroundMusic;
        }

        if (audioSource.clip == null)
        {
            Debug.LogWarning("MusicManager: no hay AudioClip asignado al AudioSource.");
            return;
        }

        // resetSong = true → empezar la pista desde 0. resetSong = false → intentar reanudar.
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
    // Métodos estáticos de conveniencia
    // ------------------------------------------------------------------------
    // Estos métodos permiten que otros scripts llamen a las acciones de música
    // sin tener que buscar la instancia del MusicManager. Son envoltorios sencillos
    // para el singleton `Instance`.
    // Ejemplo de uso: `MusicManager.PauseBackgroundMusicStatic()`
    // Esto es útil desde menús o controladores de escena para pausar/reanudar música.
    // ------------------------------------------------------------------------
    // IMPORTANTE: usar Pause si quieres conservar la posición actual de la pista
    // y Stop si quieres reiniciar la pista desde el principio.
    // ------------------------------------------------------------------------
    public static void PauseBackgroundMusicStatic()
    {
        if (Instance != null && Instance.audioSource != null)
            Instance.audioSource.Pause();
    }

    public static void StopBackgroundMusicStatic()
    {
        if (Instance != null && Instance.audioSource != null)
        {
            Instance.audioSource.Stop();
            Instance.isSpecialTrack = false;
        }
    }

    // Permite al menú de pausa saber si suena una pista especial (jefe/persecución)
    // que NO debe pausarse ni reemplazarse por la música del menú.
    public static bool IsSpecialTrackPlaying()
    {
        return Instance != null && Instance.isSpecialTrack;
    }

    public static void PlayBackgroundMusicStatic(bool resetSong = false, AudioClip audioClip = null)
    {
        if (Instance != null)
            Instance.PlayBackgroundMusic(resetSong, audioClip);
    }
}