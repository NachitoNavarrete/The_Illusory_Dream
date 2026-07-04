using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum RobotBossState
{
    Inactive,
    IntroDialogue,
    Chase,
    Dodge,
    FinalBattle,
    OutroDialogue,
    Dead
}

public class RobotBoss : MonoBehaviour
{
    [Header("State")]
    public RobotBossState currentState = RobotBossState.Inactive;

    [Header("Referencias")]
    public GameObject Red;
    public BossHealthBar bossHealthBar;
    public float VisionRange = 15f;
    
    [Header("Stats")]
    public int Health = 50;
    public int MaxHealth = 50;
    public int MeleeDamage = 5;
    [Tooltip("Daño que hace el golpe cuerpo a cuerpo en Fase 2 (tierra). Usar un valor bajo para evitar instakill")]
    public int MeleeDamagePhase2 = 1;
    
    [Header("Music")]
    public AudioClip automataMusic;
    public AudioClip destructorMusic;

    [Header("Prefabs")]
    public GameObject bulletPrefab;
    public GameObject barrierPrefab;
    public GameObject GruntEnemyPrefab;
    public GameObject RobotEnemyPrefab;

    [Header("Chase Phase")]
    public float chaseDistance = 8f;
    public float chaseSpeed = 6f;
    public float dodgeInterval = 12f;
    private float nextDodgeTime;

    [Header("Dodge Phase")]
    public float dodgeDuration = 8f;

    [Header("Final Battle")]
    public float battleStartX = 50f;
    public float meleeCooldown = 3f;
    private float lastMeleeTime = -999f;

    [Header("Final Battle Phases")]
    public float attackPhaseDuration = 8f;
    public float restPhaseDuration = 4f;
    private float finalBattlePhaseTimer = 0f;
    private bool finalBattleIsResting = false;

    [Header("Shooting Stance")]
    [Tooltip("El boss se queda quieto mientras dispara para que sea más legible.")]
    public float shootStanceDuration = 1.1f;
    private bool isShootingStance = false;
    private float shootStanceEndTime = 0f;

    [Header("Final Battle Trigger")]
    [SerializeField] private Transform finalBattleMarker;

    [Header("Goblin Waves")]
    [SerializeField] private GameObject goblinPrefab;
    [SerializeField] private GameObject healthDropPrefab;
    [SerializeField] private float goblinWaveInterval = 12f;
    [SerializeField] private int goblinsPerWave = 3;
    private Coroutine goblinWaveRoutine;

    [Header("Phase 2 (Ground)")]
    public bool isPhase2 = false;
    private bool isTransitioning = false;
    public float groundSpeed = 3.5f;

    [Header("Bullet Hell Scale")]
    public float bulletScale = 7f;

    // Internals
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isDead = false;
    private GameObject dialoguePanel;
    private GameObject correPanel;
    private TMPro.TextMeshProUGUI dialogueText;
    private string[] outroDialogueLines = {
        "Así que has pasado la prueba...",
        "Interesante...",
        "Pronto nos veremos, viajero...",
        "Conocerás qué es este mundo en el siguiente camino..."
    };
    private int outroDialogueIndex = 0;
    private Coroutine activeRoutine;
    private List<GameObject> activeBarriers = new List<GameObject>();
    private bool hasStartedChase = false;

    [Header("Bullet Hell")]
    public Sprite[] bulletSprites;
    private float lastPatternTime;
    public float patternInterval = 3f;

    // Dodge triggers during Chase
    private bool trigger450Done = false;
    private bool trigger250Done = false;

    // Respawn system
    private struct EnemySpawnData
    {
        public Vector3 position;
        public bool isRobot;
    }
    private List<EnemySpawnData> enemySpawns = new List<EnemySpawnData>();

    [Header("End Game Portal")]
    public Sprite portalSprite;

    // Referencias a las paredes de la arena final para poder abrirlas al terminar la batalla.
    private List<GameObject> finalArenaWalls = new List<GameObject>();
    // Altura del suelo capturada al pasar a fase 2 (evita que el boss atraviese el piso).
    private float phase2GroundY = 0f;

    private void Awake()
    {
        // Cargar los sprites de las balas desde Resources para que sean VISIBLES tanto en el
        // Editor como en la build. AssetDatabase no existe en la build, por eso antes las balas
        // salian invisibles (parecia que "no caian") durante la persecucion/atrape.
        bulletSprites = Resources.LoadAll<Sprite>("VFX/BulletsRobotBoss");

        #if UNITY_EDITOR
        // Fallback solo-editor si por algun motivo no estan en Resources.
        if (bulletSprites == null || bulletSprites.Length == 0)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/SpriteSheets/enemy/BulletsRobotBoss.png");
            List<Sprite> sList = new List<Sprite>();
            foreach (var a in assets) if (a is Sprite s) sList.Add(s);
            bulletSprites = sList.ToArray();
        }
        #endif
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (Red == null)
        {
            Red = GameObject.Find("Red_0");
            if (Red == null) Red = GameObject.Find("Red");
        }

        if (bossHealthBar == null) bossHealthBar = GetComponent<BossHealthBar>();
        if (bossHealthBar != null) bossHealthBar.Hide();

        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Save initial positions of all enemies in the scene to revive them
        var grunts = GameObject.FindObjectsByType<GruntEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var robots = GameObject.FindObjectsByType<RobotEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var g in grunts)
        {
            enemySpawns.Add(new EnemySpawnData { position = g.transform.position, isRobot = false });
        }
        foreach (var r in robots)
        {
            if (r.gameObject != gameObject)
            {
                enemySpawns.Add(new EnemySpawnData { position = r.transform.position, isRobot = true });
            }
        }

        // Auto-load prefabs and music if null
        #if UNITY_EDITOR
        if (automataMusic == null) automataMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Music/level 2/Automata.mp3");
        if (destructorMusic == null) destructorMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Music/level 2/Destructor.mp3");
        if (GruntEnemyPrefab == null) GruntEnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animations/Prefabs/grunt/grunt.prefab");
        if (RobotEnemyPrefab == null) RobotEnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animations/Prefabs/RobotEnemy/RobotEnemy.prefab");
        if (bulletPrefab == null) bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animations/Prefabs/weapon_bullet_0.prefab");
        if (barrierPrefab == null) barrierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnergyBarrier.prefab");
        if (portalSprite == null) portalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SpriteSheets/enemy/grunt_portal.png");
        #endif

        // REFIGHT: si el jugador murió durante la batalla final, al recargar la escena
        // se salta la persecución y se empieza directamente en la batalla final (arena)
        // con el combate andando inmediatamente.
        if (PlayerPrefs.GetInt("Nivel2RefightBoss", 0) == 1)
        {
            float arenaX = finalBattleMarker != null ? finalBattleMarker.position.x : 20.0f;
            float baseY = Red != null ? Red.transform.position.y : 0.4f;
            transform.position = new Vector3(arenaX, baseY + 7f, 0f);
            StartFinalBattle();
        }
        else if (PlayerPrefs.GetInt("Nivel2ChaseStarted", 0) == 1 && PlayerPrefs.GetInt("Nivel2ChaseCompleted", 0) == 0)
        {
            // RESUME CHASE: si la persecución estaba activa pero no terminada, posicionar al jefe
            // cerca del jugador y reanudar la huida inmediatamente.
            if (Red != null)
            {
                transform.position = Red.transform.position + new Vector3(chaseDistance, 4f, 0f);
            }
            StartChasePhase();
        }
    }

    private void Update()
    {
        if (isDead) return;

        if (animator != null)
        {
            if (isPhase2)
            {
                animator.SetBool("IsFlying", false);
            }
            else
            {
                bool isMoving = rb.linearVelocity.magnitude > 0.1f || currentState == RobotBossState.Chase || currentState == RobotBossState.Dodge;
                animator.SetBool("IsFlying", isMoving);
            }
        }

        switch (currentState)
        {
            case RobotBossState.Inactive:
                if (Red != null && Vector2.Distance(transform.position, Red.transform.position) < VisionRange)
                {
                    StartIntro();
                }
                break;
            case RobotBossState.IntroDialogue:
                if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
                {
                    EndIntro();
                }
                break;
            case RobotBossState.Chase:
                HandleChase();
                ClampToScreen();
                break;
            case RobotBossState.Dodge:
                // Handled in coroutine
                ClampToScreen();
                break;
            case RobotBossState.FinalBattle:
                if (isPhase2)
                {
                    HandlePhase2Ground();
                }
                else
                {
                    HandleFinalBattle();
                }
                ClampToScreen();
                break;
            case RobotBossState.OutroDialogue:
                if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
                {
                    AdvanceOutroDialogue();
                }
                break;
        }
    }

    private void ClampToScreen()
    {
        if (Camera.main == null) return;
        Vector3 pos = Camera.main.WorldToViewportPoint(transform.position);
        pos.x = Mathf.Clamp(pos.x, 0.05f, 0.95f);
        pos.y = Mathf.Clamp(pos.y, 0.2f, 0.95f); // Stay above ground level
        transform.position = Camera.main.ViewportToWorldPoint(pos);
    }

    public void StartIntro()
    {
        currentState = RobotBossState.IntroDialogue;
        FreezePlayer(true);
        Time.timeScale = 0f; // Stop time scale during dialogues
        CreateDialogueUI("ENEMIGO! ELIMINAR!");
    }

    public void EndIntro()
    {
        if (dialoguePanel != null) Destroy(dialoguePanel);
        FreezePlayer(false);
        Time.timeScale = 1f; // Resume time scale
        StartChasePhase();
    }

    private void StartChasePhase()
    {
        currentState = RobotBossState.Chase;
        hasStartedChase = true;
        if (automataMusic != null) MusicManager.PlayBackgroundMusicStatic(true, automataMusic);
        
        // Save that chase has started
        PlayerPrefs.SetInt("Nivel2ChaseStarted", 1);
        PlayerPrefs.Save();

        // Immediately open all puzzle doors in the scene
        var doors = GameObject.FindObjectsByType<PuzzleDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var door in doors)
        {
            if (door != null)
            {
                door.isOpen = true;
            }
        }

        // Immediately destroy all explosive barriers in the scene
        var barriers = GameObject.FindObjectsByType<ExplosiveBarrier>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var barrier in barriers)
        {
            if (barrier != null)
            {
                barrier.Break();
            }
        }

        ShowCorreSign(true);
        SetRedSpeedMultiplier(1.5f); // Give Red x1.5 speed buff at start of chase

        ReviveEnemies();
        nextDodgeTime = Time.time + dodgeInterval;
    }

    private void HandleChase()
    {
        if (Red == null) return;
        
        float bossScale = 2.5f;
        transform.localScale = new Vector3(Red.transform.position.x > transform.position.x ? bossScale : -bossScale, bossScale, 1f);

        // Fly behind/above the player
        Vector3 targetPos = Red.transform.position + new Vector3(chaseDistance, 4f, 0f);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * chaseSpeed);

        // Check dodge triggers based on player x coordinate (player is running left)
        float px = Red.transform.position.x;
        if (px < 450f && !trigger450Done)
        {
            trigger450Done = true;
            StartDodgePhase();
            return;
        }
        if (px < 250f && !trigger250Done)
        {
            trigger250Done = true;
            StartDodgePhase();
            return;
        }

        // Check time-based dodge trigger (trap and shoot) to occur more frequently
        if (Time.time > nextDodgeTime)
        {
            StartDodgePhase();
            return;
        }

        // Bullet hell patterns (simpler during chase)
        if (Time.time > lastPatternTime + patternInterval)
        {
            StartCoroutine(SimpleChaseAttackRoutine());
            lastPatternTime = Time.time;
        }

        // Transition to Final Battle (bound to the mushroom sector marker at level start)
        if (finalBattleMarker != null)
        {
            if (Red.transform.position.x <= finalBattleMarker.position.x)
            {
                StartFinalBattle();
            }
        }
        else if (Red.transform.position.x < battleStartX)
        {
            StartFinalBattle();
        }
    }

    private IEnumerator SimpleChaseAttackRoutine()
    {
        // Just shoot a single targeted bullet at the player, wait a little bit, then another single targeted bullet.
        if (Red != null)
        {
            Vector2 dir = (Red.transform.position - transform.position).normalized;
            ShootBullet(dir);
        }
        yield return new WaitForSeconds(1.0f);
        if (Red != null)
        {
            Vector2 dir = (Red.transform.position - transform.position).normalized;
            ShootBullet(dir);
        }
    }

    private void SpawnRandomPattern()
    {
        // Más variedad de bullet hell (todos lentos y esquivables).
        int r = Random.Range(0, 7);
        switch (r)
        {
            case 0: StartCoroutine(SpiralPattern()); break;
            case 1: StartCoroutine(RainPattern()); break;
            case 2: StartCoroutine(SpreadPattern()); break;
            case 3: StartCoroutine(HighFireworkPattern()); break;
            case 4: StartCoroutine(RingWithGapPattern()); break;
            case 5: StartCoroutine(AimedBurstPattern()); break;
            default: StartCoroutine(DoubleSpiralPattern()); break;
        }
    }

    // Anillo con un hueco de seguridad: el jugador puede correr hacia el hueco.
    private IEnumerator RingWithGapPattern()
    {
        int count = 14;
        int gapStart = Random.Range(0, count);
        for (int i = 0; i < count; i++)
        {
            // Deja 3 huecos consecutivos como zona segura.
            if (i >= gapStart && i < gapStart + 3) continue;
            float angle = (360f / count) * i * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            FireBossBullet(dir, 4.5f, BulletPattern.Straight);
        }
        yield return null;
    }

    // Ráfaga dirigida: 3 disparos cortos apuntados al jugador con pequeño abanico.
    private IEnumerator AimedBurstPattern()
    {
        for (int shot = 0; shot < 3; shot++)
        {
            if (Red == null) yield break;
            Vector2 toPlayer = (Red.transform.position - transform.position).normalized;
            FireBossBullet(toPlayer, 6.5f, BulletPattern.Straight);
            FireBossBullet(Rotate(toPlayer, 14f * Mathf.Deg2Rad), 6.5f, BulletPattern.Straight);
            FireBossBullet(Rotate(toPlayer, -14f * Mathf.Deg2Rad), 6.5f, BulletPattern.Straight);
            yield return new WaitForSeconds(0.35f);
        }
    }

    // Doble espiral en sentidos opuestos (lenta y legible).
    private IEnumerator DoubleSpiralPattern()
    {
        for (int i = 0; i < 12; i++)
        {
            float a1 = i * 30f * Mathf.Deg2Rad;
            float a2 = -i * 30f * Mathf.Deg2Rad;
            FireBossBullet(new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)), 4.5f, BulletPattern.Straight);
            FireBossBullet(new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)), 4.5f, BulletPattern.Straight);
            yield return new WaitForSeconds(0.12f);
        }
    }

    private IEnumerator HighFireworkPattern()
    {
        // Fly high and shoot in all directions (menos balas, más separadas)
        for (int j = 0; j < 2; j++)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                FireBossBullet(dir, 4f, BulletPattern.Straight); // Slower for dodgeability
            }
            yield return new WaitForSeconds(0.6f);
        }
    }

    private IEnumerator SpiralPattern()
    {
        for (int i = 0; i < 10; i++)
        {
            float angle = i * 36f * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            FireBossBullet(dir, 5f, BulletPattern.Straight); // Fixed straight line, no corkscrew
            yield return new WaitForSeconds(0.15f);
        }
    }

    private IEnumerator RainPattern()
    {
        for (int i = 0; i < 6; i++)
        {
            Vector2 spawnPos = (Vector2)transform.position + new Vector2(Random.Range(-11f, 11f), 10f);
            FireBossBulletAt(spawnPos, Vector2.down, 5f, BulletPattern.Straight); // Slower
            yield return new WaitForSeconds(0.28f);
        }
    }

    private IEnumerator SpreadPattern()
    {
        Vector2 toPlayer = (Red.transform.position - transform.position).normalized;
        for (int i = -2; i <= 2; i++)
        {
            float angle = i * 24f * Mathf.Deg2Rad;
            Vector2 dir = Rotate(toPlayer, angle);
            FireBossBullet(dir, 6f, BulletPattern.Straight); // Menos balas, más separadas
        }
        yield return null;
    }

    private void FireBossBullet(Vector2 dir, float speed, BulletPattern pattern)
    {
        FireBossBulletAt(transform.position, dir, speed, pattern);
    }

    private void FireBossBulletAt(Vector2 pos, Vector2 dir, float speed, BulletPattern pattern)
    {
        GameObject bObj = new GameObject("BossBullet", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(BossBullet));
        bObj.transform.position = pos;
        bObj.transform.localScale = Vector3.one * bulletScale;
        var b = bObj.GetComponent<BossBullet>();
        var sprite = (bulletSprites != null && bulletSprites.Length > 0) ? bulletSprites[Random.Range(0, Mathf.Min(10, bulletSprites.Length))] : null;
        b.Setup(dir, speed, sprite, pattern, Red.transform);
        // Reducir daño de balas cuando el boss está en fase 2 (tierra), para evitar instakill
        if (isPhase2)
        {
            b.damage = 1f;
            // También podemos reducir la velocidad para hacerlas más esquivables
            b.speed = Mathf.Min(b.speed, 5f);
        }
        
        var col = bObj.GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        // Radio local menor porque la escala visual ahora es mayor: hitbox justo (~0.06 * scale).
        col.radius = 0.06f;
    }

    private Vector2 Rotate(Vector2 v, float delta)
    {
        return new Vector2(
            v.x * Mathf.Cos(delta) - v.y * Mathf.Sin(delta),
            v.x * Mathf.Sin(delta) + v.y * Mathf.Cos(delta)
        );
    }

    private void StartDodgePhase()
    {
        if (currentState == RobotBossState.Dead) return;
        
        RobotBossState previousState = currentState;
        currentState = RobotBossState.Dodge;

        ShowCorreSign(false);
        SetRedSpeedMultiplier(1.0f); // Deactivate speed boost when trapped

        activeRoutine = StartCoroutine(DodgeRoutine(previousState));
    }

    private IEnumerator DodgeRoutine(RobotBossState returnState)
    {
        CreateArenaBarriers();

        float timer = dodgeDuration;
        while (timer > 0)
        {
            // Hover further away: 12 units high and offset horizontally to stay back
            float sideOffset = (transform.position.x > Red.transform.position.x) ? 10f : -10f;
            Vector3 hoverPos = Red.transform.position + new Vector3(sideOffset, 12f, 0);
            transform.position = Vector3.MoveTowards(transform.position, hoverPos, Time.deltaTime * 8f);

            // Balas que caen desde arriba (menos densas y más separadas para poder esquivar)
            if (Time.frameCount % 34 == 0)
            {
                Vector2 spawnPos = new Vector2(Red.transform.position.x + Random.Range(-11f, 11f), Red.transform.position.y + 12f);
                FireBossBulletAt(spawnPos, Vector2.down, 4.5f, BulletPattern.Straight);
            }
            
            yield return null;
            timer -= Time.deltaTime;
        }

        foreach (var b in activeBarriers) if (b != null) Destroy(b);
        activeBarriers.Clear();

        currentState = returnState;
        nextDodgeTime = Time.time + dodgeInterval;

        // Reactivate speed boost and CORRE sign if resuming chase
        if (currentState == RobotBossState.Chase)
        {
            ShowCorreSign(true);
            SetRedSpeedMultiplier(1.5f);
        }
    }

    private void StartFinalBattle()
    {
        currentState = RobotBossState.FinalBattle;
        if (destructorMusic != null) MusicManager.PlayBackgroundMusicStatic(true, destructorMusic);

        // Arrancar limpio: sin postura de disparo, fase de ataque activa y esquive
        // programado con retraso para que el boss SEA vulnerable y no salte a Dodge al instante.
        isShootingStance = false;
        finalBattleIsResting = false;
        finalBattlePhaseTimer = 0f;
        lastPatternTime = Time.time;
        // Esquives menos frecuentes y más cortos durante la batalla final (era muy difícil).
        dodgeInterval = Mathf.Max(dodgeInterval, 22f);
        dodgeDuration = Mathf.Min(dodgeDuration, 4f);
        nextDodgeTime = Time.time + dodgeInterval;

        // Hard-stop the chase/dodge: kill any active routine and clear dodge barriers
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
        foreach (var b in activeBarriers) if (b != null) Destroy(b);
        activeBarriers.Clear();

        ShowCorreSign(false);
        SetRedSpeedMultiplier(1.0f); // Deactivate speed boost permanently for the final battle!

        // Save checkpoint flag: the chase has been completed (read by the checkpoint system)
        PlayerPrefs.SetInt("Nivel2ChaseCompleted", 1);
        PlayerPrefs.Save();

        // Begin spawning goblin waves for the duration of the final battle
        if (goblinWaveRoutine != null) StopCoroutine(goblinWaveRoutine);
        goblinWaveRoutine = StartCoroutine(GoblinWaveRoutine());

        if (bossHealthBar == null)
        {
            bossHealthBar = GameObject.FindAnyObjectByType<BossHealthBar>(FindObjectsInactive.Include);
        }

        if (bossHealthBar != null)
        {
            bossHealthBar.gameObject.SetActive(true); // Force object active
            bossHealthBar.Show();
            bossHealthBar.SetHealth(Health, MaxHealth);
            Debug.Log("Boss Health Bar shown!");
        }
        else
        {
            Debug.LogError("Boss Health Bar NOT FOUND!");
        }

        CreateFinalArenaWalls();
    }

    private IEnumerator GoblinWaveRoutine()
    {
        // Spawn goblin waves for as long as the final battle is ongoing
        while (currentState == RobotBossState.FinalBattle && !isDead)
        {
            SpawnGoblinWave();
            yield return new WaitForSeconds(goblinWaveInterval);
        }
        goblinWaveRoutine = null;
    }

    private void SpawnGoblinWave()
    {
        if (goblinPrefab == null) return;

        // Anchor the wave to the arena marker if present, otherwise to the boss position
        Vector3 anchor = finalBattleMarker != null ? finalBattleMarker.position : transform.position;
        float groundY = Red != null ? Red.transform.position.y : anchor.y;

        for (int i = 0; i < goblinsPerWave; i++)
        {
            // Alternate spawning near left/right arena edges with a small horizontal offset
            float side = (i % 2 == 0) ? -1f : 1f;
            float edgeOffset = 10f + (i / 2) * 2f;
            Vector3 spawnPos = new Vector3(anchor.x + side * edgeOffset, groundY, 0f);

            GameObject g = Instantiate(goblinPrefab, spawnPos, Quaternion.identity);
            g.SetActive(true);

            // GoblinScript does NOT auto-find Red, so inject the boss's Red reference
            var gob = g.GetComponent<GoblinScript>();
            if (gob != null)
            {
                gob.Red = Red;
                // Set health drop chance to 70%
                if (healthDropPrefab != null)
                {
                    gob.lootTable.Clear();
                    gob.lootTable.Add(new Loot { itemPrefab = healthDropPrefab, dropChance = 70f });
                }
            }
        }
    }

    private void HandleFinalBattle()
    {
        if (Red == null || isTransitioning) return;

        float bossScale = 2.5f;
        transform.localScale = new Vector3(Red.transform.position.x > transform.position.x ? bossScale : -bossScale, bossScale, 1f);

        float distance = Vector2.Distance(transform.position, Red.transform.position);

        // Manage active attack and rest phases in final battle
        finalBattlePhaseTimer += Time.deltaTime;
        if (!finalBattleIsResting && finalBattlePhaseTimer >= attackPhaseDuration)
        {
            finalBattleIsResting = true;
            finalBattlePhaseTimer = 0f;
            spriteRenderer.color = new Color(0.6f, 0.8f, 1f, 1f); // cool blue indicating resting/vulnerability
        }
        else if (finalBattleIsResting && finalBattlePhaseTimer >= restPhaseDuration)
        {
            finalBattleIsResting = false;
            finalBattlePhaseTimer = 0f;
            spriteRenderer.color = Color.white; // return to normal
        }

        if (finalBattleIsResting)
        {
            // Fase de descanso: el boss desciende a media altura, vulnerable, sin disparar.
            Vector3 restPos = Red.transform.position + new Vector3(0f, 4.5f, 0f);
            transform.position = Vector3.MoveTowards(transform.position, restPos, Time.deltaTime * 3f);
            return;
        }

        // FASE 1 = COMBATE AÉREO (bullet hell). El boss vuela alto sobre el jugador y,
        // cuando dispara, se queda quieto para que el patrón sea legible y esquivable.
        if (isShootingStance)
        {
            // Quieto en el aire mientras dispara.
            if (Time.time >= shootStanceEndTime) isShootingStance = false;
            return;
        }

        // Flotar alto sobre el jugador, barriendo de lado a lado (esquivable).
        float hoverHeight = 7.5f;
        float sweep = Mathf.Sin(Time.time * 0.9f) * 6f;
        Vector3 hoverPos = new Vector3(Red.transform.position.x + sweep, Red.transform.position.y + hoverHeight, 0f);
        transform.position = Vector3.MoveTowards(transform.position, hoverPos, Time.deltaTime * 5f);

        // Disparar un patrón de bullet hell por intervalos. Al disparar, se detiene.
        if (Time.time > lastPatternTime + patternInterval)
        {
            lastPatternTime = Time.time;
            BeginShootStance();
            SpawnRandomPattern();
        }

        if (Time.time > nextDodgeTime)
        {
            StartDodgePhase();
        }
    }

    private void BeginShootStance()
    {
        isShootingStance = true;
        shootStanceEndTime = Time.time + shootStanceDuration;
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (animator != null) StartCoroutine(TriggerAnimation("IsShooting"));
    }

    private void MeleeSlam()
    {
        lastMeleeTime = Time.time;
        if (animator != null) StartCoroutine(TriggerAnimation("IsMelee"));
        StartCoroutine(MeleeRoutine());
    }

    private IEnumerator TriggerAnimation(string paramName)
    {
        animator.SetBool(paramName, true);
        yield return new WaitForSeconds(0.5f);
        animator.SetBool(paramName, false);
    }

    private IEnumerator MeleeRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = Red.transform.position;
        if (isPhase2)
        {
            targetPos.y = startPos.y; // keep ground level
        }
        
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.5f);
        
        float dashTime = 0.3f;
        float elapsed = 0f;
        while(elapsed < dashTime)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / dashTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (Vector2.Distance(transform.position, Red.transform.position) < 4f)
        {
            var redMov = Red.GetComponent<RedMovement>();
            if (redMov != null && !redMov.IsInvulnerable && !redMov.IsAdminModeActive)
            {
                // Ajustar daño y knockback en fase 2 para evitar instakill.
                int damageToApply = isPhase2 ? MeleeDamagePhase2 : MeleeDamage;
                float forceMultiplier = isPhase2 ? 1.5f : 3f;
                redMov.Hit((Red.transform.position - transform.position).normalized, damageToApply, forceMultiplier, gameObject, false);
            }
        }
        
        yield return new WaitForSeconds(0.5f);
        spriteRenderer.color = Color.white;
    }

    private void ShootBullet(Vector2 dir)
    {
        if (bulletPrefab == null) return;
        if (animator != null) StartCoroutine(TriggerAnimation("IsShooting"));
        GameObject bObj = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        bObj.transform.localScale = Vector3.one * 5f; // Larger bullet

        // Adjust the collider to make the hitboxes super fair and precise
        var col = bObj.GetComponent<CircleCollider2D>();
        if (col != null)
        {
            col.radius = 0.08f; // Precise hitbox relative to visual size
        }

        Bullet b = bObj.GetComponent<Bullet>();
        if (b != null)
        {
            b.SetDirection(dir);
            b.SetOwner(gameObject);
            b.Damage = 1;
            b.LifeTime = 10f; // Long range
            b.Speed = 7f; // Even slower for dodgeability
        }
        
        #if UNITY_EDITOR
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/disparo.mp3");
        if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position);
        #endif
    }

    private void ReviveEnemies()
    {
        int index = 0;
        foreach (var spawn in enemySpawns)
        {
            index++;
            if (spawn.isRobot)
            {
                // Revive only 1 out of 3 robots to make it cleaner
                if (index % 3 != 0) continue;
            }
            else
            {
                // Revive only 1 out of 5 grunts to significantly reduce density in chase
                if (index % 5 != 0) continue;
            }

            GameObject prefab = spawn.isRobot ? RobotEnemyPrefab : GruntEnemyPrefab;
            if (prefab != null)
            {
                GameObject newEnemy = Instantiate(prefab, spawn.position, Quaternion.identity);
                newEnemy.SetActive(true);
            }
        }
    }

    private void CreateArenaBarriers()
    {
        foreach (var b in activeBarriers) if (b != null) Destroy(b);
        activeBarriers.Clear();

        if (barrierPrefab != null)
        {
            GameObject b1 = Instantiate(barrierPrefab, Red.transform.position + new Vector3(-12f, 0, 0), Quaternion.identity);
            b1.name = "BossBarrier_Left";
            b1.transform.localScale = new Vector3(1.5f, 40f, 1f);
            activeBarriers.Add(b1);

            GameObject b2 = Instantiate(barrierPrefab, Red.transform.position + new Vector3(12f, 0, 0), Quaternion.identity);
            b2.name = "BossBarrier_Right";
            b2.transform.localScale = new Vector3(1.5f, 40f, 1f);
            activeBarriers.Add(b2);
        }
        else
        {
            GameObject b1 = new GameObject("BossBarrier_Left");
            b1.transform.position = Red.transform.position + new Vector3(-12f, 0, 0);
            b1.AddComponent<BoxCollider2D>().size = new Vector2(2, 40);
            activeBarriers.Add(b1);

            GameObject b2 = new GameObject("BossBarrier_Right");
            b2.transform.position = Red.transform.position + new Vector3(12f, 0, 0);
            b2.AddComponent<BoxCollider2D>().size = new Vector2(2, 40);
            activeBarriers.Add(b2);
        }
    }

    private void CreateFinalArenaWalls()
    {
        // Centrar la arena en el marcador (donde ocurre la batalla), no en battleStartX.
        float centerX = finalBattleMarker != null ? finalBattleMarker.position.x
                        : (Red != null ? Red.transform.position.x : transform.position.x);
        float centerY = Red != null ? Red.transform.position.y : 0f;
        float halfWidth = 16f;

        finalArenaWalls.Clear();

        if (barrierPrefab != null)
        {
            GameObject w1 = Instantiate(barrierPrefab, new Vector3(centerX - halfWidth, centerY, 0f), Quaternion.identity);
            w1.name = "FinalArena_Left";
            w1.transform.localScale = new Vector3(1.5f, 50f, 1f);
            finalArenaWalls.Add(w1);

            GameObject w2 = Instantiate(barrierPrefab, new Vector3(centerX + halfWidth, centerY, 0f), Quaternion.identity);
            w2.name = "FinalArena_Right";
            w2.transform.localScale = new Vector3(1.5f, 50f, 1f);
            finalArenaWalls.Add(w2);
        }
        else
        {
            GameObject w1 = new GameObject("FinalArena_Left");
            w1.transform.position = new Vector3(centerX - halfWidth, centerY, 0f);
            w1.AddComponent<BoxCollider2D>().size = new Vector2(2, 50);
            finalArenaWalls.Add(w1);

            GameObject w2 = new GameObject("FinalArena_Right");
            w2.transform.position = new Vector3(centerX + halfWidth, centerY, 0f);
            w2.AddComponent<BoxCollider2D>().size = new Vector2(2, 50);
            finalArenaWalls.Add(w2);
        }
    }

    // Abre (destruye) todas las barreras de la arena cuando la batalla termina.
    private void OpenAllBarriers()
    {
        foreach (var w in finalArenaWalls) if (w != null) Destroy(w);
        finalArenaWalls.Clear();
        foreach (var b in activeBarriers) if (b != null) Destroy(b);
        activeBarriers.Clear();
    }

    private void FreezePlayer(bool freeze)
    {
        if (Red == null) return;
        var move = Red.GetComponent<RedMovement>();
        if (move != null) move.enabled = !freeze;
        var rbP = Red.GetComponent<Rigidbody2D>();
        if (rbP != null) rbP.linearVelocity = Vector2.zero;
    }

    private void CreateDialogueUI(string text)
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        dialoguePanel = new GameObject("RobotBossDialogue", typeof(RectTransform));
        dialoguePanel.transform.SetParent(canvas.transform, false);
        var rect = dialoguePanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.8f);
        rect.anchorMax = new Vector2(0.5f, 0.8f);
        rect.sizeDelta = new Vector2(400f, 100f);
        
        var img = dialoguePanel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.8f);

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(dialoguePanel.transform, false);
        var t = textGo.AddComponent<TMPro.TextMeshProUGUI>();
        t.text = text;
        t.alignment = TMPro.TextAlignmentOptions.Center;
        t.color = Color.red;
        t.fontSize = 32;
        
        #if UNITY_EDITOR
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset");
        if (fontAsset != null) t.font = fontAsset;
        #endif

        dialogueText = t; // Save reference
    }

    private void StartOutroDialogue()
    {
        currentState = RobotBossState.OutroDialogue;
        FreezePlayer(true);
        Time.timeScale = 0f; // Stop time scale during dialogues
        
        // Hide sprite renderer so the boss appears destroyed
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        
        outroDialogueIndex = 0;
        CreateDialogueUI(outroDialogueLines[outroDialogueIndex]);
    }

    private void AdvanceOutroDialogue()
    {
        outroDialogueIndex++;
        if (outroDialogueIndex < outroDialogueLines.Length)
        {
            UpdateDialogueText(outroDialogueLines[outroDialogueIndex]);
        }
        else
        {
            EndOutroDialogueAndLoadMenu();
        }
    }

    private void UpdateDialogueText(string text)
    {
        if (dialogueText != null)
        {
            dialogueText.text = text;
        }
    }

    private void EndOutroDialogueAndLoadMenu()
    {
        if (dialoguePanel != null) Destroy(dialoguePanel);
        Time.timeScale = 1f; // Restore time scale
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }

    private void HandlePhase2Ground()
    {
        if (Red == null || isTransitioning) return;

        float bossScale = 2.5f;
        transform.localScale = new Vector3(Red.transform.position.x > transform.position.x ? bossScale : -bossScale, bossScale, 1f);

        // Bloquear la altura al suelo para que NUNCA atraviese el piso (collider es trigger).
        if (Mathf.Abs(transform.position.y - phase2GroundY) > 0.001f)
        {
            transform.position = new Vector3(transform.position.x, phase2GroundY, transform.position.z);
        }
        if (rb != null) rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        float distance = Vector2.Distance(transform.position, Red.transform.position);

        // Manage active attack and rest phases in ground phase
        finalBattlePhaseTimer += Time.deltaTime;
        if (!finalBattleIsResting && finalBattlePhaseTimer >= attackPhaseDuration)
        {
            finalBattleIsResting = true;
            finalBattlePhaseTimer = 0f;
            spriteRenderer.color = new Color(0.6f, 0.8f, 1f, 1f); // cool blue indicating resting/vulnerability
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else if (finalBattleIsResting && finalBattlePhaseTimer >= restPhaseDuration)
        {
            finalBattleIsResting = false;
            finalBattlePhaseTimer = 0f;
            spriteRenderer.color = Color.white;
        }

        if (finalBattleIsResting)
        {
            // Resting phase: do nothing, just slide slowly or stay still
            if (rb != null) rb.linearVelocity = new Vector2(Mathf.Sign(Red.transform.position.x - transform.position.x) * groundSpeed * 0.2f, rb.linearVelocity.y);
            return;
        }

        // Postura de disparo: quieto mientras dispara (más legible).
        if (isShootingStance)
        {
            if (Time.time >= shootStanceEndTime) isShootingStance = false;
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // Active ground attack phase
        float dirX = Mathf.Sign(Red.transform.position.x - transform.position.x);
        
        if (distance < 4f && Time.time > lastMeleeTime + meleeCooldown)
        {
            MeleeSlam();
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else
        {
            // Walk towards player
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(dirX * groundSpeed, rb.linearVelocity.y);
            }

            // Disparar en abanico por intervalos: al disparar, se detiene.
            if (Time.time > lastPatternTime + patternInterval)
            {
                lastPatternTime = Time.time;
                BeginShootStance();
                Vector2 shootDir = (Red.transform.position - transform.position).normalized;
                FireBossBullet(shootDir, 6f, BulletPattern.Straight);
                FireBossBullet(Rotate(shootDir, 22f * Mathf.Deg2Rad), 6f, BulletPattern.Straight);
                FireBossBullet(Rotate(shootDir, -22f * Mathf.Deg2Rad), 6f, BulletPattern.Straight);
            }
        }
    }

    private IEnumerator TransitionToPhase2Routine()
    {
        isTransitioning = true;
        currentState = RobotBossState.FinalBattle; // Ensure final battle
        spriteRenderer.color = Color.gray;
        
        // Detener velocidad kinematic
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        Time.timeScale = 0f; // Pause time scale during dialogue transition!
        // Display dialogue or screen message
        CreateDialogueUI("SISTEMAS GRAVITATORIOS DESTRUIDOS!\nINICIANDO PROTOCOLO DE COMBATE TERRESTRE!");

        // Wait on ground
        yield return new WaitForSecondsRealtime(3.0f);

        if (dialoguePanel != null) Destroy(dialoguePanel);
        Time.timeScale = 1f; // Resume time scale!

        // El único collider del boss es un TRIGGER, así que la física de gravedad lo haría
        // atravesar el suelo. En vez de eso, mantenemos el cuerpo KINEMÁTICO y bloqueamos su
        // altura al nivel del suelo (la Y del jugador). Hacemos una caída manual para el drama.
        phase2GroundY = Red != null ? Red.transform.position.y : transform.position.y;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // Disable hover or flying animations
        if (animator != null)
        {
            animator.SetBool("IsFlying", false);
        }

        // Caída manual controlada hasta el suelo.
        float fallTime = 0.5f;
        float fallElapsed = 0f;
        Vector3 fallStart = transform.position;
        Vector3 fallEnd = new Vector3(fallStart.x, phase2GroundY, 0f);
        while (fallElapsed < fallTime)
        {
            transform.position = Vector3.Lerp(fallStart, fallEnd, fallElapsed / fallTime);
            fallElapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = fallEnd;

        // Restore Health for Phase 2
        isPhase2 = true;
        Health = MaxHealth;
        if (bossHealthBar != null)
        {
            bossHealthBar.SetHealth(Health, MaxHealth);
        }

        spriteRenderer.color = Color.white;
        isTransitioning = false;
        finalBattleIsResting = false;
        finalBattlePhaseTimer = 0f;
        isShootingStance = false;
    }

    public void TakeDamage(int damage)
    {
        if (currentState != RobotBossState.FinalBattle || isDead || isTransitioning) return;

        // If in Final Battle Phase 1 or Phase 2, visual indicator when not resting but still take damage
        if (!finalBattleIsResting)
        {
            StartCoroutine(ImmuneFlashRoutine());
        }

        Health -= damage;
        if (bossHealthBar != null) bossHealthBar.SetHealth(Health, MaxHealth);

        if (Health <= 0)
        {
            if (!isPhase2)
            {
                StartCoroutine(TransitionToPhase2Routine());
            }
            else
            {
                StartCoroutine(DieSequenceRoutine());
            }
        }
    }

    private IEnumerator ImmuneFlashRoutine()
    {
        Color orig = spriteRenderer.color;
        spriteRenderer.color = Color.cyan;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = orig;
    }

    private IEnumerator DieSequenceRoutine()
    {
        isDead = true;
        currentState = RobotBossState.Dead;
        MusicManager.StopBackgroundMusicStatic();

        if (bossHealthBar != null) bossHealthBar.Hide();

        // La batalla terminó: abrir todas las barreras de la arena.
        OpenAllBarriers();

        // Detener oleadas de goblins.
        if (goblinWaveRoutine != null) { StopCoroutine(goblinWaveRoutine); goblinWaveRoutine = null; }

        // El boss fue derrotado: limpiar banderas para que no vuelva a aparecer al recargar.
        PlayerPrefs.SetInt("Nivel2RefightBoss", 0);
        PlayerPrefs.SetInt("Nivel2BossDefeated", 1);
        PlayerPrefs.Save();

        Vector3 deathPos = transform.position;

        // Slow down and freeze
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Play dramatic death flashing
        for (int i = 0; i < 8; i++)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            spriteRenderer.color = Color.clear;
            yield return new WaitForSeconds(0.15f);
            
            // Spawn some medium-sized explosions around the boss body
            Vector3 randomOffset = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-1.5f, 1.5f), 0);
            PixelExplosion.CreateExplosion(transform.position + randomOffset, 3.5f);
        }

        // Final giant massive explosion!
        PixelExplosion.CreateExplosion(transform.position, 8.0f);
        PixelExplosion.CreateExplosion(transform.position + Vector3.left * 2f, 6.0f);
        PixelExplosion.CreateExplosion(transform.position + Vector3.right * 2f, 6.0f);
        PixelExplosion.CreateExplosion(transform.position + Vector3.up * 2f, 6.0f);

        AudioClip expSound = Resources.Load<AudioClip>("Audio/snd_explosion_solid");
        #if UNITY_EDITOR
        if (expSound == null)
            expSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SpriteSheets/sounds/snd_explosion_solid.ogg");
        #endif
        if (expSound != null)
        {
            AudioSource.PlayClipAtPoint(expSound, transform.position, 1.5f);
        }

        // Donde muere el jefe, crear el portal final del juego (lleva al diálogo misterioso + FIN).
        SpawnEndGamePortal(new Vector3(deathPos.x, phase2GroundY, 0f));

        Destroy(gameObject, 0.2f);
    }

    private void SpawnEndGamePortal(Vector3 pos)
    {
        GameObject portal = new GameObject("EndGamePortal");
        portal.transform.position = pos;

        var sr = portal.AddComponent<SpriteRenderer>();
        sr.sprite = portalSprite;
        sr.sortingOrder = 6;
        portal.transform.localScale = Vector3.one * 2f;

        var col = portal.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.5f, 2.0f);

        portal.AddComponent<EndGamePortal>();
    }

    private void ShowCorreSign(bool show)
    {
        if (show)
        {
            if (correPanel != null) Destroy(correPanel);

            var canvas = GameObject.Find("Canvas");
            if (canvas == null) return;

            correPanel = new GameObject("CorreSign", typeof(RectTransform));
            correPanel.transform.SetParent(canvas.transform, false);
            var rect = correPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.7f); // Centered, slightly above middle of screen
            rect.anchorMax = new Vector2(0.5f, 0.7f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(250f, 60f);

            var img = correPanel.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.7f); // semi-transparent black background

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(correPanel.transform, false);
            var t = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            t.text = "¡CORRE!";
            t.alignment = TMPro.TextAlignmentOptions.Center;
            t.color = Color.red;
            t.fontSize = 36;
            t.fontStyle = TMPro.FontStyles.Bold;

            #if UNITY_EDITOR
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset");
            if (fontAsset != null) t.font = fontAsset;
            #endif
        }
        else
        {
            if (correPanel != null)
            {
                Destroy(correPanel);
            }
        }
    }

    private void SetRedSpeedMultiplier(float mult)
    {
        if (Red != null)
        {
            var move = Red.GetComponent<RedMovement>();
            if (move != null)
            {
                move.SpeedMultiplier = mult;
                Debug.Log($"Set Red SpeedMultiplier to {mult}");
            }
        }
    }
}
