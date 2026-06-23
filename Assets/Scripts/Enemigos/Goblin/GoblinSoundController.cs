using UnityEngine;

// GoblinSoundController: controla sonidos específicos del goblin (muerte, etc.).
// - Este script guarda la "caja" que reproduce sonidos (AudioSource) y el clip
//   que suena cuando el goblin muere. Cuando el goblin muere, llamamos a
//   playMuerte() y suena el efecto.
public class GoblinSoundController : MonoBehaviour
{
    // Caja que reproduce sonidos (arrástrala en el Inspector)
    public AudioSource audioSource;
    // Clip que suena cuando el goblin muere
    public AudioClip sonidoMuerte;

    // Reproduce el sonido de muerte del goblin una vez.
    public void playMuerte()
    {
        if (audioSource != null && sonidoMuerte != null)
            audioSource.PlayOneShot(sonidoMuerte);
    }
}


