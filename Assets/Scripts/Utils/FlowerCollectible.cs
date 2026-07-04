using UnityEngine;
/* FlowerCollectible.cs: objeto coleccionable que el jugador puede recoger (flor). */
using TMPro;

public class FlowerCollectible : MonoBehaviour
{
    [Header("UI Prompt")]
    public GameObject promptObject;
    public TextMeshPro promptText;

    private bool isPlayerNear = false;
    private RedMovement playerMovement;

    private void Start()
    {
        if (promptObject != null)
        {
            promptObject.SetActive(false);
        }

        if (promptText != null)
        {
            promptText.text = "Interactuar [Abajo]";
            var mr = promptText.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = "Default";
                mr.sortingOrder = 10;
            }
        }

        // A�adir collider trigger si no existe
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            var boxCol = gameObject.AddComponent<BoxCollider2D>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector2(1.5f, 1.5f);
        }
        else
        {
            col.isTrigger = true;
        }
    }

    private void Update()
    {
        if (isPlayerNear && playerMovement != null && playerMovement.IsAlive)
        {
            if (Input.GetKeyDown(KeyCode.DownArrow) || MobileInput.GetKeyDown("Down"))
            {
                Interact();
            }
        }
    }

    private void Interact()
    {
        if (playerMovement != null)
        {
            playerMovement.PlayInteractAnimation();
            playerMovement.CollectFlower();
            
            // Disable interaction to prevent double collecting
            isPlayerNear = false;
            if (promptObject != null)
            {
                promptObject.SetActive(false);
            }

            // Destroy after playing interaction
            Destroy(gameObject, 0.2f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var pm = other.GetComponent<RedMovement>();
        if (pm != null && pm.IsAlive)
        {
            playerMovement = pm;
            isPlayerNear = true;

            if (promptObject != null)
            {
                promptObject.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var pm = other.GetComponent<RedMovement>();
        if (pm != null)
        {
            isPlayerNear = false;
            playerMovement = null;

            if (promptObject != null)
            {
                promptObject.SetActive(false);
            }
        }
    }
}