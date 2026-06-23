using System.Collections;
/* RobotEnemy.cs: enemigo tipo robot que explota tras recibir suficiente daño. */
using UnityEngine;
using TMPro;

public class RobotEnemy : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject Red;
    public GameObject ExplosionEffectPrefab; 

    [Header("Stats")]
    public int Health = 4;
    public float MoveSpeed = 2.0f;
    public int MeleeDamage = 2;              // Inflige 2 puntos de daño
    public float VisionRange = 10.0f;

    [Header("Countdown on Death")]
    public float ExplosionRadius = 2.5f;
    public int ExplosionDamage = 3;

    [Header("Visual Scale")]
    public float scaleMultiplier = 0.65f; // Escala visual para ajustar proporciones

    [Header("Salto")]
    public float JumpForce = 5.5f;
    public float JumpCooldown = 3.0f;
    public float JumpHeightThreshold = 0.5f;
    private float lastJumpTime = -100f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Collider2D cachedCollider;
    private bool isDead = false;

    private TextMeshPro countdownText;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        cachedCollider = GetComponent<Collider2D>();

        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (Red == null)
        {
            Red = GameObject.Find("Red_0");
            if (Red == null) Red = GameObject.Find("Red");
        }

        // Crear dinámicamente el texto de cuenta atrás para asegurar que existe
        CreateCountdownText();
    }

    private void CreateCountdownText()
    {
        GameObject textGo = new GameObject("CountdownText");
        textGo.transform.SetParent(transform, false);
        textGo.transform.localPosition = new Vector3(0f, 1.3f, 0f);

        countdownText = textGo.AddComponent<TextMeshPro>();
        countdownText.text = "";
        countdownText.fontSize = 6f;
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownText.color = Color.red;
        countdownText.fontStyle = FontStyles.Bold;

        var mr = textGo.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingLayerName = "Default";
            mr.sortingOrder = 15; // Render on top of everything
        }
    }

    private void Update()
    {
        if (isDead) return;

        if (Red == null)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        var playerComp = Red.GetComponent<RedMovement>();
        if (playerComp != null && !playerComp.IsAlive)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        Vector3 toPlayer = Red.transform.position - transform.position;
        float distance = toPlayer.magnitude;

        // Voltear el sprite para que mire al jugador
        if (toPlayer.x != 0f)
        {
            transform.localScale = new Vector3(toPlayer.x >= 0f ? scaleMultiplier : -scaleMultiplier, scaleMultiplier, 1f);
        }

        // Si el jugador está dentro del rango de visión, seguir y atacar
        if (distance <= VisionRange)
        {
            float dir = Mathf.Sign(toPlayer.x);
            if (rb != null) rb.linearVelocity = new Vector2(dir * MoveSpeed, rb.linearVelocity.y);
            if (animator != null) animator.SetBool("Movimiento", true);
        }
        else
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (animator != null) animator.SetBool("Movimiento", false);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        Health -= damage;
        
        // Destello breve en blanco al recibir daño
        StartCoroutine(DamageFlashRoutine());

        if (Health <= 0)
        {
            StartCountdownSequence();
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            spriteRenderer.color = Color.white;
        }
    }

    private void StartCountdownSequence()
    {
        isDead = true;
        
        // Detener todo movimiento por completo
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Desactivar los colliders estándar para que el jugador no siga chocando, pero permitir la explosión
        var colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        if (animator != null)
        {
            animator.SetBool("Movimiento", false);
            animator.Play("Idle"); // Mantenerse en idle
        }

        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        // Parpadear en rojo rápidamente para indicar la secuencia de autodestrucción
        for (int count = 3; count >= 1; count--)
        {
            if (countdownText != null)
            {
                countdownText.text = count.ToString();
                countdownText.fontSize = 6f + (3 - count) * 1.5f; // Aumenta al acercarse a 0
            }

            // Secuencia de parpadeo rápido durante 1 segundo
            float elapsed = 0f;
            while (elapsed < 1.0f)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = (spriteRenderer.color == Color.white) ? Color.red : Color.white;
                }
                yield return new WaitForSeconds(0.2f);
                elapsed += 0.2f;
            }
        }

        if (countdownText != null)
        {
            countdownText.text = "💥";
        }

        Explode();
    }

    private void Explode()
    {
        // Efecto visual de explosión
        SpawnExplosionVisuals();

        // Daño en área al jugador
        if (Red != null)
        {
            float distToPlayer = Vector2.Distance(transform.position, Red.transform.position);
            if (distToPlayer <= ExplosionRadius)
            {
                var player = Red.GetComponent<RedMovement>();
                if (player != null && player.IsAlive)
                {
                    Vector2 pushDir = (Red.transform.position - transform.position).normalized;
                    // La explosión inflige gran daño y empuje fuerte
                    player.Hit(pushDir, ExplosionDamage, 2.5f, gameObject);
                    Debug.Log("RobotEnemy explotó! Infligió " + ExplosionDamage + " de daño a Red!");
                }
            }
        }

        Destroy(gameObject, 0.1f);
    }

    private void SpawnExplosionVisuals()
    {
        // Generar explosión visual retro con sprites
        PixelExplosion.CreateExplosion(transform.position);

        // Reproducir sonido de explosión si está disponible (solo en Editor se carga por seguridad)
#if UNITY_EDITOR
        AudioClip expSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SpriteSheets/sounds/snd_explosion_solid.ogg");
        if (expSound != null)
        {
            AudioSource.PlayClipAtPoint(expSound, transform.position);
        }
#endif
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        // Ataque cuerpo a cuerpo al colisionar
        var player = collision.gameObject.GetComponent<RedMovement>();
        if (player != null && player.IsAlive)
        {
            Vector2 pushDir = (collision.transform.position - transform.position).normalized;
            player.Hit(pushDir, MeleeDamage, 1.2f, gameObject);
        }
    }
}