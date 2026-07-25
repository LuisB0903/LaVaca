using UnityEngine;

public class ShipMouseMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("Distancia m�nima para considerar que la nave lleg� al cursor.")]
    [SerializeField] private float stoppingDistance = 0.1f;

    [Header("Configuraci�n")]
    [Tooltip("C�mara utilizada para convertir la posici�n del mouse al mundo.")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("Plano sobre el cual se mueve la nave.")]
    [SerializeField] private float movementPlaneZ = 0f;

    [Tooltip("Permite que la nave rote hacia la direcci�n del movimiento.")]
    [SerializeField] private bool rotateTowardsMovement = false;

    [SerializeField] private float rotationSpeed = 8f;

    private Vector3 targetPosition;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        targetPosition = transform.position;
    }

    private void Update()
    {
        UpdateTargetPosition();
        MoveTowardsTarget();
    }

    private void UpdateTargetPosition()
    {
        if (mainCamera == null)
            return;

        Vector3 mousePosition = Input.mousePosition;

        float distanceFromCamera = Mathf.Abs(
            movementPlaneZ - mainCamera.transform.position.z
        );

        mousePosition.z = distanceFromCamera;

        targetPosition = mainCamera.ScreenToWorldPoint(mousePosition);
        // targetPosition.z = movementPlaneZ;
    }

    private void MoveTowardsTarget()
    {
        Vector3 direction = targetPosition - transform.position;
        direction.z = 0f;

        if (direction.sqrMagnitude <= stoppingDistance * stoppingDistance)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        if (rotateTowardsMovement)
            RotateTowardsDirection(direction);
    }

    private void RotateTowardsDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}