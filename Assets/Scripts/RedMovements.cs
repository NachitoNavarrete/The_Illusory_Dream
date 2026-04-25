using System.Collections;
using UnityEngine;

/// <summary>
/// Control principal del jugador y protagonista "Red".
/// Maneja movimiento horizontal, salto, dash, disparo, sistema de vida y daño,
/// partículas para el salto y un sistema de parry que empuja y aturde al enemigo.
/// </summary>
public class RedMovement : MonoBehaviour
{
    // ------------------------------------------------------------
    // CABECERAS DEL INSPECTOR - Movimiento
    // ------------------------------------------------------------
    [Header("Movimiento (Inspector)")]
    public float Speed = 5f;               // Velocidad de desplazamiento horizontal (unidades por segundo)
    public float JumpForce = 6f;           // Fuerza de impulso vertical aplicada al saltar

    // ------------------------------------------------------------
    // CABECERAS DEL INSPECTOR - Detección de suelo
    // ------------------------------------------------------------
    [Header("Suelo (Raycasts)")]
    public LayerMask GroundLayer;          // Capa(s) que se consideran suelo para los raycasts
    public float GroundRayLength = 0.18f;  // Longitud del rayo hacia abajo para detectar el suelo

    // ------------------------------------------------------------
    // CABECERAS DEL INSPECTOR - Dash
    // ------------------------------------------------------------
    [Header("Dash")]
    public KeyCode DashKey = KeyCode.LeftShift;   // Tecla que activa el dash
    public float DashSpeed = 12f;                 // Velocidad horizontal durante el dash
    public float DashDuration = 0.18f;            // Cuánto dura el dash en segundos
    public float DashCooldown = 1.0f;             // Tiempo de espera antes de poder volver a usar el dash

    // ------------------------------------------------------------
    // CABECERAS DEL INSPECTOR - Disparo
    // ------------------------------------------------------------
    [Header("Disparo")]
    public GameObject BulletPrefab;        // Prefab de la bala que se instancia al disparar
    public float BulletSpawnOffset = 0.5f; // Distancia desde el centro del jugador donde aparece la bala

    // ------------------------------------------------------------
    // CABECERAS DEL INSPECTOR - Vida y knockback
    // ------------------------------------------------------------
    [Header("Vida y Knockback")]
    public int Health = 5;                 // Puntos de vida iniciales del jugador
    public float BaseKnockback = 3f;       // Fuerza base de empuje cuando recibe daño

    // ------------------------------------------------------------
    // CABECERAS DEL INSPECTOR - Partículas (salto)
    // ------------------------------------------------------------
    [Header("Particulas")]
    public ParticleSystem particulaSalto;  // Sistema de partículas que se reproduce al saltar

    // ------------------------------------------------------------
    // CABECERAS DEL INSPECTOR - Parry (bloqueo con empuje y aturdimiento)
    // ------------------------------------------------------------
    [Header("Parry (bloqueo)")]
    public KeyCode ParryKey = KeyCode.C;           // Tecla para ejecutar el parry
    public float ParryWindow = 0.25f;              // Duración en segundos de la ventana de parry (cuánto tiempo puedes bloquear)
    public float ParryCooldown = 0.8f;             // Tiempo de espera antes de poder volver a parrear
    public ParticleSystem parrySuccessParticle;    // Partícula que se lanza al bloquear con éxito un golpe (opcional)
    public GameObject parryExplosionPrefab;        // Prefab de una explosión/efecto visual que se instancia al parrear (opcional)
    public float ParryPushForce = 8f;              // Fuerza de empuje que recibe el enemigo al ser parreado
    public float ParryStunDuration = 0.6f;         // Duración en segundos del aturdimiento del enemigo tras el parry

    // ------------------------------------------------------------
    // COMPONENTES INTERNOS (cacheados al inicio)
    // ------------------------------------------------------------
    private Rigidbody2D rb;                         // Referencia al Rigidbody2D del jugador
    private Collider2D col;                         // Referencia al Collider2D del jugador (usado para los raycasts de suelo)
    private Animator animator;                     // Referencia al Animator (puede ser nulo si no se usa)

    // ------------------------------------------------------------
    // ESTADO INTERNO DEL JUGADOR
    // ------------------------------------------------------------
    private float horizontal;                       // Valor del eje horizontal (-1, 0, 1) leído cada frame
    private bool grounded;                          // Verdadero si el jugador está tocando el suelo
    private bool isDamaged = false;                 // Bandera temporal para la animación de daño

    // Invulnerabilidad
    public bool IsInvulnerable { get; private set; } = false;  // Indica si el jugador ignora el daño (ej. tras recibir un golpe o power-up)

    // Dash
    private bool isDashing = false;                 // Verdadero si el dash está activo en este momento
    private float dashEndTime = 0f;                 // Momento (Time.time) en que termina el dash actual
    private float nextDashTime = 0f;                // Momento a partir del cual se puede volver a usar el dash

    // Vida
    private bool isAlive = true;                    // Falso cuando el jugador muere
    public bool IsAlive => isAlive;                // Propiedad pública de solo lectura para consultar si está vivo

    // Parry
    private bool isParrying = false;                // Verdadero mientras la ventana de parry está activa
    private float parryEndTime;                     // Momento en que termina la ventana de parry actual
    private float nextParryTime;                    // Momento a partir del cual se puede volver a parrear

    // Suelo (propiedad pública para consultar desde otros scripts)
    public bool IsGrounded => grounded;

    // ------------------------------------------------------------
    // MÉTODO START: inicialización de componentes y configuración
    // ------------------------------------------------------------
    private void Start()
    {
        // Obtenemos los componentes necesarios
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();

        // Avisos de depuración si faltan componentes esenciales
        if (rb == null) Debug.LogWarning("RedMovement: falta Rigidbody2D en el GameObject.");
        if (col == null) Debug.LogWarning("RedMovement: falta Collider2D en el GameObject.");
        if (animator == null) Debug.Log("RedMovement: no se encontró Animator (si no usas animaciones está bien).");
        else
        {
            // Listamos todos los parámetros del Animator para ayudar en la configuración
            Debug.Log($"Animator encontrado en '{name}'. Parámetros:");
            foreach (var p in animator.parameters)
                Debug.Log($" - {p.name} ({p.type})");

            // Comprobamos si existe el bool "Muerto", necesario para la animación de muerte
            if (!AnimatorHasBool("Muerto"))
                Debug.LogWarning("Animator no tiene el parámetro booleano 'Muerto'. Añádelo o cambia el nombre en el script.");

            // Si hay un clip llamado "Idle", lo reproducimos al inicio para partir de un estado conocido
            if (animator.runtimeAnimatorController != null)
            {
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                {
                    if (clip.name == "Idle")
                    {
                        animator.Play("Idle");
                        break;
                    }
                }
            }
        }

        // Si no se ha configurado GroundLayer, intentamos asignar automáticamente la capa "Ground"
        if (GroundLayer.value == 0)
        {
            int li = LayerMask.NameToLayer("Ground");
            if (li != -1) GroundLayer = LayerMask.GetMask("Ground");
        }
    }

    // ------------------------------------------------------------
    // MÉTODO UPDATE: lógica por frame (inputs, estados, animaciones)
    // ------------------------------------------------------------
    private void Update()
    {
        // Si el jugador está muerto, no procesamos nada
        if (!isAlive) return;

        // --- 1) Movimiento horizontal y flip del sprite ---
        // Leemos el eje horizontal (teclas A/D o flechas izquierda/derecha)
        horizontal = Input.GetAxisRaw("Horizontal");
        // Volteamos el sprite según la dirección: escala X negativa = mira a la izquierda, positiva = mira a la derecha
        if (horizontal < 0f) transform.localScale = new Vector3(-1f, 1f, 1f);
        else if (horizontal > 0f) transform.localScale = new Vector3(1f, 1f, 1f);

        // --- 2) Detectar si está en el suelo ---
        UpdateGrounded();

        // --- 3) Sincronizar parámetros del Animator ---
        // Estos métodos actualizan los bools del Animator solo si existen, sin causar errores
        SetAnimatorBoolSafe("isRunning", horizontal != 0f);     // Moviéndose horizontalmente
        SetAnimatorBoolSafe("isGrounded", grounded);            // En el suelo
        SetAnimatorBoolSafe("isJumping", !grounded);            // En el aire
        SetAnimatorBoolSafe("Damage", isDamaged);               // Recibiendo daño
        SetAnimatorBoolSafe("Muerto", !isAlive);                // Muerto

        // --- 4) Salto ---
        // Tecla Z, solo si está en el suelo y no es invulnerable
        if (Input.GetKeyDown(KeyCode.Z) && grounded && !IsInvulnerable)
        {
            Jump();
        }

        // --- 5) Dash ---
        // Tecla configurada, solo si no está en cooldown y no está ya dasheando
        if (Input.GetKeyDown(DashKey) && Time.time >= nextDashTime && !isDashing)
        {
            StartDash();
        }

        // --- 6) Terminar el dash cuando se acaba su duración ---
        if (isDashing && Time.time >= dashEndTime)
        {
            EndDash();
        }

        // --- 7) Disparo (tecla X) ---
        if (Input.GetKeyDown(KeyCode.X))
        {
            Shoot();
        }

        // --- 8) Parry (bloqueo/empuje/aturdimiento) ---
        // Activar parry con la tecla correspondiente, si no está en cooldown y no está ya parryando
        if (Input.GetKeyDown(ParryKey) && Time.time >= nextParryTime && !isParrying)
        {
            StartParry();
        }

        // Finalizar el parry cuando se supera la ventana de tiempo
        if (isParrying && Time.time >= parryEndTime)
        {
            EndParry();
        }
    }

    // ------------------------------------------------------------
    // MÉTODO FIXEDUPDATE: física (movimiento horizontal constante)
    // ------------------------------------------------------------
    private void FixedUpdate()
    {
        // No aplicamos movimiento si está muerto o en dash
        if (!isAlive) return;
        if (isDashing) return;

        // Movimiento horizontal: establecemos la velocidad X según la entrada y la velocidad configurada
        // La velocidad Y se mantiene (para respetar la gravedad y los saltos)
        if (rb != null) rb.linearVelocity = new Vector2(horizontal * Speed, rb.linearVelocity.y);
    }

    // ------------------------------------------------------------
    // DETECCIÓN DE SUELO MEDIANTE RAYCASTS
    // ------------------------------------------------------------
    /// <summary>
    /// Comprueba si el jugador está tocando el suelo usando tres raycasts hacia abajo
    /// (izquierdo, central y derecho) desde la base del collider.
    /// Actualiza la variable 'grounded'.
    /// </summary>
    private void UpdateGrounded()
    {
        grounded = false;
        if (col == null) return;

        // Obtenemos los límites del collider
        Bounds b = col.bounds;
        // El origen Y de los rayos es justo debajo del borde inferior, con un pequeño margen
        float yOrigin = b.min.y + 0.01f;
        // Posiciones de los tres orígenes (izquierda, centro y derecha del personaje)
        Vector2 leftOrigin = new Vector2(b.center.x - b.extents.x * 0.9f, yOrigin);
        Vector2 centerOrigin = new Vector2(b.center.x, yOrigin);
        Vector2 rightOrigin = new Vector2(b.center.x + b.extents.x * 0.9f, yOrigin);

        ///Raycast fue el proceso que más me complico, ya que el RayCast no aparecia y todo era por un error que 
        //no estaba ignorando el propio collider del jugador, lo que hacia que el rayo impactara siempre con el jugador y no detectara el suelo
        //y el suelo no detecto al jugador y el jugador al piso, así que no tuve más opción que agregar más Raycast y configurar desde proyect settings
        //la configuración de Unity.
        RaycastHit2D hitL, hitC, hitR;
        if (GroundLayer.value != 0)
        {
            // Si hay capas de suelo configuradas, lanzamos rayos solo contra ellas
            hitL = Physics2D.Raycast(leftOrigin, Vector2.down, GroundRayLength, GroundLayer);
            hitC = Physics2D.Raycast(centerOrigin, Vector2.down, GroundRayLength, GroundLayer);
            hitR = Physics2D.Raycast(rightOrigin, Vector2.down, GroundRayLength, GroundLayer);
        }
        else
        {
            // Si no, lanzamos contra todo, pero ignorando el propio collider del jugador
            hitL = Physics2D.Raycast(leftOrigin, Vector2.down, GroundRayLength);
            if (hitL.collider == col) hitL = default;
            hitC = Physics2D.Raycast(centerOrigin, Vector2.down, GroundRayLength);
            if (hitC.collider == col) hitC = default;
            hitR = Physics2D.Raycast(rightOrigin, Vector2.down, GroundRayLength);
            if (hitR.collider == col) hitR = default;
        }

        // Se considera en suelo si al menos uno de los tres rayos impacta algo
        grounded = (hitL.collider != null) || (hitC.collider != null) || (hitR.collider != null);

        // Dibujamos los raycasts en la vista de escena para depuración (verde = suelo, rojo = aire)
        Debug.DrawRay(leftOrigin, Vector2.down * GroundRayLength, hitL.collider != null ? Color.green : Color.red);
        Debug.DrawRay(centerOrigin, Vector2.down * GroundRayLength, hitC.collider != null ? Color.green : Color.red);
        Debug.DrawRay(rightOrigin, Vector2.down * GroundRayLength, hitR.collider != null ? Color.green : Color.red);
    }

    // ------------------------------------------------------------
    // SALTO
    // ------------------------------------------------------------
    /// <summary>
    /// Aplica el salto: resetea la velocidad vertical para que el salto sea consistente,
    /// añade un impulso hacia arriba y reproduce la partícula de salto.
    /// </summary>
    private void Jump()
    {
        if (rb == null) return;

        // Ponemos la velocidad Y a 0 para evitar que la gravedad acumulada reduzca el salto
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        // Aplicamos fuerza instantánea hacia arriba
        rb.AddForce(Vector2.up * JumpForce, ForceMode2D.Impulse);

        // Si hay un sistema de partículas asignado, lo reproducimos
        if (particulaSalto != null)
            particulaSalto.Play();
    }

    // ------------------------------------------------------------
    // DASH
    // ------------------------------------------------------------
    private void StartDash()
    {
        isDashing = true;
        dashEndTime = Time.time + DashDuration;
        nextDashTime = Time.time + DashCooldown;

        // La dirección del dash es hacia donde mira el sprite (escala X positiva = derecha)
        float dir = transform.localScale.x >= 0f ? 1f : -1f;
        float vy = rb != null ? rb.linearVelocity.y : 0f; // Mantenemos la velocidad vertical actual
        if (rb != null) rb.linearVelocity = new Vector2(dir * DashSpeed, vy);
    }

    private void EndDash()
    {
        isDashing = false;
        // Al terminar el dash, detenemos el movimiento horizontal dejando que la física normal tome el control
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // ------------------------------------------------------------
    // DISPARO
    // ------------------------------------------------------------
    /// <summary>
    /// Instancia una bala en la dirección apuntada (flecha arriba, abajo, o según el sprite).
    /// </summary>
    private void Shoot()
    {
        if (BulletPrefab == null) return;

        // Determinamos la dirección de disparo
        Vector3 aim;
        if (Input.GetKey(KeyCode.UpArrow)) aim = Vector2.up;
        else if (Input.GetKey(KeyCode.DownArrow) && !grounded) aim = Vector2.down; // Solo disparar hacia abajo si está en el aire
        else aim = transform.localScale.x == 1f ? Vector2.right : Vector2.left;   // Horizontal según hacia dónde mira

        // Instanciamos la bala un poco desplazada en la dirección del disparo
        GameObject bullet = Instantiate(BulletPrefab, transform.position + (Vector3)aim * BulletSpawnOffset, Quaternion.identity);
        var b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            b.SetDirection(aim);
            b.SetOwner(gameObject);
        }
    }

    // ------------------------------------------------------------
    // PARRY (BLOQUEO CON EMPUJE Y ATURDIMIENTO)
    // ------------------------------------------------------------

    /// <summary>
    /// Activa la ventana de parry. Durante ParryWindow segundos, los golpes recibidos serán bloqueados
    /// y se aplicará un empuje y aturdimiento al atacante.
    /// </summary>
    private void StartParry()
    {
        isParrying = true;
        parryEndTime = Time.time + ParryWindow;
        nextParryTime = Time.time + ParryCooldown;
    }

    /// <summary>
    /// Desactiva la ventana de parry. A partir de este momento los golpes volverán a hacer daño.
    /// </summary>
    private void EndParry()
    {
        isParrying = false;
    }

    /// <summary>
    /// Llamada cuando un golpe es bloqueado con éxito durante la ventana de parry.
    /// Reproduce efectos visuales (partículas y explosión opcionales) y, si se conoce al atacante,
    /// le aplica un empuje y un aturdimiento.
    /// </summary>
    /// <param name="attacker">GameObject del enemigo que ha intentado golpear al jugador.</param>
    private void OnParrySuccess(GameObject attacker)
    {
        // --- Efectos visuales del parry ---
        if (parrySuccessParticle != null)
            parrySuccessParticle.Play(); // Reproduce la partícula configurada (debe estar en el jugador o ser hija)

        if (parryExplosionPrefab != null)
            Instantiate(parryExplosionPrefab, transform.position, Quaternion.identity); // Crea una explosión en la posición del jugador

        // --- Efectos sobre el atacante (empuje y aturdimiento) ---
        if (attacker != null)
        {
            // Intentamos obtener el script GoblinScript (u otro con el método ApplyParryEffects)
            GoblinScript goblin = attacker.GetComponent<GoblinScript>();
            if (goblin != null)
            {
                // Calculamos la dirección del empuje: desde el jugador hacia el enemigo (alejándolo)
                Vector2 pushDirection = (attacker.transform.position - transform.position).normalized;
                // Llamamos al método público del enemigo que aplica el empuje y el aturdimiento
                goblin.ApplyParryEffects(pushDirection, ParryPushForce, ParryStunDuration);
            }
        }
    }

    // ------------------------------------------------------------
    // MÉTODOS PARA RECIBIR DAÑO (Hit)
    // ------------------------------------------------------------

    /// <summary>
    /// Versión simple de recibir daño (sin dirección ni atacante conocido).
    /// Si está en parry, bloquea el daño y lanza OnParrySuccess sin referencia a atacante.
    /// </summary>
    public void Hit()
    {
        if (!isAlive) return; // No recibe daño si ya está muerto

        // Si está en ventana de parry, anula el daño y ejecuta la lógica de parry exitoso
        if (isParrying)
        {
            OnParrySuccess(null);
            return;
        }

        if (IsInvulnerable) return; // No recibe daño si es invulnerable

        Health -= 1;
        StartCoroutine(TemporaryDamageFlag()); // Activa la bandera de daño para la animación
        if (Health <= 0) Die();
    }

    /// <summary>
    /// Versión completa de recibir daño: con dirección, cantidad de daño, multiplicador de knockback,
    /// y referencia opcional al atacante (para poder aplicar efectos de parry).
    /// </summary>
    /// <param name="direction">Dirección desde la que proviene el golpe.</param>
    /// <param name="damage">Cantidad de puntos de daño.</param>
    /// <param name="forceMultiplier">Multiplicador adicional para el knockback.</param>
    /// <param name="attacker">GameObject del enemigo que ataca (opcional, por defecto null).</param>
    public void Hit(Vector2 direction, int damage, float forceMultiplier = 1f, GameObject attacker = null)
    {
        if (!isAlive) return;

        // Bloqueo por parry: niega el daño, lanza éxito y termina el método
        if (isParrying)
        {
            OnParrySuccess(attacker);
            return;
        }

        if (IsInvulnerable) return;

        Health -= damage;
        if (Health <= 0)
        {
            Die();
            return;
        }

        // Aplicar knockback (empuje al recibir el golpe)
        isDamaged = true;
        if (rb != null)
        {
            float horizSign = Mathf.Sign(direction.x); // Signo de la dirección horizontal
            // Fuerza horizontal: la máxima entre un valor base y la influencia de la dirección real
            float horizForce = Mathf.Max(BaseKnockback * 0.6f * forceMultiplier, Mathf.Abs(direction.x) * BaseKnockback * forceMultiplier);
            float vertForce = BaseKnockback * 0.5f * forceMultiplier; // Componente vertical del knockback

            // Establecemos velocidad directamente (más reactivo) y añadimos algo de impulso adicional
            rb.linearVelocity = new Vector2(horizSign * horizForce, vertForce);
            rb.AddForce(new Vector2(horizSign * horizForce * 0.3f, vertForce * 0.5f), ForceMode2D.Impulse);
        }

        StartCoroutine(TemporaryDamageFlag());
    }

    // ------------------------------------------------------------
    // MUERTE DEL JUGADOR
    // ------------------------------------------------------------
    private void Die()
    {
        isAlive = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;   // Detenemos cualquier movimiento residual
            rb.angularVelocity = 0f;
        }

        SetAnimatorBoolSafe("Muerto", true);    // Activamos la animación de muerte si existe
        Debug.Log("Red: ha muerto.");
    }

    // ------------------------------------------------------------
    // CORRUTINA DE BANDERA DE DAÑO (para la animación de "Damage")
    // ------------------------------------------------------------
    private IEnumerator TemporaryDamageFlag()
    {
        SetAnimatorBoolSafe("Damage", true);
        yield return new WaitForSeconds(0.25f);   // La bandera se mantiene 0.25 segundos
        isDamaged = false;
        SetAnimatorBoolSafe("Damage", false);
    }

    // ------------------------------------------------------------
    // INVULNERABILIDAD TEMPORAL
    // ------------------------------------------------------------
    /// <summary>
    /// Activa la invulnerabilidad durante la cantidad de segundos especificada.
    /// </summary>
    public void SetInvulnerable(float duration)
    {
        StopCoroutine(nameof(InvulnerabilityTimer));
        StartCoroutine(InvulnerabilityTimer(duration));
    }

    private IEnumerator InvulnerabilityTimer(float duration)
    {
        IsInvulnerable = true;
        SetAnimatorBoolSafe("Invulnerable", true);
        yield return new WaitForSeconds(duration);
        IsInvulnerable = false;
        SetAnimatorBoolSafe("Invulnerable", false);
    }

    // ------------------------------------------------------------
    // UTILIDADES PARA EL ANIMATOR
    // ------------------------------------------------------------
    /// <summary>
    /// Comprueba si el Animator tiene un parámetro booleano con el nombre dado.
    /// </summary>
    private bool AnimatorHasBool(string name)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
            if (p.name == name && p.type == AnimatorControllerParameterType.Bool) return true;
        return false;
    }

    /// <summary>
    /// Establece un parámetro booleano en el Animator, pero solo si existe (evita errores).
    /// </summary>
    private void SetAnimatorBoolSafe(string name, bool value)
    {
        if (AnimatorHasBool(name)) animator.SetBool(name, value);
    }
}