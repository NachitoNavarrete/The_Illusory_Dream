using UnityEngine;

public class ExplosiveBarrier : MonoBehaviour
{
    [Header("Settings")]
    public GameObject destroyedPrefab;
    public GameObject normalVisual;
    public bool destroyOnBreak = true;

    [Header("VFX")]
    public ParticleSystem breakEffect;

    private bool _isBroken = false;

    private void Start()
    {
        if (PlayerPrefs.GetInt("Nivel2ChaseStarted", 0) == 1)
        {
            _isBroken = true;
            if (normalVisual != null)
            {
                normalVisual.SetActive(false);
            }
            var cols = GetComponentsInChildren<Collider2D>();
            foreach (var col in cols)
            {
                col.enabled = false;
            }
        }
    }

    public void Break()
    {
        if (_isBroken) return;
        _isBroken = true;

        if (breakEffect != null)
        {
            breakEffect.Play();
        }

        if (destroyedPrefab != null)
        {
            Instantiate(destroyedPrefab, transform.position, transform.rotation);
        }

        if (normalVisual != null)
        {
            normalVisual.SetActive(false);
        }

        if (destroyOnBreak)
        {
            Destroy(gameObject, 0.5f);
        }
        
        Debug.Log("Barrier destroyed by explosion!");
    }
}
