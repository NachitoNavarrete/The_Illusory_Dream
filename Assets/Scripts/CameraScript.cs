using UnityEngine;


/// Cámara simple que sigue la posición X/Y del jugador `Red`.
/// - Mantiene la Z original de la cámara.
/// - Consejo: para suavizar el seguimiento, sustituir asignación directa por Lerp o SmoothDamp.
/// 
public class CameraScript : MonoBehaviour
{
    public GameObject Red; // Arrastrar el jugador desde el Inspector

    void Update()
    {
        if (Red == null) return;
        Vector3 pos = transform.position;
        pos.x = Red.transform.position.x;
        pos.y = Red.transform.position.y;
        transform.position = pos;
    }
}