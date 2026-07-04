using UnityEngine;

public class PollenBullet : MonoBehaviour
{
    public float Speed = 10f;
    public float LifeTime = 1.5f;

    private Rigidbody2D rb;
    private Vector3 direction;
    public GameObject Owner;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) Debug.LogWarning("PollenBullet: falta Rigidbody2D.");

        Destroy(gameObject, LifeTime);
    }

    private void FixedUpdate()
    {
        if (rb != null) rb.linearVelocity = direction * Speed;
    }

    public void SetDirection(Vector3 dir) => direction = dir.normalized;

    public void SetOwner(GameObject owner)
    {
        Owner = owner;
        if (Owner == null) return;
        var ownerCol = Owner.GetComponent<Collider2D>();
        var myCol = GetComponent<Collider2D>();
        if (ownerCol != null && myCol != null) Physics2D.IgnoreCollision(ownerCol, myCol);
    }

    public void DestroyBullet() => Destroy(gameObject);

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == Owner) return;

        // Ignore other projectiles
        if (collision.gameObject.name.Contains("Bullet") || collision.gameObject.name.Contains("Feather") || collision.gameObject.name.Contains("Pollen"))
        {
            return;
        }

        // Check if Admin Mode is active on the owner player to enable one-shot kills.
        // No dependemos del tag "Player" (Red puede estar Untagged).
        bool isAdminOneShot = false;
        if (Owner != null)
        {
            var pm = Owner.GetComponent<RedMovement>();
            if (pm != null && pm.IsAdminModeActive)
            {
                isAdminOneShot = true;
            }
        }

        if (isAdminOneShot)
        {
            var robot = collision.GetComponent<RobotEnemy>();
            if (robot != null) { robot.TakeDamage(9999); DestroyBullet(); return; }

            var grunt = collision.GetComponent<GruntEnemy>();
            if (grunt != null) { grunt.TakeDamage(9999); DestroyBullet(); return; }

            var gob = collision.GetComponent<GoblinScript>();
            if (gob != null) { gob.TakeDamage(9999); DestroyBullet(); return; }

            var crow = collision.GetComponent<CrowEnemy>();
            if (crow != null) { crow.TakeDamage(9999); DestroyBullet(); return; }

            var boss = collision.GetComponent<CrowBoss>();
            if (boss != null) { boss.TakeDamage(9999); DestroyBullet(); return; }
        }

        // If it hits CrowBoss, destroy it immediately without applying effects (Immunity)
        var bossTarget = collision.GetComponent<CrowBoss>();
        if (bossTarget != null)
        {
            DestroyBullet();
            return;
        }

        // Robots are completely immune to pollen/nature
        if (collision.GetComponent<RobotEnemy>() != null || collision.GetComponent<GruntEnemy>() != null || collision.GetComponent<RobotBoss>() != null)
        {
            DestroyBullet();
            return;
        }

        var gobTarget = collision.GetComponent<GoblinScript>();
        if (gobTarget != null)
        {
            // Apply Pollen Effect (Slow + DOT)
            var effect = gobTarget.gameObject.GetComponent<PollenEffect>();
            if (effect == null)
            {
                gobTarget.gameObject.AddComponent<PollenEffect>();
            }
            DestroyBullet();
            return;
        }

        var crowTarget = collision.GetComponent<CrowEnemy>();
        if (crowTarget != null)
        {
            // Apply Pollen Effect (Slow + DOT)
            var effect = crowTarget.gameObject.GetComponent<PollenEffect>();
            if (effect == null)
            {
                crowTarget.gameObject.AddComponent<PollenEffect>();
            }
            DestroyBullet();
            return;
        }

        // Destroy against any other solid wall, ground, etc.
        if (!collision.isTrigger)
        {
            DestroyBullet();
        }
    }
}