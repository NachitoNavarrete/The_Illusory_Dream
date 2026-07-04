using UnityEngine;
/* CrowBoss: controla al jefe (movimiento, ataques, vida y UI). */
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum BossState
{
    Inactive,
    IntroDialogue,
    Battle,
    Stunned,
    OutroDialogue,
    Dead
}

public class CrowBoss : MonoBehaviour
{
    [Header("State")]
    public BossState currentState = BossState.Inactive;

    [Header("Referencias")]
    public GameObject Red;
    public BossHealthBar bossHealthBar;
    public float VisionRange = 8f;
    public LayerMask ObstacleMask;

    [Header("Stats")]
    public int Health = 15;
    public int MaxHealth = 15;
    public int AttackDamage = 1;
    public float AttackForceMultiplier = 1f;

    [Header("Movimiento y ataque")]
    public float MoveSpeed = 2.5f;
    public float AttackRange = 1.4f;
    public float AttackCooldown = 1.8f;

    [Header("Ataque a Distancia")]
    public float projectileSpeed = 8.5f;
    public int projectileDamage = 1;

    [Header("Animator")]
    public string RunBoolName = "Movimiento";
    public string DeathBoolName = "Muerto";

    [Header("Audio Tracks")]
    public AudioClip introMusic;   // Loaded from Assets/Music/Graznido de Escarcha.mp3
    public AudioClip battleMusic;  // Loaded from Assets/Music/grajo de cristal.mp3

    [Header("Battle Barrier")]
    public GameObject battleBarrier;

    [Header("Health Drop Prefab")]
    public GameObject healthDropPrefab;

    [Header("Summon Crows")]
    public GameObject crowMinionPrefab;

    [Header("Sprites de Proyectil y Barrera")]
    public Sprite projectileSprite;
    public Sprite barrierBlockSprite;
    public Sprite portalSprite;

    // Internals
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private float lastAttackTime = -999f;
    private bool isDead = false;
    private float stunEndTime = 0f;
    private Coroutine activeAttackCoroutine = null;
    private bool isDashing = false;

    // Dialogue Panel references
    private string[] introDialogueLines = {
        "Bienvenido...",
        "mis amigos quieren seguir jugando",
        "podrías solo dejarlos...?",
        "si no es el caso...entonces"
    };

    private string[] outroDialogueLines = {
        "ngh...se supone que este mundo es para divertirse!",
        "el me lo dijo....."
    };

    private int currentDialogueIndex = 0;
    private GameObject dialoguePanel;
    private TMPro.TextMeshProUGUI dialogueText;
    private TMPro.TextMeshProUGUI speakerText;

    private Coroutine typewriterCoroutine;

    private void ShowDialogueLine(string line)
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }
        typewriterCoroutine = StartCoroutine(TypewriterRoutine(line));
    }

    private IEnumerator TypewriterRoutine(string line)
    {
        if (dialogueText == null) yield break;
        dialogueText.text = "";
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(0.04f);
        }
        typewriterCoroutine = null;
    }

    private void Start()
    {
        // Start: inicializa componentes, UI y zonas del jefe
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rb == null) Debug.LogWarning("CrowBoss: falta Rigidbody2D.");
        if (animator == null) Debug.LogWarning("CrowBoss: falta Animator.");

        // Automatically search for player if not assigned
        if (Red == null)
        {
            Red = GameObject.Find("Red_0");
            if (Red == null) Red = GameObject.Find("Red");
        }

        // Initialize health
        if (MaxHealth <= 0) MaxHealth = Health > 0 ? Health : 15;
        Health = Mathf.Clamp(Health, 0, MaxHealth);

        // Configure BossHUD and automatically link the health bar
        GameObject hudGo = null;
        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            var trans = canvas.transform.Find("BossHUD");
            if (trans != null) hudGo = trans.gameObject;
        }

        if (hudGo != null)
        {
            // Disable default Vida player-tracking component
            var vidaComp = hudGo.GetComponent<Vida>();
            if (vidaComp != null)
            {
                vidaComp.enabled = false;
            }

            // Find child "relleno" to use as fill image
            var rellenoTrans = hudGo.transform.Find("relleno");
            if (rellenoTrans != null)
            {
                var fillImg = rellenoTrans.GetComponent<Image>();
                if (bossHealthBar == null)
                {
                    bossHealthBar = GetComponent<BossHealthBar>();
                }
                if (bossHealthBar != null)
                {
                    bossHealthBar.fillImage = fillImg;
                }
            }

            // Keep it hidden initially
            hudGo.SetActive(false);
        }

        if (bossHealthBar != null)
        {
            bossHealthBar.Hide();
            bossHealthBar.SetHealth(Health, MaxHealth);
        }

        // Dynamically create a battle barrier if none exists
        if (battleBarrier == null)
        {
            CreateBattleBarrier();
        }

        // Search for user's custom trigger object named "a", link it safely
        var triggerObj = GameObject.Find("a");
        if (triggerObj != null)
        {
            var rbTrigger = triggerObj.GetComponent<Rigidbody2D>();
            if (rbTrigger == null) rbTrigger = triggerObj.AddComponent<Rigidbody2D>();
            rbTrigger.bodyType = RigidbodyType2D.Kinematic;
            rbTrigger.useFullKinematicContacts = true;

            var zone = triggerObj.GetComponent<BossTriggerZone>();
            if (zone == null) zone = triggerObj.AddComponent<BossTriggerZone>();
            zone.boss = this;
            
            var col = triggerObj.GetComponent<BoxCollider2D>();
            if (col == null) col = triggerObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2f, 20f); // Impossible to jump over!
            Debug.Log("Linked empty object 'a' as BossTriggerZone.");
        }
        else
        {
            // Fallback: create dynamic trigger if "a" is not manually placed
            CreateTriggerZone();
        }
    }

    private void CreateTriggerZone()
    {
        GameObject triggerGo = new GameObject("BossTriggerZone");
        triggerGo.transform.position = new Vector3(254f, 1f, 0f);
        
        var rbTrigger = triggerGo.AddComponent<Rigidbody2D>();
        rbTrigger.bodyType = RigidbodyType2D.Kinematic;
        rbTrigger.useFullKinematicContacts = true;

        var col = triggerGo.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 20f); // Impossible to jump over!

        var triggerScript = triggerGo.AddComponent<BossTriggerZone>();
        triggerScript.boss = this;
    }

    public void ActivateBossFight()
    {
        // ActivateBossFight: comienza la pelea con el jefe (se usa desde un trigger)
        if (currentState != BossState.Inactive) return;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        // Destroy trigger zone elements immediately to prevent invisible colliders from blocking bullets!
        var triggerObj = GameObject.Find("a");
        if (triggerObj != null) Destroy(triggerObj);
        var triggerZone = GameObject.Find("BossTriggerZone");
        if (triggerZone != null) Destroy(triggerZone);

        StartIntroDialogue();
    }

    private void CreateBattleBarrier()
    {
        // CreateBattleBarrier: crea una barrera física/visual para encerrar la arena del jefe (izquierda y derecha)
        battleBarrier = new GameObject("BossBattleBarrier");
        battleBarrier.transform.position = new Vector3(250.0f, 2.5f, 0f);

        // --- PARED IZQUIERDA ---
        GameObject leftWall = new GameObject("LeftWall");
        leftWall.transform.SetParent(battleBarrier.transform, false);
        leftWall.transform.localPosition = Vector3.zero;
        
        var colLeft = leftWall.AddComponent<BoxCollider2D>();
        colLeft.size = new Vector2(2f, 30f); // Súper alta para que nadie salte ni escape

        Sprite stoneSprite = barrierBlockSprite;

        // Crear bloques visuales para la pared izquierda
        for (int i = 0; i < 20; i++)
        {
            GameObject block = new GameObject("Left_BarrierBlock_" + i);
            block.transform.SetParent(leftWall.transform, false);
            block.transform.localPosition = new Vector3(0f, -10f + i, 0f);
            var sr = block.AddComponent<SpriteRenderer>();
            sr.sprite = stoneSprite;
            sr.sortingOrder = 4;
            block.transform.localScale = new Vector3(2.0f, 1.0f, 1.0f);
        }

        // --- PARED DERECHA ---
        GameObject rightWall = new GameObject("RightWall");
        rightWall.transform.SetParent(battleBarrier.transform, false);
        rightWall.transform.localPosition = new Vector3(28.0f, 0f, 0f); // A 28 unidades, cerrando en X = 278f
        
        var colRight = rightWall.AddComponent<BoxCollider2D>();
        colRight.size = new Vector2(2f, 30f); // Súper alta para que nadie escape

        // Crear bloques visuales para la pared derecha
        for (int i = 0; i < 20; i++)
        {
            GameObject block = new GameObject("Right_BarrierBlock_" + i);
            block.transform.SetParent(rightWall.transform, false);
            block.transform.localPosition = new Vector3(0f, -10f + i, 0f);
            var sr = block.AddComponent<SpriteRenderer>();
            sr.sprite = stoneSprite;
            sr.sortingOrder = 4;
            block.transform.localScale = new Vector3(2.0f, 1.0f, 1.0f);
        }

        battleBarrier.SetActive(false);
    }

    private void Update()
    {
        // Update: comportamiento por frame según estado y visión
        if (isDead) return;

        if (Red == null)
        {
            SetRunBool(false);
            return;
        }

        Vector3 toPlayer = Red.transform.position - transform.position;
        float horizontalDistance = Mathf.Abs(toPlayer.x);

        switch (currentState)
        {
            case BossState.Inactive:
                // Trigger battle start if Crow can see the player, or if the player is within range, or reaches X >= 254
                if (CanSeePlayer(horizontalDistance) || Red.transform.position.x >= 254f)
                {
                    ActivateBossFight();
                }
                break;

            case BossState.IntroDialogue:
                if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                SetRunBool(false);

                if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
                {
                    Debug.Log("Dialogue Input received (Z, X, Space, Return, or Mouse Click) to advance Intro Dialogue.");
                    AdvanceIntroDialogue();
                }
                break;

            case BossState.Battle:
                // Don't execute default movement/attack patterns if currently in an active special attack coroutine
                if (activeAttackCoroutine != null) return;

                // Look towards player
                transform.localScale = new Vector3(toPlayer.x >= 0f ? 1f : -1f, 1f, 1f);

                // Trigger randomized attack patterns when cooldown finishes
                if (Time.time >= lastAttackTime + AttackCooldown)
                {
                    ChooseRandomAttack(horizontalDistance, toPlayer);
                }
                else
                {
                    // Regular movement
                    if (horizontalDistance > AttackRange)
                    {
                        MoveTowardsPlayer(toPlayer);
                        SetRunBool(true);
                    }
                    else
                    {
                        SetRunBool(false);
                        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    }
                }
                break;

            case BossState.Stunned:
                if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                SetRunBool(false);

                if (Time.time >= stunEndTime)
                {
                    currentState = BossState.Battle;
                    if (animator != null)
                    {
                        animator.Play("Crow_Idle");
                    }
                }
                break;

            case BossState.OutroDialogue:
                if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                SetRunBool(false);

                if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
                {
                    Debug.Log("Dialogue Input received (Z, X, Space, Return, or Mouse Click) to advance Outro Dialogue.");
                    if (typewriterCoroutine != null)
                    {
                        StopCoroutine(typewriterCoroutine);
                        typewriterCoroutine = null;
                        if (dialogueText != null && outroDialogueLines != null && currentDialogueIndex < outroDialogueLines.Length)
                        {
                            dialogueText.text = outroDialogueLines[currentDialogueIndex];
                        }
                    }
                    else
                    {
                        AdvanceOutroDialogue();
                    }
                }
                break;

            case BossState.Dead:
                break;
        }
    }

    private void MoveTowardsPlayer(Vector3 toPlayer)
    {
        if (rb == null) return;
        float dir = Mathf.Sign(toPlayer.x);
        rb.linearVelocity = new Vector2(dir * MoveSpeed, rb.linearVelocity.y);
    }

    // --- RANDOM ATTACK DECISION TREE ---
    private void ChooseRandomAttack(float distance, Vector3 toPlayer)
    {
        // Cooldown reset point
        lastAttackTime = Time.time;

        if (distance <= AttackRange)
        {
            // Close Range Attacks: Melee, Dash back/through, Shockwave slam, Summon Crows
            float rand = Random.value;
            if (rand < 0.4f)
            {
                MeleeAttack();
            }
            else if (rand < 0.6f)
            {
                activeAttackCoroutine = StartCoroutine(DashAttackRoutine(toPlayer));
            }
            else if (rand < 0.8f)
            {
                activeAttackCoroutine = StartCoroutine(SlamAttackRoutine());
            }
            else
            {
                activeAttackCoroutine = StartCoroutine(SummonCrowsRoutine());
            }
        }
        else
        {
            // Mid/Long Range Attacks: Single feather, Feather storm spread, Wind dash, Shockwave slam, Summon Crows
            float rand = Random.value;
            if (rand < 0.2f)
            {
                SingleFeatherAttack(toPlayer);
            }
            else if (rand < 0.4f)
            {
                FeatherSpreadAttack(toPlayer);
            }
            else if (rand < 0.6f)
            {
                activeAttackCoroutine = StartCoroutine(DashAttackRoutine(toPlayer));
            }
            else if (rand < 0.8f)
            {
                activeAttackCoroutine = StartCoroutine(SlamAttackRoutine());
            }
            else
            {
                activeAttackCoroutine = StartCoroutine(SummonCrowsRoutine());
            }
        }
    }

    private IEnumerator SummonCrowsRoutine()
    {
        if (animator != null)
        {
            animator.Play("Crow_Attack");
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.2f, 0.8f, 1f); // Cyan/blue aura for summon telegraph
        }

        yield return new WaitForSeconds(0.5f);

        if (currentState == BossState.Battle && !isDead && crowMinionPrefab != null)
        {
            // Spawn 3 crows
            Vector3[] spawnOffsets = new Vector3[]
            {
                new Vector3(-1.5f, 2f, 0f),
                new Vector3(0f, 3f, 0f),
                new Vector3(1.5f, 2f, 0f)
            };

            for (int i = 0; i < spawnOffsets.Length; i++)
            {
                GameObject minion = Instantiate(crowMinionPrefab, transform.position + spawnOffsets[i], Quaternion.identity);
                var crowScript = minion.GetComponent<CrowEnemy>();
                if (crowScript != null)
                {
                    crowScript.Red = Red;
                    crowScript.Health = 1;
                    crowScript.healthDropPrefab = healthDropPrefab; // Pass drop prefab
                }
            }
        }

        ResetBossAura();
        activeAttackCoroutine = null;
    }

    // --- DIALOGUE SYSTEM ---

    private void StartIntroDialogue()
    {
        currentState = BossState.IntroDialogue;
        FreezePlayer(true);
        Time.timeScale = 0f; // Pause time scale during dialogue!

        // Activate the gate/barrier behind the player
        if (battleBarrier != null)
        {
            battleBarrier.SetActive(true);
            Debug.Log("Boss Battle Barrier activated! Trapped player in arena.");
        }

        // Play Intro Music
        if (introMusic != null)
        {
            MusicManager.PlayBackgroundMusicStatic(true, introMusic);
        }

        // Create Dialogue Box UI
        CreateDialogueUI();
        currentDialogueIndex = 0;
        ShowDialogueLine(introDialogueLines[currentDialogueIndex]);
    }

    private void AdvanceIntroDialogue()
    {
        currentDialogueIndex++;
        Debug.Log("AdvanceIntroDialogue called. currentDialogueIndex: " + currentDialogueIndex);
        if (introDialogueLines != null && currentDialogueIndex < introDialogueLines.Length)
        {
            if (dialogueText != null)
            {
                ShowDialogueLine(introDialogueLines[currentDialogueIndex]);
                Debug.Log("Displayed intro dialogue: " + introDialogueLines[currentDialogueIndex]);
            }
        }
        else
        {
            Debug.Log("Ending intro dialogue and starting fight.");
            EndIntroDialogue();
        }
    }

    private void EndIntroDialogue()
    {
        if (dialoguePanel != null)
        {
            Destroy(dialoguePanel);
        }
        FreezePlayer(false);
        Time.timeScale = 1f; // Resume time scale!

        currentState = BossState.Battle;

        // Show Health Bar
        GameObject hudGo = null;
        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            var trans = canvas.transform.Find("BossHUD");
            if (trans != null) hudGo = trans.gameObject;
        }

        if (hudGo != null)
        {
            hudGo.SetActive(true);
        }
        if (bossHealthBar != null)
        {
            bossHealthBar.Show();
            bossHealthBar.SetHealth(Health, MaxHealth);
        }

        // Play Battle Music
        if (battleMusic != null)
        {
            MusicManager.PlayBackgroundMusicStatic(true, battleMusic);
        }
    }

    private void StartOutroDialogue()
    {
        currentState = BossState.OutroDialogue;
        FreezePlayer(true);
        Time.timeScale = 0f; // Pause time scale during dialogue!

        // Interrupt any ongoing special attacks
        if (activeAttackCoroutine != null)
        {
            StopCoroutine(activeAttackCoroutine);
            activeAttackCoroutine = null;
        }
        isDashing = false;
        ResetBossAura();

        // Hide Boss HUD
        GameObject hudGo = null;
        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            var trans = canvas.transform.Find("BossHUD");
            if (trans != null) hudGo = trans.gameObject;
        }

        if (hudGo != null)
        {
            hudGo.SetActive(false);
        }
        if (bossHealthBar != null)
        {
            bossHealthBar.Hide();
        }

        // Stop current music
        MusicManager.StopBackgroundMusicStatic();

        // Create Dialogue Box UI
        CreateDialogueUI();
        currentDialogueIndex = 0;
        ShowDialogueLine(outroDialogueLines[currentDialogueIndex]);
    }

    private void AdvanceOutroDialogue()
    {
        currentDialogueIndex++;
        Debug.Log("AdvanceOutroDialogue called. currentDialogueIndex: " + currentDialogueIndex);
        if (outroDialogueLines != null && currentDialogueIndex < outroDialogueLines.Length)
        {
            if (dialogueText != null)
            {
                ShowDialogueLine(outroDialogueLines[currentDialogueIndex]);
                Debug.Log("Displayed outro dialogue: " + outroDialogueLines[currentDialogueIndex]);
            }
        }
        else
        {
            Debug.Log("Ending outro dialogue and triggering death sequence.");
            EndOutroDialogue();
        }
    }

    private void EndOutroDialogue()
    {
        if (dialoguePanel != null)
        {
            Destroy(dialoguePanel);
        }
        FreezePlayer(false);
        Time.timeScale = 1f; // Resume time scale!
        
        StartCoroutine(DieSequence());
    }

    private IEnumerator DieSequence()
    {
        // First play the death animation while boss is alive and on screen
        if (animator != null)
        {
            animator.Play("Crow_Death");
        }

        // Wait for the animation to play fully before starting the portal and clean up
        yield return new WaitForSeconds(0.6f);

        currentState = BossState.Dead;
        isDead = true;

        // Deactivate the battle barrier
        if (battleBarrier != null)
        {
            battleBarrier.SetActive(false);
        }

        // Spawn level transition door
        SpawnPortal();

        // Destroy the boss GameObject
        Destroy(gameObject);
    }

    private void CreateDialogueUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        // Parent Panel (Positioned at the TOP of the screen, completely black background)
        dialoguePanel = new GameObject("CrowDialoguePanel", typeof(RectTransform));
        dialoguePanel.transform.SetParent(canvas.transform, false);

        var rect = dialoguePanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f); // TOP Anchor
        rect.anchorMax = new Vector2(0.5f, 1f); // TOP Anchor
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(650f, 120f);
        rect.anchoredPosition = new Vector2(0f, -40f); // Spaced from screen ceiling

        var img = dialoguePanel.AddComponent<Image>();
        img.color = Color.black; // Basic black background

        // Speaker name
        var speakerGo = new GameObject("SpeakerText", typeof(RectTransform));
        speakerGo.transform.SetParent(dialoguePanel.transform, false);
        var speakerRect = speakerGo.GetComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0f, 1f);
        speakerRect.anchorMax = new Vector2(0f, 1f);
        speakerRect.pivot = new Vector2(0f, 1f);
        speakerRect.sizeDelta = new Vector2(200f, 30f);
        speakerRect.anchoredPosition = new Vector2(25f, -12f);

        speakerText = speakerGo.AddComponent<TMPro.TextMeshProUGUI>();
        speakerText.text = "Crow";
        speakerText.fontSize = 20f;
        speakerText.fontStyle = TMPro.FontStyles.Bold;
        speakerText.color = Color.white; // Simple white name

        // Dialogue text
        var textGo = new GameObject("DialogueText", typeof(RectTransform));
        textGo.transform.SetParent(dialoguePanel.transform, false);
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(-50f, -55f);
        textRect.anchoredPosition = new Vector2(0f, -15f);

        dialogueText = textGo.AddComponent<TMPro.TextMeshProUGUI>();
        dialogueText.text = "";
        dialogueText.fontSize = 17f;
        dialogueText.color = Color.white; // White letters

        // Prompt
        var promptGo = new GameObject("PromptText", typeof(RectTransform));
        promptGo.transform.SetParent(dialoguePanel.transform, false);
        var promptRect = promptGo.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(1f, 0f);
        promptRect.anchorMax = new Vector2(1f, 0f);
        promptRect.pivot = new Vector2(1f, 0f);
        promptRect.sizeDelta = new Vector2(250f, 20f);
        promptRect.anchoredPosition = new Vector2(-20f, 12f);

        var promptTmp = promptGo.AddComponent<TMPro.TextMeshProUGUI>();
        promptTmp.text = "Presiona [Z] para avanzar";
        promptTmp.fontSize = 11f;
        promptTmp.fontStyle = TMPro.FontStyles.Italic;
        promptTmp.color = new Color(0.8f, 0.8f, 0.8f);
        promptTmp.alignment = TMPro.TextAlignmentOptions.Right;

        // Apply retro pixelated font to all dialogue texts
#if UNITY_EDITOR
        var fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset");
        if (fontAsset != null)
        {
            speakerText.font = fontAsset;
            dialogueText.font = fontAsset;
            promptTmp.font = fontAsset;
        }
#endif

        dialoguePanel.transform.SetAsLastSibling();
    }

    private void FreezePlayer(bool freeze)
    {
        if (Red == null) return;
        var moveComp = Red.GetComponent<RedMovement>();
        if (moveComp != null)
        {
            moveComp.enabled = !freeze;
        }
        var playerRb = Red.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
        }
        var anim = Red.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("Movimiento", false);
        }
    }

    // --- BOSS ATTACK PORTFOLIO ---

    // ATTACK 1: Picotazo (Melee)
    private void MeleeAttack()
    {
        if (animator != null)
        {
            animator.Play("Crow_Attack");
        }

        var redComp = Red.GetComponent<RedMovement>();
        if (redComp != null && redComp.IsAlive)
        {
            Vector2 dir = (Red.transform.position - transform.position).normalized;
            redComp.Hit(dir, AttackDamage, AttackForceMultiplier, gameObject);
        }
    }

    // ATTACK 2: Pluma Sola (Single targeted projectile)
    private void SingleFeatherAttack(Vector3 toPlayer)
    {
        if (animator != null)
        {
            animator.Play("Crow_Attack");
        }

        Vector2 direction = toPlayer.normalized;
        SpawnFeatherProjectile(direction);
    }

    // ATTACK 3: Abanico de Plumas (Feather storm spread shot - 3 feathers)
    private void FeatherSpreadAttack(Vector3 toPlayer)
    {
        if (animator != null)
        {
            animator.Play("Crow_Attack");
        }

        Vector2 centerDir = toPlayer.normalized;

        // Calculate angled vectors
        float spreadAngle = 18f;
        Vector2 upperDir = RotateVector(centerDir, spreadAngle);
        Vector2 lowerDir = RotateVector(centerDir, -spreadAngle);

        SpawnFeatherProjectile(centerDir);
        SpawnFeatherProjectile(upperDir);
        SpawnFeatherProjectile(lowerDir);
    }

    // ATTACK 4: Embestida Sombra (Wind Charge Dash attack)
    private IEnumerator DashAttackRoutine(Vector3 toPlayer)
    {
        // 1. Telegraph the attack: turns dark, screams (anim), pauses
        if (animator != null)
        {
            animator.Play("Crow_Damage"); // Plays damage animation as wind-up stance
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.3f, 0.1f, 0.4f); // Purple aura
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        yield return new WaitForSeconds(0.45f);

        // Cancel if parried or dead during wind-up
        if (currentState != BossState.Battle || isDead || Red == null)
        {
            ResetBossAura();
            activeAttackCoroutine = null;
            yield break;
        }

        // 2. Dash forwards horizontally towards the player's direction
        isDashing = true;
        float dashDirection = Mathf.Sign(toPlayer.x);
        float dashTime = 0.4f;
        float elapsed = 0f;

        if (animator != null)
        {
            animator.Play("Crow_Walk"); // Plays walk animation super fast during dash
        }

        // Apply constant high speed velocity for the duration
        while (elapsed < dashTime)
        {
            if (currentState != BossState.Battle || isDead) break;

            if (rb != null)
            {
                rb.linearVelocity = new Vector2(dashDirection * 15f, rb.linearVelocity.y);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        isDashing = false;
        ResetBossAura();

        if (animator != null)
        {
            animator.Play("Crow_Idle");
        }

        activeAttackCoroutine = null;
    }

    // ATTACK 5: Eco de Sombras (Ground Slam shockwave projectile left & right)
    private IEnumerator SlamAttackRoutine()
    {
        // 1. Hop up into the air
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 8.5f);
        }

        if (animator != null)
        {
            animator.Play("Crow_Idle");
        }

        yield return new WaitForSeconds(0.4f);

        // Cancel if interrupted
        if (currentState != BossState.Battle || isDead || Red == null)
        {
            activeAttackCoroutine = null;
            yield break;
        }

        // 2. Slam down rapidly
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -16f);
        }

        yield return new WaitForSeconds(0.15f);

        // Wait until grounded
        while (rb != null && Mathf.Abs(rb.linearVelocity.y) > 0.1f)
        {
            yield return null;
        }

        // 3. Impact! Release shockwaves horizontally left and right
        if (currentState == BossState.Battle && !isDead)
        {
            if (animator != null)
            {
                animator.Play("Crow_Attack");
            }

            // Spawn shockwaves traveling horizontally along the ground
            SpawnFeatherProjectile(Vector2.left, 10f);
            SpawnFeatherProjectile(Vector2.right, 10f);

            // Shakes camera briefly if possible or just standard landing wait
            yield return new WaitForSeconds(0.2f);
        }

        activeAttackCoroutine = null;
    }

    private void SpawnFeatherProjectile(Vector2 dir, float customSpeed = 0f)
    {
        float speed = customSpeed > 0f ? customSpeed : projectileSpeed;

        GameObject projGo = new GameObject("CrowFeather");
        projGo.transform.position = transform.position + (Vector3)(dir * 0.5f);
        
        var sr = projGo.AddComponent<SpriteRenderer>();
        sr.sprite = GetProjectileSprite();
        sr.color = new Color(0.4f, 0.1f, 0.6f); // Purple shadow feather
        sr.sortingOrder = 3;

        var projRb = projGo.AddComponent<Rigidbody2D>();
        projRb.gravityScale = 0f;
        projRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = projGo.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.45f, 0.18f);

        var projScript = projGo.AddComponent<CrowProjectile>();
        projScript.Setup(dir, speed, projectileDamage, gameObject);
        projScript.healthDropPrefab = healthDropPrefab;
    }

    private void ResetBossAura()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }

    private Sprite GetProjectileSprite()
    {
        return projectileSprite;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (introMusic == null)
        {
            introMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Music/Graznido de Escarcha.mp3");
        }
        if (battleMusic == null)
        {
            battleMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Music/grajo de cristal.mp3");
        }
        if (healthDropPrefab == null)
        {
            healthDropPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animations/Prefabs/lifedrop_0.prefab");
        }
        if (crowMinionPrefab == null)
        {
            crowMinionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Animations/Prefabs/CrowEnemy.prefab");
        }
        if (barrierBlockSprite == null)
        {
            barrierBlockSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiles/Tileset_55.asset");
            if (barrierBlockSprite == null)
            {
                barrierBlockSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiles/Tileset_0.asset");
            }
        }
        if (projectileSprite == null)
        {
            string path = "Assets/Animations/Crow Animations/crow_attack.png";
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    if (asset is Sprite s && s.name.Contains("4"))
                    {
                        projectileSprite = s;
                        break;
                    }
                }
                if (projectileSprite == null)
                {
                    foreach (var asset in assets)
                    {
                        if (asset is Sprite s)
                        {
                            projectileSprite = s;
                            break;
                        }
                    }
                }
            }
        }
        if (portalSprite == null)
        {
            portalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SpriteSheets/enemy/grunt_portal.png");
        }
    }
#endif

    // --- CONTACT DAMAGE DURING CHARGES ---
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDashing || isDead) return;

        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<RedMovement>() != null)
        {
            var player = collision.gameObject.GetComponent<RedMovement>();
            // Respetar invulnerabilidad / modo admin de Red: no aplicar daño ni knockback.
            if (player != null && player.IsAlive && !player.IsInvulnerable && !player.IsAdminModeActive)
            {
                Vector2 pushDir = (collision.transform.position - transform.position).normalized;
                player.Hit(pushDir, AttackDamage + 1, AttackForceMultiplier * 1.5f, gameObject);
            }
        }
    }

    // --- DAMAGE & PARRY ---

    public void ApplyParryEffects(Vector2 direction, float force, float duration)
    {
        if (currentState != BossState.Battle && currentState != BossState.Stunned) return;

        // Safely interrupt active special attacks
        if (activeAttackCoroutine != null)
        {
            StopCoroutine(activeAttackCoroutine);
            activeAttackCoroutine = null;
        }
        isDashing = false;
        ResetBossAura();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(direction * force, ForceMode2D.Impulse);
        }

        currentState = BossState.Stunned;
        stunEndTime = Time.time + duration; // Exactly 2 seconds as requested!
        
        if (animator != null)
        {
            animator.Play("Crow_Damage");
        }

        Debug.Log("Crow Boss parried! Stunned for: " + duration + "s");
    }

    public void TakeDamage(int damage)
    {
        if (currentState != BossState.Battle && currentState != BossState.Stunned) return;

        Health -= damage;
        
        if (bossHealthBar != null)
        {
            bossHealthBar.Show();
            bossHealthBar.SetHealth(Health, MaxHealth);
        }

        if (Health <= 0)
        {
            StartOutroDialogue();
        }
        else
        {
            // Play hurt animation briefly if not already stunned and not in custom coroutine attack
            if (currentState == BossState.Battle && activeAttackCoroutine == null && animator != null)
            {
                animator.Play("Crow_Damage");
            }
        }
    }

    private void SpawnPortal()
    {
        GameObject portal = new GameObject("PortalNivel2");
        portal.transform.position = transform.position;
        
        var sr = portal.AddComponent<SpriteRenderer>();
        sr.sprite = portalSprite;
        sr.sortingOrder = 5;

        var col = portal.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.5f, 2.0f);

        portal.AddComponent<DoorToLevel2>();
    }

    private bool CanSeePlayer(float horizontalDistance)
    {
        if (horizontalDistance > VisionRange) return false;
        if (ObstacleMask.value == 0) return true;

        Vector2 origin = transform.position;
        Vector2 dir = (Red.transform.position - transform.position).normalized;
        float dist = Vector2.Distance(origin, Red.transform.position);
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, ObstacleMask);

        Debug.DrawLine(origin, origin + dir * dist, hit.collider == null ? Color.green : Color.red);
        return hit.collider == null;
    }

    private void SetRunBool(bool value)
    {
        if (animator == null) return;
        foreach (var p in animator.parameters)
        {
            if (p.name == RunBoolName && p.type == AnimatorControllerParameterType.Bool)
            {
                animator.SetBool(RunBoolName, value);
                return;
            }
        }
    }
}