using System.Collections;
/* PollenEffect.cs: efecto de polen que aplica da�o con el tiempo y ralentiza enemigos. */
using UnityEngine;

public class PollenEffect : MonoBehaviour
{
    private float duration = 3f;
    private float tickInterval = 1f;
    private int damagePerTick = 2;
    private float slowFactor = 0.5f;

    private float originalSpeed;
    private bool speedReduced = false;

    private GoblinScript goblin;
    private CrowEnemy crow;
    private RobotEnemy robot;
    private GruntEnemy grunt;

    private ParticleSystem purpleParticles;

    private void Start()
    {
        // CrowBoss y RobotEnemy son inmunes a este efecto; Grunt tambi�n
        if (GetComponent<CrowBoss>() != null || GetComponent<RobotEnemy>() != null || GetComponent<GruntEnemy>() != null || GetComponent<RobotBoss>() != null)
        {
            Destroy(this);
            return;
        }

        goblin = GetComponent<GoblinScript>();
        crow = GetComponent<CrowEnemy>();
        robot = GetComponent<RobotEnemy>();
        grunt = GetComponent<GruntEnemy>();

        if (goblin != null)
        {
            originalSpeed = goblin.MoveSpeed;
            goblin.MoveSpeed *= slowFactor;
            speedReduced = true;
        }
        else if (crow != null)
        {
            originalSpeed = crow.MoveSpeed;
            crow.MoveSpeed *= slowFactor;
            speedReduced = true;
        }
        else if (robot != null)
        {
            originalSpeed = robot.MoveSpeed;
            robot.MoveSpeed *= slowFactor;
            speedReduced = true;
        }
        else if (grunt != null)
        {
            originalSpeed = grunt.MoveSpeed;
            grunt.MoveSpeed *= slowFactor;
            speedReduced = true;
        }

        // Generar part�culas moradas alrededor del enemigo
        SpawnPurpleParticles();

        // Iniciar da�o por tiempo (DOT)
        StartCoroutine(DotRoutine());
    }

    private void SpawnPurpleParticles()
    {
        GameObject particleGo = new GameObject("PollenVisualEffect");
        particleGo.transform.SetParent(transform, false);
        particleGo.transform.localPosition = Vector3.zero;

        purpleParticles = particleGo.AddComponent<ParticleSystem>();

        // Configure Particle System for purple pollen aura
        var main = purpleParticles.main;
        main.duration = duration;
        main.loop = true;
        main.startLifetime = 0.6f;
        main.startSpeed = 0.5f;
        main.startSize = 0.2f;
        main.startColor = new Color(0.6f, 0.1f, 0.8f, 0.8f); // Soft purple
        main.maxParticles = 30;

        var emission = purpleParticles.emission;
        emission.rateOverTime = 15f;

        var shape = purpleParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;

        var renderer = particleGo.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 5;
        }

        purpleParticles.Play();
    }

    private IEnumerator DotRoutine()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;

            if (goblin != null)
            {
                goblin.TakeDamage(damagePerTick);
            }
            else if (crow != null)
            {
                crow.TakeDamage(damagePerTick);
            }
            else if (robot != null)
            {
                robot.TakeDamage(damagePerTick);
            }
            else if (grunt != null)
            {
                grunt.TakeDamage(damagePerTick);
            }
        }

        CleanUp();
    }

    private void CleanUp()
    {
        if (speedReduced)
        {
            if (goblin != null)
            {
                goblin.MoveSpeed = originalSpeed;
            }
            else if (crow != null)
            {
                crow.MoveSpeed = originalSpeed;
            }
            else if (robot != null)
            {
                robot.MoveSpeed = originalSpeed;
            }
            else if (grunt != null)
            {
                grunt.MoveSpeed = originalSpeed;
            }
        }

        if (purpleParticles != null)
        {
            Destroy(purpleParticles.gameObject);
        }

        Destroy(this);
    }

    private void OnDestroy()
    {
        // Safety fallback to restore speed if destroyed prematurely
        if (speedReduced)
        {
            if (goblin != null) goblin.MoveSpeed = originalSpeed;
            if (crow != null) crow.MoveSpeed = originalSpeed;
            if (robot != null) robot.MoveSpeed = originalSpeed;
            if (grunt != null) grunt.MoveSpeed = originalSpeed;
        }
    }
}