using UnityEngine;
/* BossHealthBar: muestra la barra de vida del jefe.
   Métodos clave: Show() - mostrar, Hide() - ocultar, SetHealth(current,max) - actualizar barra. */
using UnityEngine.UI;

/// <summary>
/// Controla la barra de vida del jefe (UI).
/// Asignar en el Inspector el Image de relleno y opcionalmente un Text para mostrar n�meros.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    public Image fillImage;
    public UnityEngine.UI.Text healthText;

    private void Awake()
    {
        if (gameObject.activeSelf && fillImage == null)
            Debug.LogWarning("BossHealthBar: fillImage no asignado.");
    }

    public void Show()
    {
        // Show: hace visible la UI de la barra de vida
        if (fillImage != null && fillImage.transform.parent != null)
        {
            fillImage.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        // Hide: oculta la UI de la barra de vida
        if (fillImage != null && fillImage.transform.parent != null)
        {
            fillImage.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            // Evita desactivar el GameObject principal si este script está en el boss
            if (GetComponent<CrowBoss>() == null)
            {
                gameObject.SetActive(false);
            }
        }
    }

    public void SetHealth(int current, int max)
    {
        // SetHealth: actualiza el relleno y el texto que muestra la vida
        if (fillImage != null && max > 0)
        {
            fillImage.fillAmount = Mathf.Clamp01((float)current / (float)max);
        }
        if (healthText != null)
        {
            healthText.text = $"{current}/{max}";
        }
    }
}
