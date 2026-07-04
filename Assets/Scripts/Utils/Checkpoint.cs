using UnityEngine;
/* Checkpoint.cs: controla los puntos de control y guarda el estado del jugador. */
using TMPro;
using UnityEngine.SceneManagement;

public class Checkpoint : MonoBehaviour
{
    private static Vector3? savedPosition = null;
    private static string savedScene = "";

    // Estado guardado del jugador (arma, desbloqueos, flores recogidas)
    private static WeaponType? savedWeapon = null;
    private static bool savedWeapon2Unlocked = false;
    private static int savedCollectedFlowers = 0;

    public static void RestorePlayerWeaponState(RedMovement player)
    {
        if (savedWeapon.HasValue)
        {
            player.CurrentWeapon = savedWeapon.Value;
            player.RedWeapon2Unlocked = savedWeapon2Unlocked;
            player.CollectedFlowersCount = savedCollectedFlowers;
            Debug.Log("Checkpoint: Restored player weapon state: " + player.CurrentWeapon + ", Flowers Collected: " + player.CollectedFlowersCount);
        }
    }

    [Header("UI Prompt")]
    public GameObject promptObject; // Objeto de texto flotante o indicación UI
    public TextMeshPro promptText;   // Componente TextMeshPro para el texto flotante

    [Header("Settings")]
    public KeyCode interactKey = KeyCode.DownArrow;

    private bool isPlayerNear = false;
    private bool isSaved = false;

    public static bool HasSavedCheckpoint(string sceneName)
    {
        // Si no hay datos en memoria, intentamos cargar desde PlayerPrefs
        if (!savedPosition.HasValue) LoadGlobalCheckpoint();
        
        // Verificamos si los datos cargados corresponden a la escena actual
        bool exists = savedPosition.HasValue && !string.IsNullOrEmpty(savedScene) && savedScene == sceneName;
        
        Debug.Log("[Checkpoint] HasSavedCheckpoint check for '" + sceneName + "'. Result: " + exists + 
                  " (Memory: " + (savedPosition.HasValue ? savedPosition.Value.ToString() : "null") + " in scene '" + savedScene + "')");
        
        return exists;
    }

    public static Vector3 GetSavedCheckpoint(string sceneName)
    {
        return savedPosition.GetValueOrDefault();
    }

    public static void ResetCheckpoint()
    {
        Debug.Log("Checkpoint: Resetting all checkpoint data.");
        savedPosition = null;
        savedScene = "";
        savedWeapon = null;
        savedWeapon2Unlocked = false;
        savedCollectedFlowers = 0;

        // Limpiar todas las claves de PlayerPrefs del jugador para nueva partida
        PlayerPrefs.DeleteKey("CheckpointX");
        PlayerPrefs.DeleteKey("CheckpointY");
        PlayerPrefs.DeleteKey("CheckpointZ");
        PlayerPrefs.DeleteKey("CheckpointScene");
        PlayerPrefs.DeleteKey("CheckpointWeapon");
        PlayerPrefs.DeleteKey("CheckpointWeaponUnlocked");
        PlayerPrefs.DeleteKey("CheckpointFlowers");

        // Limpiar banderas del jefe y la persecución
        PlayerPrefs.SetInt("Nivel2ChaseStarted", 0);
        PlayerPrefs.SetInt("Nivel2ChaseCompleted", 0);
        PlayerPrefs.SetInt("Nivel2RefightBoss", 0);
        PlayerPrefs.SetInt("Nivel2BossDefeated", 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Fuerza un checkpoint en una posición concreta (usado al morir durante la batalla del
    /// jefe para respawnear en el checkpoint inicial). Conserva el estado de arma del jugador.
    /// </summary>
    public static void ForceCheckpoint(Vector3 pos, RedMovement player)
    {
        Debug.Log("Checkpoint: Forcing checkpoint at " + pos);
        savedPosition = pos;
        savedScene = SceneManager.GetActiveScene().name;

        PlayerPrefs.SetFloat("CheckpointX", pos.x);
        PlayerPrefs.SetFloat("CheckpointY", pos.y);
        PlayerPrefs.SetFloat("CheckpointZ", pos.z);
        PlayerPrefs.SetString("CheckpointScene", savedScene);

        if (player != null)
        {
            savedWeapon = player.CurrentWeapon;
            savedWeapon2Unlocked = player.RedWeapon2Unlocked;
            savedCollectedFlowers = player.CollectedFlowersCount;
            PlayerPrefs.SetInt("CheckpointWeapon", (int)savedWeapon.Value);
            PlayerPrefs.SetInt("CheckpointWeaponUnlocked", savedWeapon2Unlocked ? 1 : 0);
            PlayerPrefs.SetInt("CheckpointFlowers", savedCollectedFlowers);
        }
        PlayerPrefs.Save();
    }

    private void Start()
    {
        // Ensure prompt is hidden at start
        if (promptObject != null)
        {
            promptObject.SetActive(false);
        }
        
        if (promptText != null)
        {
            promptText.text = "Guardar [Abajo]";
            var mr = promptText.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = "Default";
                mr.sortingOrder = 10; // Ensure it's rendered on top of layers
            }
        }

        // Add a trigger collider if not present
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            var boxCol = gameObject.AddComponent<BoxCollider2D>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector2(2.5f, 2.5f);
            col = boxCol;
        }
        else
        {
            col.isTrigger = true;
        }

        // CONTROL DE ACTIVACIÓN: En el Nivel 2, todos los checkpoints deben estar ACTIVOS (forzar enabled = true).
        // Se permite guardar en cualquier momento para que el jugador pueda reanudar desde donde prefiera.
        if (col != null)
        {
            col.enabled = true;
        }
    }

    private void Update()
    {
        if (isPlayerNear && !isSaved)
        {
            if (Input.GetKeyDown(interactKey) || (interactKey == KeyCode.DownArrow && MobileInput.GetKeyDown("Down")))
            {
                SaveCheckpoint();
            }
        }
    }

    private void SaveCheckpoint()
    {
        // Se permite guardar en cualquier checkpoint en cualquier momento del Nivel 2.
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Red_0");

        if (player != null)
        {
            savedPosition = player.transform.position;
            savedScene = SceneManager.GetActiveScene().name;
            isSaved = true;

            var redMove = player.GetComponent<RedMovement>();
            if (redMove != null)
            {
                savedWeapon = redMove.CurrentWeapon;
                savedWeapon2Unlocked = redMove.RedWeapon2Unlocked;
                savedCollectedFlowers = redMove.CollectedFlowersCount;
                Debug.Log("Checkpoint: Saved player weapon state: " + savedWeapon.Value + ", Flowers: " + savedCollectedFlowers);
                
                // Persist globally
                PlayerPrefs.SetFloat("CheckpointX", savedPosition.Value.x);
                PlayerPrefs.SetFloat("CheckpointY", savedPosition.Value.y);
                PlayerPrefs.SetFloat("CheckpointZ", savedPosition.Value.z);
                PlayerPrefs.SetString("CheckpointScene", savedScene);
                PlayerPrefs.SetInt("CheckpointWeapon", (int)savedWeapon.Value);
                PlayerPrefs.SetInt("CheckpointWeaponUnlocked", savedWeapon2Unlocked ? 1 : 0);
                PlayerPrefs.SetInt("CheckpointFlowers", savedCollectedFlowers);
                PlayerPrefs.Save();
            }

            if (promptText != null)
            {
                promptText.text = "¡Guardado!";
                promptText.color = Color.green;
            }

            Debug.Log("Checkpoint saved at: " + savedPosition.Value);
        }
    }

    public static void LoadGlobalCheckpoint()
    {
        if (PlayerPrefs.HasKey("CheckpointScene"))
        {
            float x = PlayerPrefs.GetFloat("CheckpointX");
            float y = PlayerPrefs.GetFloat("CheckpointY");
            float z = PlayerPrefs.GetFloat("CheckpointZ");
            savedPosition = new Vector3(x, y, z);
            savedScene = PlayerPrefs.GetString("CheckpointScene");
            
            if (PlayerPrefs.HasKey("CheckpointWeapon"))
                savedWeapon = (WeaponType)PlayerPrefs.GetInt("CheckpointWeapon");
            
            savedWeapon2Unlocked = PlayerPrefs.GetInt("CheckpointWeaponUnlocked", 0) == 1;
            savedCollectedFlowers = PlayerPrefs.GetInt("CheckpointFlowers", 0);
            
            Debug.Log("[Checkpoint] Global data loaded from PlayerPrefs: " + savedPosition.Value + " in " + savedScene);
        }
        else
        {
            Debug.Log("[Checkpoint] No global data found in PlayerPrefs.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<RedMovement>() != null)
        {
            isPlayerNear = true;
            isSaved = false;
            
            if (promptObject != null)
            {
                promptObject.SetActive(true);
            }

            if (promptText != null)
            {
                promptText.text = "Guardar [Abajo]";
                promptText.color = Color.white;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.GetComponent<RedMovement>() != null)
        {
            isPlayerNear = false;
            
            if (promptObject != null)
            {
                promptObject.SetActive(false);
            }
        }
    }
}
