using UnityEngine;

[DisallowMultipleComponent]
public class EnemyTimedBomb2D : MonoBehaviour
{
    [Header("Explosión")]
    [SerializeField] private float fuseTime = 2f;
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private Vector3 explosionOffset;
    [SerializeField] private bool destroyAfterExplosion = true;
    [SerializeField] private float cowKillRadius = 1.5f;

    private float _explodeAt;
    private bool _armed;

    private void Awake()
    {
        Arm();
    }

    private void OnEnable()
    {
        Arm();
    }

    private void Update()
    {
        if (!_armed || Time.time < _explodeAt)
            return;

        Explode();
    }

    public void Arm()
    {
        _armed = true;
        _explodeAt = Time.time + Mathf.Max(0.05f, fuseTime);
    }

    private void Explode()
    {
        if (explosionEffectPrefab != null)
            Instantiate(explosionEffectPrefab, transform.position + explosionOffset, Quaternion.identity);

        KillNearbyCows();

        _armed = false;

        if (destroyAfterExplosion)
            Destroy(gameObject);
    }

    private void KillNearbyCows()
    {
        float radius = Mathf.Max(0f, cowKillRadius);
        if (radius <= 0f)
            return;

        Vector3 explosionPosition = transform.position + explosionOffset;

        Collider2D[] hitColliders2D = Physics2D.OverlapCircleAll(explosionPosition, radius);
        for (int index = 0; index < hitColliders2D.Length; index++)
        {
            Collider2D hitCollider = hitColliders2D[index];
            if (hitCollider == null)
                continue;

            DestroyCowFromTransform(hitCollider.transform);
        }

        Collider[] hitColliders3D = Physics.OverlapSphere(explosionPosition, radius);
        for (int index = 0; index < hitColliders3D.Length; index++)
        {
            Collider hitCollider = hitColliders3D[index];
            if (hitCollider == null)
                continue;

            DestroyCowFromTransform(hitCollider.transform);
        }
    }

    private void DestroyCowFromTransform(Transform hitTransform)
    {
        EnemyCowTarget cowTarget = hitTransform.GetComponentInParent<EnemyCowTarget>();
        if (cowTarget == null)
            return;

        Transform cowRoot = cowTarget.transform.root;
        Destroy(cowRoot != null ? cowRoot.gameObject : cowTarget.gameObject);
    }
}