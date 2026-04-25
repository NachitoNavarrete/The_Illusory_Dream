using UnityEngine;

/// <summary>
/// Controla al enemigo Goblin.
/// Persigue al jugador si lo ve, ataca en rango cuerpo a cuerpo y también puede hacer daño por colisión.
/// Cuando el jugador hace un parry, puede ser empujado y aturdido durante un tiempo.
/// </summary>
public class GoblinScript : MonoBehaviour
{
    // ------------------------------------------------------------
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

    // ------------------------------------------------------------
    // ESTADO DE ATURDIMIENTO (para el parry del jugador)
    // ------------------------------------------------------------
    private bool isStunned = false;        // Verdadero mientras el goblin está aturdido (no actúa)
    private float stunEndTime;             // Momento en que termina el aturdimiento

    // ------------------------------------------------------------
    // START: inicialización
    // ------------------------------------------------------------
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Avisos si faltan componentes importantes
        if (rb == null) Debug.LogWarning("Goblin: falta Rigidbody2D.");
        if (animator == null && DebugAnimator) Debug.LogWarning("Goblin: falta Animator (solo para debug).");
    }

    // ------------------------------------------------------------
    // UPDATE: lógica principal del enemigo
    // ------------------------------------------------------------
    private void Update()
    {
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
        float distance = Mathf.Abs(toPlayer.x); // Solo nos interesa la distancia horizontal para el flip y el ataque básico

        // Voltea el sprite para que mire hacia el jugador
        transform.localScale = new Vector3(toPlayer.x >= 0f ? 1f : -1f, 1f, 1f);

        // Comprobación de visión: si no puede ver al jugador (fuera de rango o hay obstáculo), se queda quieto.
        if (!CanSeePlayer(distance))
        {
            isMoving = false;
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            SetRunBool(false);
            if (DebugAnimator) Debug.Log($"{name}: no ve a Red (dist {distance:F2})");
            return;
        }

        // Decide si debe moverse (si la distancia es mayor que la distancia de parada)
        isMoving = distance > StopDistance;
        SetRunBool(isMoving); // Actualiza el Animator con la animación de correr

        // Si está dentro del rango de ataque y el cooldown lo permite, ataca.
        if (distance <= AttackRange && Time.time >= lastAttackTime + AttackCooldown)
        {
            Attack();
            lastAttackTime = Time.time;       // Reinicia el temporizador de cooldown
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // Se detiene al atacar
            return;
        }

        // Si debe moverse, aplica velocidad hacia el jugador.
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
        var redComp = Red.GetComponent<RedMovement>();
        if (redComp != null)
        {
            if (!redComp.IsAlive) return; // No ataca a un jugador ya muerto
            // Dirección del golpe: desde el goblin hacia el jugador
            Vector2 dir = (Red.transform.position - transform.position).normalized;
            // Llama al Hit del jugador pasando este GameObject como atacante
            redComp.Hit(dir, AttackDamage, AttackForceMultiplier, gameObject);
            return;
        }

        // Fallback: si no encuentra RedMovement, usa SendMessage (por si se cambia el script)
        Red.SendMessage("Hit", SendMessageOptions.DontRequireReceiver);
    }

    // ------------------------------------------------------------
    // DAÑO POR COLISIÓN (cuando el goblin choca contra el jugador con velocidad)
    // ------------------------------------------------------------
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Solo interactúa si el objeto con el que choca tiene el tag "Player"
        if (!collision.gameObject.CompareTag("Player")) return;

        var player = collision.gameObject.GetComponent<RedMovement>();
        if (player == null) return;

        // No aplica daño si el jugador está muerto o en el aire (según el diseño original)
        if (!player.IsAlive || !player.IsGrounded) return;

        // Calcula la velocidad actual del goblin (magnitud del vector de velocidad)
        float mySpeed = rb != null ? rb.linearVelocity.magnitude : 0f;
        // Si no supera el umbral mínimo, no hace daño
        if (mySpeed < DamageVelocityThreshold) return;

        // Dirección del daño: desde el goblin hacia el jugador
        Vector2 direccionDamage = (collision.transform.position - transform.position).normalized;
        // El multiplicador de fuerza depende de la velocidad (a más velocidad, más empuje)
        float forceMultiplier = Mathf.Max(1f, mySpeed * KnockbackMultiplier);

        // Aplica daño al jugador (en esta llamada no se pasa referencia al atacante,
        // por lo que un parry bloquearía el daño pero no aplicaría empuje al goblin).
        player.Hit(direccionDamage, CollisionDamage, forceMultiplier);
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
        Health -= damage;
        Debug.Log($"{name} recibe {damage} de daño. Vida restante: {Health}");
        if (Health <= 0)
        {
            Destroy(gameObject);
        }
    }

    // ------------------------------------------------------------
    // MÉTODO HIT (versión simple, usado por el antiguo SendMessage)
    // ------------------------------------------------------------
    public void Hit()
    {
        Health -= 1;
        if (Health <= 0) Destroy(gameObject);
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