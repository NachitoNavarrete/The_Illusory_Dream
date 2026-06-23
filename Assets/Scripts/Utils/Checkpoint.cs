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
        return savedPosition.HasValue && savedScene == sceneName;
    }

    public static Vector3 GetSavedCheckpoint(string sceneName)
    {
        return savedPosition.GetValueOrDefault();
    }

    public static void ResetCheckpoint()
    {
        savedPosition = null;
        savedScene = "";
        savedWeapon = null;
        savedWeapon2Unlocked = false;
        savedCollectedFlowers = 0;
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
        }
        else
        {
            col.isTrigger = true;
        }
    }

    private void Update()
    {
        if (isPlayerNear && !isSaved)
        {
            if (Input.GetKeyDown(interactKey))
            {
                SaveCheckpoint();
            }
        }
    }

    private void SaveCheckpoint()
    {
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
            }

            if (promptText != null)
            {
                promptText.text = "¡Guardado!";
                promptText.color = Color.green;
            }

            Debug.Log("Checkpoint saved at: " + savedPosition.Value);
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
