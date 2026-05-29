using System.Linq;
using UnityEngine;

// Helpers simples para el audio: "solo un oyente"
//
// - Unity usa un "AudioListener" como si fuera una oreja en la escena.
// - Si hay más de una oreja, Unity se queja y el sonido puede comportarse raro.
// - Esta clase se asegura de que haya exactamente UNA oreja activa.
// - Los managers de música llaman a EnsureSingleAudioListener("MiManager") en Awake().
//
// Qué hace la función:
// - Si no hay ninguna oreja, intenta crear/activar la de la cámara principal.
// - Si hay más de una, desactiva las extras y deja solo una activa.
// - Si no hay ninguna y no hay Camera.main, deja un warning para que lo soluciones en el Editor.
public static class AudioListenerManager
{
    public static void EnsureSingleAudioListener(string caller)
    {
        var listeners = GameObject.FindObjectsOfType<AudioListener>();

        // 1) Si no hay ninguno, intentamos activar/crear uno en la cámara principal
        if (listeners == null || listeners.Length == 0)
        {
            if (Camera.main != null)
            {
                var al = Camera.main.gameObject.GetComponent<AudioListener>();
                if (al == null)
                {
                    // Le ponemos una "oreja" a la cámara principal para que se oiga el audio
                    Camera.main.gameObject.AddComponent<AudioListener>();
                    Debug.Log($"[{caller}] No se encontró AudioListener. Se creó uno en Camera.main.");
                }
            }
            else
            {
                // Aquí avisamos: lo más seguro es que arregles esto en el Editor
                Debug.LogWarning($"[{caller}] No hay AudioListener en la escena y Camera.main es nulo. Añade un AudioListener a la cámara principal.");
            }
            return;
        }

        // 2) Si hay más de una oreja activa, dejamos solo una (preferimos la cámara)
        int enabledCount = listeners.Count(l => l.enabled);
        if (enabledCount > 1)
        {
            AudioListener preferred = null;
            if (Camera.main != null)
                preferred = Camera.main.GetComponent<AudioListener>();

            if (preferred == null)
                preferred = listeners.FirstOrDefault();

            foreach (var l in listeners)
            {
                if (l != preferred)
                    l.enabled = false; // apagamos las orejas extras
            }

            Debug.Log($"[{caller}] Había {listeners.Length} AudioListeners. Se desactivaron los extras y se mantiene '{preferred.gameObject.name}'.");
        }
        else if (enabledCount == 0)
        {
            // Ninguna estaba habilitada: activamos la de la cámara si existe, si no la primera
            if (Camera.main != null && Camera.main.gameObject.GetComponent<AudioListener>() != null)
            {
                Camera.main.gameObject.GetComponent<AudioListener>().enabled = true;
                Debug.Log($"[{caller}] Ningún AudioListener estaba activo. Se activó el de Camera.main.");
            }
            else
            {
                listeners[0].enabled = true;
                Debug.Log($"[{caller}] Ningún AudioListener estaba activo. Se activó el primero encontrado: '{listeners[0].gameObject.name}'.");
            }
        }

        // Si enabledCount == 1, está todo bien y no hacemos nada.
    }
}
