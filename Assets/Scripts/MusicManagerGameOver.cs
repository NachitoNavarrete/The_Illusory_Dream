using UnityEngine;
/* MusicManagerGameOver.cs: gestiona la música de la pantalla de Game Over. */
using UnityEngine.UI;

// Gestor de la música específica de la pantalla "Game Over".
// - Igual que los otros managers de música, es un singleton y persiste entre escenas.
// - Permite asignar una pista de Game Over en el Inspector y controlarla desde código
//   mediante métodos estáticos (Play/Pause/Stop) para que otros scripts la activen.
//
// - Poner aquí la canción que quieres que suene cuando salga "Game Over".
// - Si no pusieras este objeto en la escena, el código se encargará de crear uno
//   por ti cuando haga falta (ver PlayBackgroundMusicStatic). Así no tienes que
//   añadirlo manualmente si solo quieres que se escuche la pista una vez.
public class MusicManagerGameOver : MonoBehaviour
{
    private static MusicManagerGameOver Instance;

    [SerializeField] private AudioClip BackgroundMusicGameOver;
    [SerializeField] private Slider musicSlider;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.loop = true;
            }
            // Aseguramos que solo haya un AudioListener activo en la escena
            AudioListenerManager.EnsureSingleAudioListener(nameof(MusicManagerGameOver));
        }
        else
        {
            if (BackgroundMusicGameOver != null)
            {
                Instance.BackgroundMusicGameOver = BackgroundMusicGameOver;
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

        if (BackgroundMusicGameOver != null && audioSource != null)
        {
            audioSource.clip = BackgroundMusicGameOver;
            // No reproducimos automáticamente: dejamos que el GameManager decida cuándo sonar.
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
        if (audioSource == null)
        {
            Debug.LogWarning("MusicManagerGameOver: no hay AudioSource asignado. No se puede reproducir música de GameOver.");
            return;
        }

        audioSource.loop = true;

        // Si el código recibe un clip al llamar a Play, usamos ese clip.
        // Si no, el AudioSource debe tener ya un clip asignado en el Inspector.
        if (audioClip != null)
            audioSource.clip = audioClip;
        else
            audioSource.clip = BackgroundMusicGameOver;

        if (audioSource.clip == null)
        {
            // Avisamos de forma clara: no hay canción para reproducir
            Debug.LogWarning("MusicManagerGameOver: no hay AudioClip asignado al AudioSource de GameOver.");
            return;
        }

        // resetSong = true → empezar desde el principio (stop + play)
        // resetSong = false → simplemente play (útil si quieres reanudar)
        if (resetSong)
            audioSource.Stop();

        audioSource.Play();
    }

    public void PauseBackgroundMusic()
    {
        if (audioSource != null)
            audioSource.Pause();
    }

    // Métodos estáticos para control desde otros scripts
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
        // Si no existe aún el gestor (p.ej. no lo colocaste en la escena), lo creamos
        // automáticamente para que la música pueda sonar sin más pasos.
        if (Instance == null)
        {
            GameObject go = new GameObject("MusicManagerGameOver");
            var mgr = go.AddComponent<MusicManagerGameOver>();
            var src = go.AddComponent<AudioSource>();
            src.loop = true;
            mgr.audioSource = src;
            DontDestroyOnLoad(go);
            Instance = mgr;
            // No añadimos AudioListener aquí para no crear duplicados (usar la cámara)
            AudioListenerManager.EnsureSingleAudioListener(nameof(MusicManagerGameOver));
            if (audioClip != null)
            {
                // Si el llamador pasó un clip, lo asignamos ahora para reproducirlo
                mgr.audioSource.clip = audioClip;
            }
        }

        if (Instance != null)
            Instance.PlayBackgroundMusic(resetSong, audioClip);
    }
}
