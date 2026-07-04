using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSystem : MonoBehaviour
{
    [Header("Instrucciones UI")]
    public GameObject panelInstrucciones;

    [Header("Opciones UI")]
    public GameObject panelOpciones;
    public TMPro.TextMeshProUGUI pcBtnText;
    public TMPro.TextMeshProUGUI celularBtnText;

    private void Start()
    {
        // Default control mode to PC if not set
        if (!PlayerPrefs.HasKey("ControlMode"))
        {
            PlayerPrefs.SetString("ControlMode", "PC");
            PlayerPrefs.Save();
        }
        ActualizarVisualOpciones();
    }

    // Cargar la siguiente escena en el Build Settings.
    public void Jugar()
    {
        Checkpoint.ResetCheckpoint(); // Start a fresh game with reset checkpoint, weapons, and chase flags
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    // Salir de la aplicación. No provoca nada mientras estés en el Editor (solo log).
    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }

    // Mostrar/ocultar el panel de instrucciones
    public void MostrarInstrucciones(bool mostrar)
    {
        if (panelInstrucciones != null)
        {
            panelInstrucciones.SetActive(mostrar);
        }
    }

    // Mostrar/ocultar el panel de opciones
    public void MostrarOpciones(bool mostrar)
    {
        if (panelOpciones != null)
        {
            panelOpciones.SetActive(mostrar);
            if (mostrar)
            {
                ActualizarVisualOpciones();
            }
        }
    }

    public void AbrirOpciones()
    {
        MostrarOpciones(true);
    }

    public void CerrarOpciones()
    {
        MostrarOpciones(false);
    }

    public void SeleccionarPC()
    {
        PlayerPrefs.SetString("ControlMode", "PC");
        PlayerPrefs.Save();
        ActualizarVisualOpciones();
    }

    public void SeleccionarCelular()
    {
        PlayerPrefs.SetString("ControlMode", "Mobile");
        PlayerPrefs.Save();
        ActualizarVisualOpciones();
    }

    private void ActualizarVisualOpciones()
    {
        string mode = PlayerPrefs.GetString("ControlMode", "PC");
        if (pcBtnText != null)
        {
            pcBtnText.text = mode == "PC" ? "[X] PC / WINDOWS" : "PC / WINDOWS";
            pcBtnText.color = mode == "PC" ? new Color(0.1f, 0.8f, 0.1f, 1f) : new Color(0.196f, 0.196f, 0.196f, 1.0f);
        }
        if (celularBtnText != null)
        {
            celularBtnText.text = mode == "Mobile" ? "[X] CELULAR / MÓVIL" : "CELULAR / MÓVIL";
            celularBtnText.color = mode == "Mobile" ? new Color(0.1f, 0.8f, 0.1f, 1f) : new Color(0.196f, 0.196f, 0.196f, 1.0f);
        }
    }
}