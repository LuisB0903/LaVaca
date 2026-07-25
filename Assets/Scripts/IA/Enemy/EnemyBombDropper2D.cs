using UnityEngine;

[DisallowMultipleComponent]
public class EnemyBombDropper2D : MonoBehaviour
{
    [Header("Bombas")]
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, -0.25f, 0f);
    [SerializeField] private float dropInterval = 4f;
    [SerializeField] private float startDelay = 0f;

    private float _nextDropTime;

    public void Tick(float elapsedTime)
    {
        if (bombPrefab == null)
            return;

        if (elapsedTime < startDelay)
            return;

        if (elapsedTime < _nextDropTime)
            return;

        _nextDropTime = elapsedTime + Mathf.Max(0.1f, dropInterval);

        Vector3 spawnPosition = dropPoint != null ? dropPoint.position : transform.position;
        spawnPosition += transform.TransformVector(localOffset);

        Quaternion spawnRotation = dropPoint != null ? dropPoint.rotation : transform.rotation;
        Instantiate(bombPrefab, spawnPosition, spawnRotation);
    }

    public void SetBombPrefab(GameObject newBombPrefab)
    {
        bombPrefab = newBombPrefab;
    }
}