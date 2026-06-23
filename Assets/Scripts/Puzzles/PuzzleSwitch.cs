using UnityEngine;

/* PuzzleSwitch.cs: interruptor/pulsador para puzzles que activa puertas u otros mecanismos. */
public enum SwitchMode
{
    Toggle,     // Alterna: al golpear se activa, al golpear de nuevo se desactiva
    OneTime,    // Una sola vez: al golpear se activa y permanece activo
    Hold        // Mantener: activo mientras algo (jugador/objeto) esté encima (presa)
}

public enum TriggerSource
{
    BulletOnly,
    PlayerOnly,
    Both
}

public class PuzzleSwitch : MonoBehaviour
{
    [Header("Switch Settings")]
    public SwitchMode mode = SwitchMode.Toggle;
    public TriggerSource allowedSource = TriggerSource.Both;
    
    [Header("Visual States")]
    public Sprite activeSprite;
    public Sprite inactiveSprite;
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.red;

    [Header("Linked Doors")]
    public PuzzleDoor[] targetDoors;

    private bool isActive = false;
    private SpriteRenderer sr;
    private int overlappingCollidersCount = 0;

    public bool IsActive => isActive;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        UpdateVisuals();
    }

    private void Start()
    {
        // Ensure we have a trigger collider
        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }
    }

    private void UpdateVisuals()
    {
        if (sr != null)
        {
            if (isActive)
            {
                if (activeSprite != null) sr.sprite = activeSprite;
                sr.color = activeColor;
            }
            else
            {
                if (inactiveSprite != null) sr.sprite = inactiveSprite;
                sr.color = inactiveColor;
            }
        }
    }

    private bool IsAllowed(Collider2D other, out bool isBullet)
    {
        isBullet = other.GetComponent<Bullet>() != null || other.name.Contains("Bullet") || other.name.Contains("Bala");
        bool isPlayer = other.CompareTag("Player") || other.GetComponent<RedMovement>() != null;

        if (allowedSource == TriggerSource.BulletOnly && isBullet) return true;
        if (allowedSource == TriggerSource.PlayerOnly && isPlayer) return true;
        if (allowedSource == TriggerSource.Both && (isPlayer || isBullet)) return true;

        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        bool isBullet;
        if (!IsAllowed(other, out isBullet)) return;

        overlappingCollidersCount++;

        if (mode == SwitchMode.OneTime)
        {
            if (!isActive)
            {
                isActive = true;
                UpdateVisuals();
                NotifyDoors();
            }
        }
        else if (mode == SwitchMode.Toggle)
        {
            // Si lo golpea una bala, alterna inmediatamente. Si lo activa el jugador, alterna solo una vez por entrada.
            isActive = !isActive;
            UpdateVisuals();
            NotifyDoors();
        }
        else if (mode == SwitchMode.Hold)
        {
            if (!isActive)
            {
                isActive = true;
                UpdateVisuals();
                NotifyDoors();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        bool isBullet;
        if (!IsAllowed(other, out isBullet)) return;

        overlappingCollidersCount--;
        if (overlappingCollidersCount < 0) overlappingCollidersCount = 0;

        if (mode == SwitchMode.Hold && overlappingCollidersCount == 0)
        {
            if (isActive)
            {
                isActive = false;
                UpdateVisuals();
                NotifyDoors();
            }
        }
    }

    private void NotifyDoors()
    {
        if (targetDoors == null) return;
        foreach (var door in targetDoors)
        {
            if (door != null)
            {
                door.EvaluateState();
            }
        }
    }
}
