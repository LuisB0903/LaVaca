using UnityEngine;

[DisallowMultipleComponent]
public class EnemyProjectileShooter2D : MonoBehaviour
{
    [Header("Proyectiles")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Vector3 localSpawnOffset = new Vector3(0.35f, 0f, 0f);
    [SerializeField] private float fireInterval = 2f;
    [SerializeField] private float startDelay = 0f;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileLifetime = 5f;
    [SerializeField] private bool rotateToAim = true;

    private Transform _target;
    private float _nextShotTime;
    private bool _attackEnabled;

    public void SetTarget(Transform newTarget)
    {
        _target = newTarget;
    }

    public void SetAttackEnabled(bool value)
    {
        _attackEnabled = value;
    }

    public void Tick(float elapsedTime)
    {
        if (!_attackEnabled || projectilePrefab == null || _target == null)
            return;

        if (elapsedTime < startDelay)
            return;

        if (elapsedTime < _nextShotTime)
            return;

        _nextShotTime = elapsedTime + Mathf.Max(0.1f, fireInterval);

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        spawnPosition += transform.TransformVector(localSpawnOffset);

        Vector2 aimVector = (Vector2)(_target.position - spawnPosition);
        if (aimVector.sqrMagnitude <= 0.0001f)
            aimVector = Vector2.right;

        Vector2 direction = aimVector.normalized;
        Quaternion spawnRotation = rotateToAim
            ? Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg)
            : (firePoint != null ? firePoint.rotation : transform.rotation);

        GameObject projectileInstance = Instantiate(projectilePrefab, spawnPosition, spawnRotation);
        EnemyProjectile2D projectile = projectileInstance.GetComponent<EnemyProjectile2D>();

        if (projectile != null)
        {
            projectile.Launch(direction, projectileSpeed, projectileLifetime);
            return;
        }

        Rigidbody2D rigidbody2D = projectileInstance.GetComponent<Rigidbody2D>();
        if (rigidbody2D != null)
            rigidbody2D.linearVelocity = direction * projectileSpeed;

        Destroy(projectileInstance, projectileLifetime);
    }

    public void SetProjectilePrefab(GameObject newProjectilePrefab)
    {
        projectilePrefab = newProjectilePrefab;
    }
}