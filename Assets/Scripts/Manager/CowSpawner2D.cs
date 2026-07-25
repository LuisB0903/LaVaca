using UnityEngine;

[DisallowMultipleComponent]
public class CowSpawner2D : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private GameObject cowPrefab;
    [SerializeField] private Transform spawnParent;

    [Header("Reglas")]
    [SerializeField] private float checkInterval = 0.5f;
    [SerializeField] private int minimumVisibleCows = 1;
    [SerializeField] private int maximumAliveCows = 8;
    [SerializeField] private bool spawnOffscreenPopulation = true;

    [Header("Jugador")]
    [SerializeField] private string playerTag = "Player";

    [Header("Spawn Visible")]
    [SerializeField] private float visiblePaddingX = 0.8f;
    [SerializeField] private float visiblePaddingY = 0.8f;

    [Header("Spawn Fuera De Pantalla")]
    [SerializeField] private float offscreenMargin = 1.5f;
    [SerializeField] private float minSpawnDistanceFromPlayer = 4f;
    [SerializeField] private float maxSpawnDistanceFromPlayer = 8f;
    [SerializeField] private float spawnPlaneZ = 0f;
    [SerializeField] private bool addEnemyCowTarget = true;

    private float _nextCheckTime;

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
    }

    private void Update()
    {
        if (Time.time < _nextCheckTime)
            return;

        _nextCheckTime = Time.time + Mathf.Max(0.1f, checkInterval);
        MaintainCowPopulation();
    }

    public void ForceSpawnVisibleCow()
    {
        SpawnVisibleCow();
    }

    public void ForceSpawnOffscreenCow()
    {
        SpawnOffscreenCow();
    }

    private void MaintainCowPopulation()
    {
        if (cowPrefab == null || gameplayCamera == null)
            return;

        EnemyCowTarget[] cows = Object.FindObjectsByType<EnemyCowTarget>(FindObjectsSortMode.None);
        int aliveCount = cows.Length;
        int visibleCount = CountVisibleCows(cows);

        while (visibleCount < minimumVisibleCows && aliveCount < maximumAliveCows)
        {
            if (!SpawnVisibleCow())
                return;

            aliveCount++;
            visibleCount++;
        }

        if (!spawnOffscreenPopulation)
            return;

        if (aliveCount >= maximumAliveCows)
            return;

        SpawnOffscreenCow();
    }

    private int CountVisibleCows(EnemyCowTarget[] cows)
    {
        if (cows == null || cows.Length == 0)
            return 0;

        int visibleCount = 0;
        for (int index = 0; index < cows.Length; index++)
        {
            EnemyCowTarget cow = cows[index];
            if (cow == null)
                continue;

            if (IsInsideVisibleBounds(cow.transform.position))
                visibleCount++;
        }

        return visibleCount;
    }

    private bool SpawnVisibleCow()
    {
        if (!TryGetVisibleSpawnPosition(out Vector3 spawnPosition))
            return false;

        SpawnCowAt(spawnPosition);
        return true;
    }

    private void SpawnOffscreenCow()
    {
        if (!TryGetOffscreenSpawnPosition(out Vector3 spawnPosition))
            return;

        SpawnCowAt(spawnPosition);
    }

    private void SpawnCowAt(Vector3 spawnPosition)
    {
        GameObject cow = Instantiate(cowPrefab, spawnPosition, Quaternion.identity, spawnParent);

        if (addEnemyCowTarget && cow.GetComponentInChildren<EnemyCowTarget>() == null)
            cow.AddComponent<EnemyCowTarget>();
    }

    private bool TryGetVisibleSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        if (gameplayCamera == null)
            return false;

        GetCameraWorldBoundsAtDepth(spawnPlaneZ, out Vector3 minBounds, out Vector3 maxBounds);

        float minX = minBounds.x + visiblePaddingX;
        float maxX = maxBounds.x - visiblePaddingX;
        float minY = minBounds.y + visiblePaddingY;
        float maxY = maxBounds.y - visiblePaddingY;

        if (minX > maxX)
        {
            minX = maxX = (minBounds.x + maxBounds.x) * 0.5f;
        }

        if (minY > maxY)
        {
            minY = maxY = (minBounds.y + maxBounds.y) * 0.5f;
        }

        spawnPosition = new Vector3(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY),
            spawnPlaneZ
        );

        return true;
    }

    private bool TryGetOffscreenSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        if (gameplayCamera == null)
            return false;

        GetCameraWorldBoundsAtDepth(spawnPlaneZ, out Vector3 minBounds, out Vector3 maxBounds);

        Vector3 center = playerTarget != null ? playerTarget.position : gameplayCamera.transform.position;
        float minDistance = Mathf.Max(0f, minSpawnDistanceFromPlayer);
        float maxDistance = Mathf.Max(minDistance, maxSpawnDistanceFromPlayer);

        for (int attempt = 0; attempt < 12; attempt++)
        {
            int side = Random.Range(0, 4);
            Vector3 candidate = center;

            switch (side)
            {
                case 0:
                    candidate.x = minBounds.x - offscreenMargin;
                    candidate.y = Random.Range(minBounds.y - offscreenMargin, maxBounds.y + offscreenMargin);
                    break;
                case 1:
                    candidate.x = maxBounds.x + offscreenMargin;
                    candidate.y = Random.Range(minBounds.y - offscreenMargin, maxBounds.y + offscreenMargin);
                    break;
                case 2:
                    candidate.x = Random.Range(minBounds.x - offscreenMargin, maxBounds.x + offscreenMargin);
                    candidate.y = minBounds.y - offscreenMargin;
                    break;
                default:
                    candidate.x = Random.Range(minBounds.x - offscreenMargin, maxBounds.x + offscreenMargin);
                    candidate.y = maxBounds.y + offscreenMargin;
                    break;
            }

            candidate.z = spawnPlaneZ;

            Vector3 fromPlayer = candidate - center;
            fromPlayer.z = 0f;

            float distance = fromPlayer.magnitude;
            if (distance < minDistance || distance > maxDistance)
                continue;

            if (IsInsideVisibleBounds(candidate))
                continue;

            spawnPosition = candidate;
            return true;
        }

        return TryGetVisibleSpawnPosition(out spawnPosition);
    }

    private bool IsInsideVisibleBounds(Vector3 worldPosition)
    {
        if (gameplayCamera == null)
            return true;

        GetCameraWorldBoundsAtDepth(worldPosition.z, out Vector3 minBounds, out Vector3 maxBounds);

        float minX = minBounds.x + visiblePaddingX;
        float maxX = maxBounds.x - visiblePaddingX;
        float minY = minBounds.y + visiblePaddingY;
        float maxY = maxBounds.y - visiblePaddingY;

        if (minX > maxX || minY > maxY)
            return true;

        return worldPosition.x >= minX && worldPosition.x <= maxX && worldPosition.y >= minY && worldPosition.y <= maxY;
    }

    private void GetCameraWorldBoundsAtDepth(float worldZ, out Vector3 minBounds, out Vector3 maxBounds)
    {
        float depthFromCamera = Mathf.Abs(worldZ - gameplayCamera.transform.position.z);
        minBounds = gameplayCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depthFromCamera));
        maxBounds = gameplayCamera.ViewportToWorldPoint(new Vector3(1f, 1f, depthFromCamera));
    }

    private void OnValidate()
    {
        checkInterval = Mathf.Max(0.1f, checkInterval);
        minimumVisibleCows = Mathf.Max(1, minimumVisibleCows);
        maximumAliveCows = Mathf.Max(minimumVisibleCows, maximumAliveCows);
        visiblePaddingX = Mathf.Max(0f, visiblePaddingX);
        visiblePaddingY = Mathf.Max(0f, visiblePaddingY);
        offscreenMargin = Mathf.Max(0f, offscreenMargin);
        minSpawnDistanceFromPlayer = Mathf.Max(0f, minSpawnDistanceFromPlayer);
        maxSpawnDistanceFromPlayer = Mathf.Max(minSpawnDistanceFromPlayer, maxSpawnDistanceFromPlayer);
    }
}