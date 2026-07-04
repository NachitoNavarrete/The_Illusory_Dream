using UnityEngine;

// BulletPattern: patrones posibles para las balas del jefe.
// BossBullet: bala usada por los jefes con varios patrones (recta, espiral, sinusoidal, homing).
//
// Comentarios en español y explicación para niños (12-13 años):
// - Esta clase crea balas que pueden moverse de formas diferentes.
// - "Straight" = va recta hacia donde apunta.
// - "Spiral" = gira un poco mientras avanza.
// - "Sinusoidal" = se mueve en una onda (como una serpiente)
// - "Homing" = busca al jugador poco a poco.
//
// Funciones clave explicadas sencillamente:
// - Setup(dir, spd, sprite, pat, trg): configura la bala cuando se crea.
//   Piensa que es como darle instrucciones: hacia dónde ir, qué velocidad y qué dibujo usar.
// - Update(): se llama cada frame y mueve la bala según su patrón. También rota
//   la imagen para que apunte en la dirección de movimiento.
// - OnTriggerEnter2D(collision): se activa cuando la bala choca con algo.
//   Si choca con el jugador, le aplica daño; si choca con el suelo, se destruye.

public enum BulletPattern { Straight, Spiral, Sinusoidal, Homing }

public class BossBullet : MonoBehaviour
{
    public float speed = 5f;
    public float damage = 1f;
    public float lifeTime = 5f;
    public BulletPattern pattern = BulletPattern.Straight;
    
    private Vector2 direction;
    private float startTime;
    private Transform target;
    private float spiralAngle = 0f;

    // Setup: configura la bala cuando se crea.
    // Explicación simple: imagina que le das un mapa y una velocidad a la bala.
    public void Setup(Vector2 dir, float spd, Sprite sprite, BulletPattern pat = BulletPattern.Straight, Transform trg = null)
    {
        direction = dir;
        speed = spd;
        pattern = pat;
        target = trg;
        startTime = Time.time;
        
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = sprite;
        
        // Programar autodestrucción para no llenar la escena de balas viejas.
        Destroy(gameObject, lifeTime);
    }

    // Update: se llama cada frame y mueve la bala según su patrón.
    // Explicación para jóvenes: cada segundo la bala decide cómo moverse dependiendo
    // del tipo (recta, espiral, onda o buscar al jugador).
    void Update()
    {
        switch (pattern)
        {
            case BulletPattern.Straight:
                transform.Translate(direction * speed * Time.deltaTime, Space.World);
                break;
            case BulletPattern.Spiral:
                spiralAngle += Time.deltaTime * 5f;
                Vector2 spiralDir = new Vector2(Mathf.Cos(spiralAngle), Mathf.Sin(spiralAngle));
                transform.Translate((direction + spiralDir * 0.2f).normalized * speed * Time.deltaTime, Space.World);
                break;
            case BulletPattern.Sinusoidal:
                float age = Time.time - startTime;
                Vector2 perp = new Vector2(-direction.y, direction.x);
                transform.Translate((direction * speed + perp * Mathf.Sin(age * 10f) * 5f) * Time.deltaTime, Space.World);
                break;
            case BulletPattern.Homing:
                if (target != null)
                {
                    // Lerp suaviza el giro para que no cambie de dirección de golpe.
                    direction = Vector2.Lerp(direction, (target.position - transform.position).normalized, Time.deltaTime * 2f);
                }
                transform.Translate(direction * speed * Time.deltaTime, Space.World);
                break;
        }

        // Rotar hacia la dirección de movimiento para que la bala "mire" hacia donde va.
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    // OnTriggerEnter2D: cuando la bala choca con algo
    // Explicación sencilla: si choca con el jugador, le hace daño y se destruye; si choca con el suelo, también se destruye.
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.name.Contains("Red"))
        {
            var red = collision.GetComponent<RedMovement>();
            if (red != null)
            {
                // Aplicar daño al jugador (si existe)
                red.Hit((collision.transform.position - transform.position).normalized, (int)damage, 1f, gameObject);
            }
            Destroy(gameObject);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
