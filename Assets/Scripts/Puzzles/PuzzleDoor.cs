using UnityEngine;

/* PuzzleDoor.cs: puerta controlada por switches de puzzle. */
public class PuzzleDoor : MonoBehaviour
{
    [Header("Door Activation")]
    public PuzzleSwitch[] requiredSwitches;
    [Tooltip("Si es true, TODOS los interruptores deben estar activos. Si es false, CUALQUIER interruptor lo abrir�.")]
    public bool allSwitchesRequired = true;

    [Header("Movement Settings")]
    public Vector3 openOffset = new Vector3(0f, 4f, 0f);
    public float openSpeed = 3f;

    private Vector3 closedPos;
    private Vector3 openPos;
    public bool isOpen = false;

    private void Start()
    {
        closedPos = transform.position;
        openPos = closedPos + openOffset;

        if (PlayerPrefs.GetInt("Nivel2ChaseStarted", 0) == 1)
        {
            isOpen = true;
            transform.position = openPos; // Start fully open
        }
    }

    private void Update()
    {
        Vector3 target = isOpen ? openPos : closedPos;
        if (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, openSpeed * Time.deltaTime);
        }
    }

    public void EvaluateState()
    {
        if (PlayerPrefs.GetInt("Nivel2ChaseStarted", 0) == 1)
        {
            isOpen = true;
            return;
        }

        if (requiredSwitches == null || requiredSwitches.Length == 0)
        {
            isOpen = true;
            return;
        }

        bool shouldOpen = allSwitchesRequired;

        foreach (var sw in requiredSwitches)
        {
            if (sw == null) continue;

            if (allSwitchesRequired)
            {
                if (!sw.IsActive)
                {
                    shouldOpen = false;
                    break;
                }
            }
            else
            {
                if (sw.IsActive)
                {
                    shouldOpen = true;
                    break;
                }
            }
        }

        isOpen = shouldOpen;
    }
}
