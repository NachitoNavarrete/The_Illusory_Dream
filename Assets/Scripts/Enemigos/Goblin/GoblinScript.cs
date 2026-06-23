/*
/* GoblinScript: controla al enemigo goblin (ver, perseguir, atacar, recibir daño). */
using JetBrains.Annotations;
using UnityEngine;
using System.Collections.Generic;
using LootItem = Loot;

/// <summary>
/// Controla al enemigo Goblin.
/// Persigue al jugador si lo ve, ataca en rango cuerpo a cuerpo y también puede hacer daño por colisión.
/// Cuando el jugador hace un parry, puede ser empujado y aturdido durante un tiempo.
/// </summary>
// - El goblin busca al jugador y se mueve hacia él.
// - Si está suficientemente cerca, intenta atacar.
// - Si el jugador hace parry, el goblin se empuja y queda aturdido por un rato.
// - Ajusta valores en el Inspector (daño, velocidad, rango) para cambiar su comportamiento.
public class GoblinScript : MonoBehaviour
{
    public GoblinSoundController soundController; // Referencia al controlador de sonidos del goblin (asignar en el Inspector)
    // ------------------------------------
    // ------------------------
    // REFERENCIAS
    // ------------------------------------------------------------
    [Header("Referencias")]
    public GameObject Red;                 // Referencia al GameObject del jugador "Red" (asignar en el Inspector)

    // ------------------------------------------------------------
    // COMBATE
    // ------------------------------------------------------------
    [Header("Combate")]
    public float AttackRange = 1.0f;       // Distancia a la que el goblin inicia su ataque cuerpo a cuerpo
    public float AttackCooldown = 1.0f;    // Tiempo mínimo entre ataques
    private float lastAttackTime;          // Momento del último ataque para controlar el cooldown
    public int Health = 3;                 // Vida actual del goblin

    // ------------------------------------------------------------
    // ATAQUE CUERPO A CUERPO
    // ------------------------------------------------------------
    [Header("Ataque cuerpo a cuerpo")]
    public int AttackDamage = 1;           // Daño que inflige cada ataque
    public float AttackForceMultiplier = 1f; // Multiplicador de la fuerza de knockback al jugador

    // ------------------------------------------------------------
    // DAÑO POR COLISIÓN
    // ------------------------------------------------------------
    [Header("Daño por colisión")]
    public int CollisionDamage = 1;        // Daño por contacto cuando el goblin choca con velocidad suficiente
    public float DamageVelocityThreshold = 0.2f; // Velocidad mínima que debe llevar para hacer daño por colisión
    public float KnockbackMultiplier = 1.0f;    // Multiplicador de knockback en el daño por colisión

    // ------------------------------------------------------------
    // MOVIMIENTO
    // ------------------------------------------------------------
    [Header("Movimiento")]
    public float MoveSpeed = 2.0f;         // Velocidad de desplazamiento horizontal
    public float StopDistance = 0.9f;      // Distancia a la que deja de acercarse (para no solaparse)

    // ------------------------------------------------------------
    // VISIÓN
    // ------------------------------------------------------------
    [Header("Visión")]
    public float VisionRange = 5.0f;       // Distancia máxima a la que detecta al jugador
    public LayerMask ObstacleMask;         // Capas que bloquean la visión (ej. paredes)
    [Tooltip("Altura máxima (en unidades Unity) entre goblin y jugador para que el goblin intente atacar.")]
    public float MaxAttackVerticalDifference = 1.2f;
    [Tooltip("Requerir que el goblin no esté cayendo para iniciar un ataque (usa rb.linearVelocity.y).")]
    public bool RequireNotFallingToAttack = true;

    // ------------------------------------------------------------
    // ANIMATOR
    // ------------------------------------------------------------
    [Header("Animator")]
    public string RunBoolName = "Movimiento"; // Nombre del parámetro bool del Animator para correr
    public bool DebugAnimator = false;        // Si es true, muestra mensajes de depuración sobre el Animator

    // ------------------------------------------------------------
    // COMPONENTES INTERNOS
    // ------------------------------------------------------------
    private Rigidbody2D rb;                // Rigidbody2D del goblin
    private Animator animator;            // Componente Animator (opcional)
    private bool isMoving;                // Bandera local para saber si se está moviendo (para animaciones)
    private Collider2D cachedCollider;    // Collider2D cacheado en Start

    [Header("Salto")]
    public float JumpForce = 5f;               // Fuerza de salto (impulso)
    public float JumpCooldown = 5f;            // Tiempo mínimo entre saltos (segundos)
    public float JumpHeightThreshold = 0.5f;   // Altura por encima del goblin para considerar que el jugador está "arriba"
    private float lastJumpTime = -100f;       // Marca del último salto

    [Header("Loot")]
    public List<LootItem> lootTable = new List<LootItem>();

    // ------------------------------------------------------------
    // ESTADO DE ATURDIMIENTO (para el parry del jugador)
    // ------------------------------------------------------------
    private bool isStunned = false;        // Verdadero mientras el goblin está aturdido (no actúa)
    private float stunEndTime;             // Momento en que termina el aturdimiento
    private bool isDead = false;           // Verdadero si el goblin ha muerto

    // ------------------------------------------------------------
    // START: inicialización
    // ------------------------------------------------------------
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        cachedCollider = GetComponent<Collider2D>();

        // Avisos si faltan componentes importantes
        if (rb == null) Debug.LogWarning("Goblin: falta Rigidbody2D.");
        if (animator == null && DebugAnimator) Debug.LogWarning("Goblin: falta Animator (solo para debug).");
        if (cachedCollider == null) Debug.LogWarning("Goblin: falta Collider2D (recomendado para comprobaciones de suelo).");
    }

    // ------------------------------------------------------------
    // UPDATE: lógica principal del enemigo
    // ------------------------------------------------------------
    private void Update()
    {
        if (isDead) return;

        // --- BLOQUEO POR ATURDIMIENTO ---
        // Si el goblin está aturdido, no ejecuta ninguna acción hasta que termine el tiempo.
        if (isStunned)
        {
            if (Time.time >= stunEndTime)
            {
                isStunned = false; // Terminó el aturdimiento, vuelve a la normalidad
            }
            else
            {
                // Mientras está aturdido, detiene su movimiento por completo y no ataca.
                if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                SetRunBool(false);
                return; // Salimos del Update para que no procese nada más
            }
        }

        // Si la referencia al jugador no está asignada, no hace nada.
        if (Red == null)
        {
            SetRunBool(false);
            return;
        }

        // Obtenemos el script del jugador para consultar su estado (vivo/muerto, etc.)
        var redComp = Red.GetComponent<RedMovement>();
        // Si el jugador está muerto, el goblin se detiene y no intenta atacar ni moverse.
        if (redComp != null && !redComp.IsAlive)
        {
            SetRunBool(false);
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        
        // Vector desde el goblin hasta el jugador
        Vector3 toPlayer = Red.transform.position - transform.position;
        float horizontalDistance = Mathf.Abs(toPlayer.x); // distancia horizontal (x)
        float verticalDistance = Mathf.Abs(toPlayer.y);   // distancia vertical (y)

        // Voltea el sprite para que mire hacia el jugador
        transform.localScale = new Vector3(toPlayer.x >= 0f ? 1f : -1f, 1f, 1f);

        // Comprobación de visión: si no puede ver al jugador (fuera de rango o hay obstáculo), se queda quieto.
        if (!CanSeePlayer(horizontalDistance))
        {
            isMoving = false;
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            SetRunBool(false);
            if (DebugAnimator) Debug.Log($"{name}: no ve a Red (dist horiz {horizontalDistance:F2}, vert {verticalDistance:F2})");
            return;
        }

        // Decide si debe moverse (si la distancia horizontal es mayor que la distancia de parada)
        isMoving = horizontalDistance > StopDistance;
        SetRunBool(isMoving); // Actualiza el Animator con la animación de correr
        // Si está dentro del rango horizontal de ataque, la diferencia vertical no es
        // demasiado grande y (opcional) el goblin no está cayendo, entonces ataca.
        if (horizontalDistance <= AttackRange
            && verticalDistance <= MaxAttackVerticalDifference
            && (!RequireNotFallingToAttack || (rb == null || Mathf.Abs(rb.linearVelocity.y) < 0.5f))
            && Time.time >= lastAttackTime + AttackCooldown)
        {
            Attack();
            lastAttackTime = Time.time;       // Reinicia el temporizador de cooldown
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // Se detiene al atacar
            return;
        }

        // Si debe moverse, aplica velocidad hacia el jugador.
        // Intentamos saltar antes de movernos (si procede)
        TryJumpIfNeeded(toPlayer);

        if (isMoving) MoveTowardsPlayer(toPlayer);
        // Si no, se asegura de que no tenga velocidad horizontal residual.
        else if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // ------------------------------------------------------------
    // VISIÓN DEL JUGADOR
    // ------------------------------------------------------------
    /// <summary>
    /// Determina si el goblin puede ver al jugador.
    /// Comprueba la distancia horizontal y, si hay capa de obstáculos, lanza un raycast.
    /// </summary>
    private bool CanSeePlayer(float horizontalDistance)
    {
        // Si la distancia horizontal ya supera el rango de visión, no lo ve.
        if (horizontalDistance > VisionRange) return false;

        // Si no hay máscara de obstáculos, se asume que siempre ve dentro del rango.
        if (ObstacleMask.value == 0) return true;

        // Lanza un rayo desde el goblin hacia el jugador para ver si hay un obstáculo en medio.
        Vector2 origin = transform.position;
        Vector2 dir = (Red.transform.position - transform.position).normalized;
        float dist = Vector2.Distance(origin, Red.transform.position);
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, ObstacleMask);

        // Dibuja una línea en la vista de escena: verde = sin obstáculo, roja = obstáculo
        Debug.DrawLine(origin, origin + dir * dist, hit.collider == null ? Color.green : Color.red);

        // Si el rayo no golpea nada, el camino está despejado.
        return hit.collider == null;
    }

    // ------------------------------------------------------------
    // MOVIMIENTO HACIA EL JUGADOR
    // ------------------------------------------------------------
    /// <summary>
    /// Desplaza al goblin horizontalmente hacia el jugador a la velocidad configurada.
    /// </summary>
    private void MoveTowardsPlayer(Vector3 toPlayer)
    {
        if (rb == null) return;
        float dir = Mathf.Sign(toPlayer.x);
        rb.linearVelocity = new Vector2(dir * MoveSpeed, rb.linearVelocity.y);
    }

    // ------------------------------------------------------------
    // ATAQUE CUERPO A CUERPO
    // ------------------------------------------------------------
    /// <summary>
    /// Realiza un ataque al jugador, aplicando daño y knockback.
    /// Envía la referencia del propio goblin como atacante para que el parry pueda identificar quién golpeó.
    /// </summary>
    private void Attack()
    {
        if (Red == null) return;

        // Inicia la animación de ataque y gestiona el daño diferido
        StartCoroutine(AttackAnimationRoutine());
    }

    private System.Collections.IEnumerator AttackAnimationRoutine()
    {
        if (animator != null)
        {
            animator.SetBool("Attack", true);
        }

        // Tiempo de anticipación/viento de ataque (da tiempo de reacción al jugador para hacer parry)
        float anticipationTime = 0.35f;
        yield return new WaitForSeconds(anticipationTime);

        // Si fue aturdido, murió o el jugador desapareció durante la anticipación, se cancela el golpe
        if (isStunned || isDead || Red == null)
        {
            if (animator != null)
            {
                animator.SetBool("Attack", false);
            }
            yield break;
        }

        // Comprobamos si el jugador sigue en rango al momento del impacto real
        Vector3 toPlayer = Red.transform.position - transform.position;
        float horizontalDistance = Mathf.Abs(toPlayer.x);
        float verticalDistance = Mathf.Abs(toPlayer.y);

        if (horizontalDistance <= AttackRange + 0.3f && verticalDistance <= MaxAttackVerticalDifference)
        {
            var redComp = Red.GetComponent<RedMovement>();
            if (redComp != null)
            {
                if (redComp.IsAlive)
                {
                    Vector2 dir = (Red.transform.position - transform.position).normalized;
                    redComp.Hit(dir, AttackDamage, AttackForceMultiplier, gameObject);
                }
            }
            else
            {
                // Fallback si no encuentra RedMovement
                Red.SendMessage("Hit", SendMessageOptions.DontRequireReceiver);
            }
        }

        // Esperamos el resto de la animación de ataque
        float remainingTime = Mathf.Max(0.05f, 0.5f - anticipationTime);
        yield return new WaitForSeconds(remainingTime);

        if (animator != null)
        {
            animator.SetBool("Attack", false);
        }
    }

    // ------------------------------------------------------------
    // DAÑO POR COLISIÓN (Deshabilitado para evitar daño doble y empuje incontrolado)
    // ------------------------------------------------------------
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // El daño por colisión directa ha sido deshabilitado para evitar que el jugador 
        // reciba daño doble y salga volando inesperadamente, según diseño solicitado.
    }

    // ------------------------------------------------------------
    // MÉTODO PARA RECIBIR EFECTOS DEL PARRY DEL JUGADOR
    // ------------------------------------------------------------
    /// <summary>
    /// Aplica un empuje y un aturdimiento al goblin.
    /// Este método es llamado por el jugador cuando bloquea un ataque con parry.
    /// </summary>
    /// <param name="direction">Dirección del empuje (normalizada).</param>
    /// <param name="force">Fuerza del empuje.</param>
    /// <param name="stunDuration">Duración en segundos del aturdimiento.</param>
    public void ApplyParryEffects(Vector2 direction, float force, float stunDuration)
    {
        if (isDead) return;

        // --- Empuje ---
        if (rb != null)
        {
            // Reseteamos la velocidad actual para que el efecto sea contundente
            rb.linearVelocity = Vector2.zero;
            // Aplicamos una fuerza instantánea en la dirección indicada
            rb.AddForce(direction * force, ForceMode2D.Impulse);
        }

        // --- Aturdimiento ---
        isStunned = true;
        stunEndTime = Time.time + stunDuration;
        SetRunBool(false); // Detenemos la animación de correr

        if (animator != null)
        {
            animator.Play("DAño"); // Reproduce la animación de recibir daño
        }

        Debug.Log($"{name} ha sido empujado y aturdido durante {stunDuration} segundos.");
    }

    // ------------------------------------------------------------
    // MÉTODO PARA RECIBIR DAÑO GENÉRICO (usado por otras fuentes, como balas)
    // ------------------------------------------------------------
    /// <summary>
    /// Resta una cantidad de daño a la vida del goblin y lo destruye si llega a 0 o menos.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        Health -= damage;
        Debug.Log($"{name} recibe {damage} de daño. Vida restante: {Health}");
        if (Health <= 0)
        {
            Die();
        }
        else
        {
            if (animator != null)
            {
                animator.Play("DAño"); // Reproduce la animación de recibir daño
            }
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (animator != null)
        {
            animator.Play("death"); // Reproduce la animación de muerte
        }

        // Detener movimiento y desactivar Rigidbody para evitar empujes o gravedad extra
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Desactivar colisiones para que no estorbe ni siga recibiendo impactos
        var colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        // Reproducir sonido de muerte en la posición del goblin sin depender
        // del AudioSource del propio goblin (para que suene aunque el objeto se destruya).
        if (soundController != null && soundController.sonidoMuerte != null)
        {
            AudioSource.PlayClipAtPoint(soundController.sonidoMuerte, transform.position);
        }

        // Drops: recorrer la tabla de loot y tirar según probabilidad (dropChance en 0-100)
        foreach (var loot in lootTable)
        {
            if (loot == null || loot.itemPrefab == null) continue;
            if (Random.value <= (loot.dropChance / 100f))
            {
                Instantiate(loot.itemPrefab, transform.position, Quaternion.identity);
            }
        }

        Destroy(gameObject, 0.6f); // Esperar a que la animación de muerte termine de reproducirse
    }

    // ------------------------------------------------------------
    // MÉTODO HIT (versión simple, usado por el antiguo SendMessage)
    // ------------------------------------------------------------
    public void Hit()
    {
        if (isDead) return;

        Health -= 1;
        
        if (Health <= 0)
        {
            Die();
        }
        else
        {
            if (animator != null)
            {
                animator.Play("DAño"); // Reproduce la animación de recibir daño
            }
        }
    }

    // ------------------------------------------------------------
    // ANIMATOR: activar/desactivar el bool de correr
    // ------------------------------------------------------------
    /// <summary>
    /// Establece el parámetro booleano de correr en el Animator, si existe.
    /// </summary>
    private void SetRunBool(bool value)
    {
        if (animator == null) return;
        // Recorre los parámetros del Animator buscando el nombre configurado
        foreach (var p in animator.parameters)
        {
            if (p.name == RunBoolName && p.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(RunBoolName, value);
                return;
            }
        }
        if (DebugAnimator) Debug.LogWarning($"Animator no tiene un bool llamado '{RunBoolName}'.");
    }

    // ------------------------------------------------------------
    // SALTO: comprobación de suelo y comportamiento de salto
    // ------------------------------------------------------------
    /// <summary>
    /// Comprueba rápidamente si el goblin está apoyado en el suelo.
    /// Usa un raycast hacia abajo partiendo del centro del collider (o del transform si no hay collider).
    /// </summary>
    private bool IsGroundedGoblin()
    {
        float extraDistance = 0.05f;
        Vector2 origin = cachedCollider != null ? cachedCollider.bounds.center : (Vector2)transform.position;
        float downDistance = (cachedCollider != null ? cachedCollider.bounds.extents.y : 0.5f) + extraDistance;
        int mask = ObstacleMask.value == 0 ? ~0 : ObstacleMask;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, downDistance, mask);
        Debug.DrawLine(origin, origin + Vector2.down * downDistance, hit.collider == null ? Color.yellow : Color.green);
        return hit.collider != null;
    }

    /// <summary>
    /// Decide si el goblin debe saltar: si el jugador está por encima o si hay un obstáculo justo delante.
    /// Controla cooldown y sólo salta si está en el suelo.
    /// </summary>
    private void TryJumpIfNeeded(Vector3 toPlayer)
    {
        if (rb == null) return;
        if (Time.time < lastJumpTime + JumpCooldown) return;
        if (!IsGroundedGoblin()) return;

        // Si el jugador está por encima (más alto que threshold), saltar
        if (Red != null && Red.transform.position.y > transform.position.y + JumpHeightThreshold)
        {
            // Saltar
            rb.AddForce(Vector2.up * JumpForce, ForceMode2D.Impulse);
            lastJumpTime = Time.time;
            return;
        }

        // Detectar obstáculo horizontal corto delante (a la altura del collider)
        float checkDistance = 0.45f;
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.1f;
        float dirX = Mathf.Sign(toPlayer.x != 0f ? toPlayer.x : 1f);
        int mask = ObstacleMask.value == 0 ? ~0 : ObstacleMask;
        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(dirX, 0f), checkDistance, mask);
        Debug.DrawLine(origin, origin + new Vector2(dirX, 0f) * checkDistance, hit.collider == null ? Color.cyan : Color.magenta);
        if (hit.collider != null)
        {
            rb.AddForce(Vector2.up * JumpForce, ForceMode2D.Impulse);
            lastJumpTime = Time.time;
        }
    }

    // ------------------------------------------------------------
    // GIZMOS PARA EL EDITOR (esferas de rango en Scene View)
    // ------------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        // Amarillo: rango de ataque
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, AttackRange);
        // Cyan: rango de visión
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, VisionRange);
    }
}
