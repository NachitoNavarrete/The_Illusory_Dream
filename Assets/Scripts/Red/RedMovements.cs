using System.Collections;
using UnityEngine;

/// <summary>
/// Control principal del jugador y protagonista "Red".
/// Maneja movimiento horizontal, salto, dash, disparo, sistema de vida y daño,
/// partículas para el salto y un sistema de parry que empuja y aturde al enemigo.
/// </summary>
// - Este script controla al personaje principal (Red): moverse, saltar, dash, disparar,
//   recibir daño y hacer parry. Ajusta los valores en el Inspector para cambiar su comportamiento.
// - Piensa en él como el "cerebro" del jugador que recibe teclas y manda acciones.
public class RedMovement : MonoBehaviour
{
    //Nueva variable para añadir sonidos al jugador.
    public RedSoundController RedSoundController;
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

        // --- OBTENER CONTROLADOR DE SONIDOS ---
        // Si no se asignó manualmente en el Inspector, lo buscamos automáticamente
        // en el mismo GameObject o en sus hijos
        if (RedSoundController == null)
        {
            RedSoundController = GetComponent<RedSoundController>();
            if (RedSoundController == null)
                Debug.LogWarning("RedMovement: no se encontró RedSoundController. Los sonidos no funcionarán. Asígnalon en el Inspector o agrega el script al mismo GameObject.");
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

        // --- 1) MOVIMIENTO HORIZONTAL Y FLIP DEL SPRITE ---
        // Leemos el eje horizontal (teclas A/D o flechas izquierda/derecha)
        // GetAxisRaw devuelve -1 (izq), 0 (nada), 1 (dch) — sin suavizado
        horizontal = Input.GetAxisRaw("Horizontal");
        // Volteamos el sprite según la dirección:
        // - Si va a la izquierda (< 0): escala X = -1 (invertido)
        // - Si va a la derecha (> 0): escala X = 1 (normal)
        if (horizontal < 0f) transform.localScale = new Vector3(-1f, 1f, 1f);
        else if (horizontal > 0f) transform.localScale = new Vector3(1f, 1f, 1f);

        // --- 2) SONIDO DE CAMINAR ---
        // Si se está moviendo horizontalmente Y está en el suelo:
        // reproducimos el sonido de pasos (cada frame mientras camina)
        // SEGURIDAD: comprueba que RedSoundController existe antes de usarlo
        if (horizontal != 0f && grounded && RedSoundController != null)
        {
            RedSoundController.playCaminar();
        }

        // --- 3) DETECTAR SI ESTÁ EN EL SUELO ---
        // Usa raycasts para comprobar si hay algo debajo del jugador
        UpdateGrounded();

        // --- 4) SINCRONIZAR PARÁMETROS DEL ANIMATOR ---
        // Estos métodos actualizan los bools del Animator solo si existen, sin causar errores
        // Cada parámetro controla una animación diferente:
        SetAnimatorBoolSafe("isRunning", horizontal != 0f);     // True si se mueve, False si está quieto
        SetAnimatorBoolSafe("isGrounded", grounded);            // True si toca el suelo, False si está en el aire
        SetAnimatorBoolSafe("isJumping", !grounded);            // True si salta (opuesto a grounded)
        SetAnimatorBoolSafe("Damage", isDamaged);               // True cuando recibe daño (animación de golpe)
        SetAnimatorBoolSafe("Muerto", !isAlive);                // True cuando muere (animación final)

        // --- 5) SALTO (TECLA Z) ---
        // Solo salta si:
        // - Presiona Z en este frame
        // - Está en el suelo (grounded = true)
        // - NO está en invulnerabilidad (IsInvulnerable = false)
        if (Input.GetKeyDown(KeyCode.Z) && grounded && !IsInvulnerable)
        {
            Jump();  // Aplica fuerza hacia arriba + sonido + partículas
        }

        // --- 6) DASH (TECLA SHIFT CONFIGURADA) ---
        // Solo activa dash si:
        // - Presiona la tecla (configurada por defecto como LeftShift)
        // - Ha pasado el cooldown (Time.time >= nextDashTime)
        // - No está ya dasheando en este momento (!isDashing)
        if (Input.GetKeyDown(DashKey) && Time.time >= nextDashTime && !isDashing)
        {
            StartDash();  // Activa el dash y configura el tiempo de fin
        }

        // --- 7) TERMINAR EL DASH CUANDO SE ACABA SU DURACIÓN ---
        // Si está dasheando y pasó el tiempo máximo del dash, lo termina
        if (isDashing && Time.time >= dashEndTime)
        {
            EndDash();  // Desactiva el dash y frena el movimiento horizontal
        }

        // --- 8) DISPARO (TECLA X) ---
        // Crea una bala cuando presiona X
        // La dirección depende de las flechas (arriba/abajo/lateral)
        if (Input.GetKeyDown(KeyCode.X))
        {
            Shoot();  // Instancia bala + sonido
        }

        // --- 9) PARRY / BLOQUEO (TECLA C CONFIGURADA) ---
        // Abre la "ventana de parry" si:
        // - Presiona C
        // - Ha pasado el cooldown (Time.time >= nextParryTime)
        // - No está ya en parry (!isParrying)
        // Durante esta ventana, los golpes se bloquean y rebotan en el enemigo
        if (Input.GetKeyDown(ParryKey) && Time.time >= nextParryTime && !isParrying)
        {
            StartParry();  // Abre la ventana de bloqueo
        }

        // --- 10) TERMINAR PARRY CUANDO SE ACABA SU VENTANA ---
        // Si está en parry y pasó el tiempo máximo, cierra la ventana
        // A partir de entonces vuelve a recibir daño normal
        if (isParrying && Time.time >= parryEndTime)
        {
            EndParry();  // Cierra la ventana de bloqueo
        }
    }

    // ------------------------------------------------------------
    // MÉTODO FIXEDUPDATE: física (movimiento horizontal constante)
    // ------------------------------------------------------------
    /// <summary>
    /// Se ejecuta cada tiempo físico fijo (por defecto 50 veces por segundo).
    /// Aquí es donde aplicamos fuerzas y velocidades al Rigidbody2D.
    /// </summary>
    private void FixedUpdate()
    {
        // Seguridad 1: Si está muerto, no hay movimiento
        if (!isAlive) return;
        // Seguridad 2: Si está en dash, el movimiento lo controla StartDash() directamente
        if (isDashing) return;

        // --- MOVIMIENTO HORIZONTAL CONSTANTE ---
        // Multiplicamos 'horizontal' (-1, 0, 1) por la velocidad configurada
        // Ejemplo: si horizontal = 1 y Speed = 5, entonces linearVelocity.x = 5
        // Mantenemos la velocidad Y (para que la gravedad siga funcionando, saltos, caídas, etc.)
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
        bool wasGrounded = grounded; // Guardamos el estado anterior
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
        //agregar sonido de caida
        if (grounded && !wasGrounded && rb.linearVelocity.y < 0)
        {
            RedSoundController.playCaida();
        }
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
    /// añade un impulso hacia arriba, reproduce el sonido y la partícula de salto.
    /// ANALOGÍA MANZANA: Si una manzana cae, gana velocidad hacia abajo. 
    /// Si quieres que REBOTE igual cada vez, debes quitarle esa velocidad acumulada antes de empujarla arriba.
    /// </summary>
    private void Jump()
    {
        // Seguridad: si no hay Rigidbody2D, no podemos hacer nada
        if (rb == null) return;

        // --- PASO 1: RESETEAR LA VELOCIDAD VERTICAL ---
        // Importante: ponemos Y = 0 para que el salto sea PREDECIBLE y CONSISTENTE
        // Si NO lo hicimos, la gravedad acumulada reduciría la altura del salto
        // EJEMPLO CON PERA: 
        //   - Pera cae desde arriba: vel.y = -10 (muy rápido hacia abajo)
        //   - Sin reset: AddForce suma a -10 = salto débil
        //   - Con reset: vel.y = 0, luego AddForce = salto fuerte y predecible
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        
        // --- PASO 2: REPRODUCIR SONIDO DE SALTO ---
        // El jugador escucha un "whoosh" cuando salta
        // SEGURIDAD: comprueba que RedSoundController existe antes de usarlo
        if (RedSoundController != null)
            RedSoundController.playSaltar();
        
        // --- PASO 3: APLICAR FUERZA HACIA ARRIBA ---
        // AddForce con ForceMode2D.Impulse = fuerza INSTANTÁNEA (todo de una vez)
        // Es como un martillo que golpea: da un impulso fuerte y rápido
        // Vector2.up = (0, 1) hacia arriba
        // JumpForce = 6, así que (0, 1) * 6 = (0, 6) de fuerza hacia arriba
        rb.AddForce(Vector2.up * JumpForce, ForceMode2D.Impulse);

        // --- PASO 4: REPRODUCIR PARTÍCULAS DE SALTO ---
        // Si asignaste un ParticleSystem en el Inspector, crea un efecto visual bonito
        // Por ejemplo: una nube de polvo, chispitas, estrellas, etc.
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
    /// ANALOGÍA MANZANA: Una manzana es "disparada" desde tu mano. La velocidad y dirección
    /// dependen de cómo apuntes. Si no apuntas (teclas de flecha), usa la dirección que miras.
    /// </summary>
    private void Shoot()
    {
        // Seguridad: si no hay prefab de bala, no dispares
        if (BulletPrefab == null) return;

        // --- DETERMINAR LA DIRECCIÓN DE DISPARO ---
        // Prioridad:
        // 1. Si presionas flecha ARRIBA -> dispara arriba
        // 2. Si presionas flecha ABAJO (y estás en el aire) -> dispara abajo
        // 3. Si no presionas flechas -> dispara horizontal (según hacia dónde mira el sprite)
        Vector3 aim;
        if (Input.GetKey(KeyCode.UpArrow)) 
            aim = Vector2.up;  // Arriba
        else if (Input.GetKey(KeyCode.DownArrow) && !grounded) 
            aim = Vector2.down;  // Abajo solo si está en el aire (no puedes disparar al piso si estás en el piso)
        else 
            aim = transform.localScale.x == 1f ? Vector2.right : Vector2.left;  // Izq o Derecha según sprite
        
        // --- REPRODUCIR SONIDO DE DISPARO ---
        // El jugador escucha "pum" cuando dispara
        // SEGURIDAD: comprueba que RedSoundController existe antes de usarlo
        if (RedSoundController != null)
            RedSoundController.playDisparo();

        // --- INSTANCIAR (crear) LA BALA ---
        // Instantiate = crear un clon del prefab en el juego
        // transform.position = donde estoy yo (Red)
        // + (Vector3)aim * BulletSpawnOffset = desplazado en la dirección (para que no empiece dentro del jugador)
        // Quaternion.identity = sin rotación (está al ángulo normal)
        GameObject bullet = Instantiate(BulletPrefab, transform.position + (Vector3)aim * BulletSpawnOffset, Quaternion.identity);
        
        // --- CONFIGURAR LA BALA ---
        // Obtenemos el script Bullet del GameObject que acabamos de crear
        var b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            b.SetDirection(aim);              // Le decimos a la bala en qué dirección moverse
            b.SetOwner(gameObject);           // Le decimos quién la disparó (para ignorar colisiones con RED)
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
        // Reproducir sonido de parry en el jugador (si existe controlador de sonido)
        if (RedSoundController != null)
            RedSoundController.playParry();
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
        // --- SEGURIDAD: Verificaciones antes de procesar ---
        if (!isAlive) return; // No recibe daño si ya está muerto
        
        // --- REPRODUCIR SONIDO DE DAÑO ---
        // Escucha un "ow" cuando recibe golpe
        // SEGURIDAD: comprueba que RedSoundController existe antes de usarlo
        if (RedSoundController != null)
            RedSoundController.playRecibirDamage();

        // --- BLOQUEO POR PARRY ---
        // Si está en la ventana de parry (bloqueando), anula el daño completamente
        // y ejecuta los efectos de parry exitoso (empuje, partículas, etc.)
        if (isParrying)
        {
            OnParrySuccess(null);  // null = no sabemos quién atacó (hit sin dirección)
            return;  // Termina aquí: NO recibe daño ni pierde vida
        }

        // --- BLOQUEO POR INVULNERABILIDAD ---
        // Si es invulnerable (ej. tras recibir un golpe o power-up), también ignora el daño
        if (IsInvulnerable) return;

        // --- RESTAR 1 PUNTO DE VIDA ---
        // Cada golpe sin bloqueo resta 1 de vida
        Health -= 1;
        
        // --- ACTIVAR ANIMACIÓN DE DAÑO ---
        // Reproduce la animación de "ow" o parpadeo durante 0.25 segundos
        StartCoroutine(TemporaryDamageFlag());
        
        // --- VERIFICAR SI MURIÓ ---
        // Si la vida llega a 0 o menos, el jugador muere
        if (Health <= 0) 
            Die();
    }

    /// <summary>
    /// Versión COMPLETA de recibir daño: con dirección, cantidad de daño, multiplicador de knockback,
    /// y referencia opcional al atacante (para poder aplicar efectos de parry).
    /// ANALOGÍA PERA: Si alguien te lanza una pera desde la derecha, debes volar hacia la izquierda
    /// (knockback). Si la recibes bloqueando (parry), la pera rebota hacia quien la lanzó.
    /// </summary>
    /// <param name="direction">Dirección desde la que proviene el golpe (vector normalizado).</param>
    /// <param name="damage">Cantidad de puntos de daño a restar (ej. 1, 2, 3 puntos).</param>
    /// <param name="forceMultiplier">Multiplicador adicional para el knockback (ej. 1.5x más fuerte).</param>
    /// <param name="attacker">GameObject del enemigo que ataca (opcional, por defecto null). Para parry.</param>
    public void Hit(Vector2 direction, int damage, float forceMultiplier = 1f, GameObject attacker = null)
    {
        // --- SEGURIDAD: Si ya está muerto, ignora todo ---
        if (!isAlive) return;

        // --- BLOQUEO POR PARRY ---
        // Si está en la ventana de parry (bloqueando), anula el daño Y empuja al atacante
        if (isParrying)
        {
            OnParrySuccess(attacker);  // Activa efectos: empuje al enemigo, partículas, sonido, etc.
            return;  // Termina aquí: NO recibe daño
        }

        // --- BLOQUEO POR INVULNERABILIDAD ---
        if (IsInvulnerable) return;

        // --- RESTAR DAÑO A LA VIDA ---
        Health -= damage;
        if (RedSoundController != null)
            RedSoundController.playRecibirDamage();
        // Ejemplo: damage = 2, entonces Health = Health - 2

        // --- VERIFICAR SI MURIÓ ---
        // Si la vida llegó a 0 o menos, ya no puede seguir peleando
        if (Health <= 0)
        {
            Die();  // Muere: reproduce sonido, animación, y detiene todo
            return;  // Termina aquí: NO aplica knockback si está muerto
        }

        // --- APLICAR KNOCKBACK (EMPUJE) ---
        // Solo si sigue vivo: lo empuja en la dirección contraria al golpe
        isDamaged = true;  // Bandera para la animación de "ow"
        if (rb != null)
        {
            // --- CALCULAR FUERZAS ---
            // horizSign = el signo (+1 derecha, -1 izquierda) de la dirección del golpe
            // Si el golpe viene desde la derecha (+X), rebotamos hacia izquierda (-X)
            float horizSign = Mathf.Sign(direction.x);
            
            // horizForce = fuerza horizontal final
            // Usa el máximo entre:
            // - Valor base: BaseKnockback * 0.6 * multiplicador
            // - Valor dirigido: dirección.x * BaseKnockback * multiplicador
            // (para que golpes más fuertes causen más knockback)
            float horizForce = Mathf.Max(BaseKnockback * 0.6f * forceMultiplier, Mathf.Abs(direction.x) * BaseKnockback * forceMultiplier);
            
            // vertForce = fuerza vertical
            // Siempre es menor que horizontal (para no volar demasiado arriba)
            // BaseKnockback * 0.5 * multiplicador
            float vertForce = BaseKnockback * 0.5f * forceMultiplier;

            // --- APLICAR FUERZAS AL RIGIDBODY ---
            // Método 1: Fijar velocidad directamente (rápido y reactivo)
            rb.linearVelocity = new Vector2(horizSign * horizForce, vertForce);
            
            // Método 2: Añadir un impulso adicional (para más "punch")
            // (horizSign * horizForce * 0.3f) = 30% del knockback horizontal
            // (vertForce * 0.5f) = 50% del knockback vertical
            rb.AddForce(new Vector2(horizSign * horizForce * 0.3f, vertForce * 0.5f), ForceMode2D.Impulse);
        }

        // --- ACTIVAR ANIMACIÓN DE DAÑO ---
        // Reproduce la animación de "ow" durante 0.25 segundos
        StartCoroutine(TemporaryDamageFlag());
    }

    // ------------------------------------------------------------
    // MUERTE DEL JUGADOR
    // ------------------------------------------------------------
    // ------------------------------------------------------------
    // MUERTE DEL JUGADOR
    // ------------------------------------------------------------
    private void Die()
    {
        // Indicador: el jugador ya no está vivo
        isAlive = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }

        // Detener cualquier movimiento residual (para que no se vea raro):
        // - linearVelocity = (0, 0) → para todas las velocidades (arriba/abajo/derecha/izquierda)
        // - angularVelocity = 0f → detiene rotaciones si las hubiera
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // --- SONIDO DE MUERTE ---
        // Reproducimos el sonido de muerte cuando muere
        RedSoundController.playMuerte();

        // --- ACTIVAR ANIMACIÓN DE MUERTE ---
        // Si existe el parámetro "Muerto" en el Animator, lo ponemos en true
        // Esto le dice al Animator que reproduzca la animación de muerte
        SetAnimatorBoolSafe("Muerto", true);
        
        // Debug: mensaje en consola para confirmar que murió
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