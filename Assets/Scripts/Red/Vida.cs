/*
/* Vida: controla la barra de vida del jugador. */
using UnityEngine;
using UnityEngine.UI;

public class Vida : MonoBehaviour
{
    // - Este script actualiza la barra de vida (relleno) según la vida del jugador.
    // - Arrastra el Image (UI) y el componente RedMovement del jugador en el Inspector.

    public Image relleno;
    public RedMovement redMovement;   // arrastra aquí el componente RedMovement del jugador

    private float vidaMaxima;

    void Start()
    {
        if (redMovement == null)
        {
            // Intento de búsqueda automática por si olvidaste asignarlo (opcional)
            GameObject red = GameObject.Find("Red");
            if (red != null)
                redMovement = red.GetComponent<RedMovement>();

            if (redMovement == null)
                Debug.LogError("Vida: No se ha asignado RedMovement. Arrástralo en el Inspector.");
        }

        if (redMovement != null)
            vidaMaxima = redMovement.Health;
        else
            Debug.LogError("Vida: no se pudo obtener la vida máxima.");

        if (relleno == null)
            Debug.LogError("Vida: La Image 'relleno' no está asignada.");
    }

    void Update()
    {
        if (redMovement != null && relleno != null && vidaMaxima > 0)
        {
            relleno.fillAmount = redMovement.Health / vidaMaxima;
        }
    }
}