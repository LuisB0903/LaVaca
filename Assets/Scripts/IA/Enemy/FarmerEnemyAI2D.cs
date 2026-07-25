using UnityEngine;

[DisallowMultipleComponent]
public class FarmerEnemyAI2D : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private string playerTag = "Player";

    [Header("Vacas")]
    [SerializeField] private bool autoFindCows = true;
    [SerializeField] private Transform[] cowTargets;
    [SerializeField] private string cowTag = "Cow";
    [SerializeField] private float cowSearchInterval = 0.25f;

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float arriveDistance = 0.15f;
    [SerializeField] private float patrolRadius = 4f;
    [SerializeField] private float patrolRefreshTime = 2.5f;
    [SerializeField] private Vector2 cameraPadding = new Vector2(0.8f, 0.8f);
    [SerializeField] private float cowAcquirePadding = 0.8f;
    [SerializeField] private float cowContactKillRadius = 0.35f;

    [Header("Bombas")]
    [SerializeField] private EnemyBombDropper2D bombDropper;

    [Header("Disparo")]
    [SerializeField] private EnemyProjectileShooter2D projectileShooter;
    [SerializeField] private float projectileUnlockDelay = 60f;

    private float _elapsedTime;
    private float _nextCowScanTime;
    private float _nextPatrolRefreshTime;
    private Transform _currentCowTarget;
    private Vector3 _currentPatrolPoint;
    private bool _hasPatrolPoint;

    private void Awake()
    {
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;

        if (playerTarget == null && !string.IsNullOrWhiteSpace(playerTag))
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null)
                playerTarget = playerObject.transform;
        }

        if (bombDropper == null)
            bombDropper = GetComponent<EnemyBombDropper2D>();

        if (projectileShooter == null)
            projectileShooter = GetComponent<EnemyProjectileShooter2D>();

        if (projectileShooter != null)
        {
            projectileShooter.SetTarget(playerTarget);
            projectileShooter.SetAttackEnabled(false);
        }

        PickNewPatrolPoint();
    }

    private void Update()
    {
        _elapsedTime += Time.deltaTime;

        RefreshCowTargetIfNeeded();
        MoveTowardsCurrentGoal();
        KillNearbyCowsOnContact();

        if (bombDropper != null)
            bombDropper.Tick(_elapsedTime);

        if (projectileShooter != null)
        {
            projectileShooter.SetTarget(playerTarget);
            projectileShooter.SetAttackEnabled(_elapsedTime >= projectileUnlockDelay);
            projectileShooter.Tick(_elapsedTime);
        }
    }

    private void RefreshCowTargetIfNeeded()
    {
        if (Time.time < _nextCowScanTime)
            return;

        _nextCowScanTime = Time.time + Mathf.Max(0.05f, cowSearchInterval);
        _currentCowTarget = FindClosestCowTarget();
    }

    private Transform FindClosestCowTarget()
    {
        Transform bestTarget = null;
        float bestDistance = float.MaxValue;
        Vector3 currentPosition = transform.position;

        if (cowTargets != null)
        {
            for (int index = 0; index < cowTargets.Length; index++)
            {
                Transform candidate = cowTargets[index];
                if (candidate == null)
                    continue;

                if (!IsWithinCowAcquisitionBounds(candidate.position))
                    continue;

                float candidateDistance = (candidate.position - currentPosition).sqrMagnitude;
                if (candidateDistance < bestDistance)
                {
                    bestDistance = candidateDistance;
                    bestTarget = candidate;
                }
            }
        }

        if (!autoFindCows)
            return bestTarget;

        EnemyCowTarget[] markedCows = Object.FindObjectsByType<EnemyCowTarget>(FindObjectsSortMode.None);
        for (int index = 0; index < markedCows.Length; index++)
        {
            Transform candidate = markedCows[index].transform;
            if (!IsWithinCowAcquisitionBounds(candidate.position))
                continue;

            float candidateDistance = (candidate.position - currentPosition).sqrMagnitude;
            if (candidateDistance < bestDistance)
            {
                bestDistance = candidateDistance;
                bestTarget = candidate;
            }
        }

        if (bestTarget != null)
            return bestTarget;

        if (!string.IsNullOrWhiteSpace(cowTag))
        {
            try
            {
                GameObject[] taggedCows = GameObject.FindGameObjectsWithTag(cowTag);
                for (int index = 0; index < taggedCows.Length; index++)
                {
                    Transform candidate = taggedCows[index].transform;
                    if (!IsWithinCowAcquisitionBounds(candidate.position))
                        continue;

                    float candidateDistance = (candidate.position - currentPosition).sqrMagnitude;
                    if (candidateDistance < bestDistance)
                    {
                        bestDistance = candidateDistance;
                        bestTarget = candidate;
                    }
                }
            }
            catch (UnityException)
            {
            }
        }

        return bestTarget;
    }

    private void MoveTowardsCurrentGoal()
    {
        Vector3 desiredPosition = GetDesiredPosition();
        desiredPosition = ClampInsideCameraBounds(desiredPosition);

        Vector3 currentPosition = transform.position;
        Vector3 direction = desiredPosition - currentPosition;
        direction.z = 0f;

        if (direction.sqrMagnitude <= arriveDistance * arriveDistance)
            return;

        transform.position = Vector3.MoveTowards(
            currentPosition,
            desiredPosition,
            moveSpeed * Time.deltaTime
        );
    }

    private Vector3 GetDesiredPosition()
    {
        if (_currentCowTarget != null)
            return _currentCowTarget.position;

        if (!_hasPatrolPoint || Time.time >= _nextPatrolRefreshTime || Vector3.Distance(transform.position, _currentPatrolPoint) <= arriveDistance)
            PickNewPatrolPoint();

        return _currentPatrolPoint;
    }

    private void PickNewPatrolPoint()
    {
        _hasPatrolPoint = true;
        _nextPatrolRefreshTime = Time.time + Mathf.Max(0.25f, patrolRefreshTime);

        if (gameplayCamera == null)
        {
            _currentPatrolPoint = transform.position + Random.insideUnitSphere * patrolRadius;
            _currentPatrolPoint.z = transform.position.z;
            return;
        }

        GetCameraWorldBoundsAtDepth(transform.position.z, out Vector3 minBounds, out Vector3 maxBounds);
        float patrolX = Random.Range(minBounds.x + cameraPadding.x, maxBounds.x - cameraPadding.x);
        float patrolY = Random.Range(minBounds.y + cameraPadding.y, maxBounds.y - cameraPadding.y);

        if (minBounds.x + cameraPadding.x > maxBounds.x - cameraPadding.x)
            patrolX = transform.position.x;

        if (minBounds.y + cameraPadding.y > maxBounds.y - cameraPadding.y)
            patrolY = transform.position.y;

        _currentPatrolPoint = new Vector3(patrolX, patrolY, transform.position.z);
    }

    private Vector3 ClampInsideCameraBounds(Vector3 targetPosition)
    {
        if (gameplayCamera == null)
            return targetPosition;

        GetCameraWorldBoundsAtDepth(targetPosition.z, out Vector3 minBounds, out Vector3 maxBounds);

        float minX = minBounds.x + cameraPadding.x;
        float maxX = maxBounds.x - cameraPadding.x;
        float minY = minBounds.y + cameraPadding.y;
        float maxY = maxBounds.y - cameraPadding.y;

        if (minX <= maxX)
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        else
            targetPosition.x = (minBounds.x + maxBounds.x) * 0.5f;

        if (minY <= maxY)
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
        else
            targetPosition.y = (minBounds.y + maxBounds.y) * 0.5f;

        targetPosition.z = transform.position.z;
        return targetPosition;
    }

    private void GetCameraWorldBoundsAtDepth(float worldZ, out Vector3 minBounds, out Vector3 maxBounds)
    {
        float depthFromCamera = Mathf.Abs(worldZ - gameplayCamera.transform.position.z);
        minBounds = gameplayCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depthFromCamera));
        maxBounds = gameplayCamera.ViewportToWorldPoint(new Vector3(1f, 1f, depthFromCamera));
    }

    private bool IsWithinCowAcquisitionBounds(Vector3 worldPosition)
    {
        if (gameplayCamera == null)
            return true;

        GetCameraWorldBoundsAtDepth(worldPosition.z, out Vector3 minBounds, out Vector3 maxBounds);

        float minX = minBounds.x + cowAcquirePadding;
        float maxX = maxBounds.x - cowAcquirePadding;
        float minY = minBounds.y + cowAcquirePadding;
        float maxY = maxBounds.y - cowAcquirePadding;

        if (minX > maxX || minY > maxY)
            return true;

        return worldPosition.x >= minX && worldPosition.x <= maxX && worldPosition.y >= minY && worldPosition.y <= maxY;
    }

    public void SetPlayerTarget(Transform newTarget)
    {
        playerTarget = newTarget;
    }

    public void SetCowTargets(Transform[] newCowTargets)
    {
        cowTargets = newCowTargets;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDestroyCow(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDestroyCow(other);
    }

    private void TryDestroyCow(Collider2D hitCollider)
    {
        if (hitCollider == null)
            return;

        EnemyCowTarget cowTarget = hitCollider.GetComponentInParent<EnemyCowTarget>();

        if (cowTarget != null)
            DestroyCowRoot(cowTarget);
    }

    private void KillNearbyCowsOnContact()
    {
        float radius = Mathf.Max(0f, cowContactKillRadius);
        if (radius <= 0f)
            return;

        Collider2D[] hitColliders2D = Physics2D.OverlapCircleAll(transform.position, radius);
        for (int index = 0; index < hitColliders2D.Length; index++)
            TryDestroyCow(hitColliders2D[index]);

        Collider[] hitColliders3D = Physics.OverlapSphere(transform.position, radius);
        for (int index = 0; index < hitColliders3D.Length; index++)
        {
            Collider hitCollider = hitColliders3D[index];
            if (hitCollider == null)
                continue;

            EnemyCowTarget cowTarget = hitCollider.GetComponentInParent<EnemyCowTarget>();
            if (cowTarget != null)
                DestroyCowRoot(cowTarget);
        }
    }

    private void DestroyCowRoot(EnemyCowTarget cowTarget)
    {
        Transform cowRoot = cowTarget.transform.root;
        Destroy(cowRoot != null ? cowRoot.gameObject : cowTarget.gameObject);
    }
}