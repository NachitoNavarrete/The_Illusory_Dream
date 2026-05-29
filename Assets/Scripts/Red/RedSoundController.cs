using UnityEngine;
/// <summary>
/// Controlador de sonidos del jugador Red.
/// Reproduce sonidos de salto, disparo, muerte, caída, daño y pasos.
/// Los pasos tienen un cooldown para no sonar como metralleta.
/// </summary>
// - Este script guarda todos los sonidos que hace el jugador (saltar, disparar, morir, etc.).
// - Cuando ocurre una acción (ej. saltar), el código llama al método correspondiente
//   (ej. playSaltar()) y suena el efecto.
public class RedSoundController : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip sonidoMuerte;
    public AudioClip sonidoDisparo;
    public AudioClip sonidoSaltar;
    public AudioClip sonidoCaida;
    public AudioClip sonidoRecibirDamage;
    public AudioClip sonidoCaminar;
    public AudioClip sonidoParry;
    
    // --- COOLDOWN PARA PASOS ---
    // Tiempo mínimo entre cada paso (en segundos)
    // Ejemplo: 0.3 segundos = 1 paso cada 0.3s = ritmo natural de caminar
    // Si es muy pequeño (<0.1): suena como metralleta
    // Si es muy grande (>0.5): suena lento y raro
    private float footstepCooldown = 0.3f;           // Tiempo mínimo entre pasos
    private float lastFootstepTime = -999f;          // Última vez que se reprodujo un paso (inicialmente muy atrás)

    // --- MÉTODOS DE REPRODUCCIÓN DE SONIDOS ---

    /// <summary>
    /// Reproduce el sonido de salto (solo una vez al presionar Z).
    /// </summary>
    public void playSaltar()
    {
        audioSource.PlayOneShot(sonidoSaltar);
    }

    /// <summary>
    /// Reproduce el sonido de disparo (solo una vez al presionar X).
    /// </summary>
    public void playDisparo()
    {
        audioSource.PlayOneShot(sonidoDisparo);
    }

    /// <summary>
    /// Reproduce el sonido de muerte (solo una vez al morir).
    /// </summary>
    public void playMuerte()
    {
        audioSource.PlayOneShot(sonidoMuerte);
    }

    /// <summary>
    /// Reproduce el sonido al ejecutar un parry exitoso.
    /// </summary>
    public void playParry()
    {
        // Explicación simple:
        // - Este método suena cuando el jugador hace un parry exitoso.
        // - Usa PlayOneShot para que no corte sonidos que ya estén sonando.
        if (sonidoParry != null)
            audioSource.PlayOneShot(sonidoParry);
    }

    /// <summary>
    /// Reproduce el sonido de caída (solo una vez al tocar suelo desde el aire).
    /// </summary>
    public void playCaida()
    {
        audioSource.PlayOneShot(sonidoCaida);
    }

    /// <summary>
    /// Reproduce el sonido de recibir daño (solo una vez al ser golpeado).
    /// </summary>
    public void playRecibirDamage()
    {
        audioSource.PlayOneShot(sonidoRecibirDamage);
    }

    /// <summary>
    /// Reproduce el sonido de PASOS con cooldown.
    /// 
    /// Se llamada CADA FRAME mientras el jugador camina (Update).
    /// SIN cooldown: sería "tak-tak-tak-tak" (metralleta).
    /// CON cooldown: es "tak... tak... tak..." (pasos naturales).
    /// 
    /// Cooldown = esperar tiempo mínimo antes de reproducir otro paso.
    /// </summary>
    public void playCaminar()
    {
        // --- LÓGICA DEL COOLDOWN ---
        // Time.time = tiempo total transcurrido desde que empezó el juego (en segundos)
        // lastFootstepTime = cuándo se reprodujo el ÚLTIMO paso
        // Diferencia = cuánto tiempo pasó desde el último paso
        
        // Si diferencia >= cooldown, reproducir otro paso
        if (Time.time >= lastFootstepTime + footstepCooldown)
        {
            // --- REPRODUCIR PASO ---
            audioSource.PlayOneShot(sonidoCaminar);
            
            // --- ACTUALIZAR TIEMPO ---
            // Guardamos cuándo reproducimos el paso (para la próxima verificación)
            lastFootstepTime = Time.time;
        }
        // Si no cumple la condición: no hace nada (espera el cooldown)
    }
}

