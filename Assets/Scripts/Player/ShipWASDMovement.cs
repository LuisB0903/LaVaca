using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipWASDMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Velocidad máxima de la nave.")]
    [SerializeField] private float maxSpeed = 6f;

    [Tooltip("Rapidez con la que la nave gana velocidad.")]
    [SerializeField] private float acceleration = 12f;

    [Tooltip("Rapidez con la que la nave pierde velocidad.")]
    [SerializeField] private float deceleration = 8f;

    [Header("Plano de movimiento")]
    [Tooltip("Actívalo si la nave se mueve sobre los ejes X y Z.")]
    [SerializeField] private bool moveOnXZPlane = true;

    [Header("Animación")]
    [SerializeField] private Animator animator;

    [Tooltip("Velocidad mínima para considerar que la nave se está moviendo.")]
    [SerializeField] private float animationMovementThreshold = 0.1f;

    private Rigidbody shipRigidbody;
    private Vector2 movementInput;
    private Vector3 currentVelocity;

    private static readonly int HorizontalHash =
        Animator.StringToHash("Horizontal");

    private static readonly int VerticalHash =
        Animator.StringToHash("Vertical");

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");

    public Vector3 CurrentVelocity => currentVelocity;
    public float CurrentSpeed => currentVelocity.magnitude;

    private void Awake()
    {
        shipRigidbody = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        shipRigidbody.useGravity = false;
        shipRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        shipRigidbody.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;
    }

    private void Update()
    {
        movementInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        movementInput = Vector2.ClampMagnitude(
            movementInput,
            1f
        );
    }

    private void FixedUpdate()
    {
        Vector3 targetVelocity;

        if (moveOnXZPlane)
        {
            targetVelocity = new Vector3(
                movementInput.x,
                0f,
                movementInput.y
            ) * maxSpeed;
        }
        else
        {
            targetVelocity = new Vector3(
                movementInput.x,
                movementInput.y,
                0f
            ) * maxSpeed;
        }

        float velocityChangeRate =
            movementInput.sqrMagnitude > 0.01f
                ? acceleration
                : deceleration;

        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            targetVelocity,
            velocityChangeRate * Time.fixedDeltaTime
        );

        // Elimina velocidades residuales.
        if (movementInput.sqrMagnitude <= 0.01f &&
            currentVelocity.sqrMagnitude <= 0.01f)
        {
            currentVelocity = Vector3.zero;
        }

        shipRigidbody.MovePosition(
            shipRigidbody.position +
            currentVelocity * Time.fixedDeltaTime
        );

        UpdateAnimation();
    }

    private void UpdateAnimation()
{
    if (animator == null)
        return;

    float horizontalVelocity = currentVelocity.x;

    float verticalVelocity = moveOnXZPlane
        ? currentVelocity.z
        : currentVelocity.y;

    float thresholdSquared =
        animationMovementThreshold * animationMovementThreshold;

    bool isMoving =
        currentVelocity.sqrMagnitude > thresholdSquared;

    if (!isMoving)
    {
        animator.SetBool(IsMovingHash, false);
        animator.SetFloat(HorizontalHash, 0f);
        animator.SetFloat(VerticalHash, 0f);
        return;
    }

    float horizontalAnimation = 0f;
    float verticalAnimation = 0f;

    /*
     * En movimiento diagonal se utiliza el eje que tenga
     * mayor velocidad para evitar activar dos animaciones.
     */
    if (Mathf.Abs(horizontalVelocity) >
        Mathf.Abs(verticalVelocity))
    {
        horizontalAnimation =
            horizontalVelocity > 0f ? 1f : -1f;
    }
    else
    {
        verticalAnimation =
            verticalVelocity > 0f ? 1f : -1f;
    }

    animator.SetBool(IsMovingHash, true);
    animator.SetFloat(HorizontalHash, horizontalAnimation);
    animator.SetFloat(VerticalHash, verticalAnimation);
}

    private void OnDisable()
    {
        movementInput = Vector2.zero;
        currentVelocity = Vector3.zero;

        if (shipRigidbody != null)
            shipRigidbody.linearVelocity = Vector3.zero;

        if (animator != null)
        {
            animator.SetBool(IsMovingHash, false);
            animator.SetFloat(HorizontalHash, 0f);
            animator.SetFloat(VerticalHash, 0f);
        }
    }
}


/* using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipWASDMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float maxSpeed = 6f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 8f;

    [Header("Plano de movimiento")]
    [SerializeField] private bool moveOnXZPlane = true;

    [Header("Animación")]
[SerializeField] private Animator animator;

    private Rigidbody shipRigidbody;
    private Vector2 movementInput;
    private Vector3 currentVelocity;

    public Vector3 CurrentVelocity => currentVelocity;
    public float CurrentSpeed => currentVelocity.magnitude;

    public bool IsStopped(float tolerance = 0.15f)
    {
        return currentVelocity.sqrMagnitude <= tolerance * tolerance;
    }

    private void Awake()
    {
        if (animator == null)
    animator = GetComponentInChildren<Animator>();

        shipRigidbody = GetComponent<Rigidbody>();

        shipRigidbody.useGravity = false;
        shipRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        shipRigidbody.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;
    }

    private void Update()
    {
        movementInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        movementInput = Vector2.ClampMagnitude(
            movementInput,
            1f
        );

        movementInput = new Vector2(
    Input.GetAxisRaw("Horizontal"),
    Input.GetAxisRaw("Vertical")
);

movementInput = Vector2.ClampMagnitude(
    movementInput,
    1f
);

UpdateAnimation();
    }

    private void UpdateAnimation()
{
    animator.SetBool(
        "IsMoving",
        movementInput.sqrMagnitude > 0.01f
    );

    animator.SetFloat(
        "Horizontal",
        movementInput.x
    );
}

    private void FixedUpdate()
    {
        Vector3 targetVelocity;

        if (moveOnXZPlane)
        {
            targetVelocity = new Vector3(
                movementInput.x,
                0f,
                movementInput.y
            ) * maxSpeed;
        }
        else
        {
            targetVelocity = new Vector3(
                movementInput.x,
                movementInput.y,
                0f
            ) * maxSpeed;
        }

        float velocityChangeRate =
            movementInput.sqrMagnitude > 0.01f
                ? acceleration
                : deceleration;

        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            targetVelocity,
            velocityChangeRate * Time.fixedDeltaTime
        );

        // Elimina velocidades residuales muy pequeñas.
        if (movementInput.sqrMagnitude <= 0.01f &&
            currentVelocity.sqrMagnitude <= 0.01f)
        {
            currentVelocity = Vector3.zero;
        }

        shipRigidbody.MovePosition(
            shipRigidbody.position +
            currentVelocity * Time.fixedDeltaTime
        );
    }

    private void OnDisable()
    {
        movementInput = Vector2.zero;
        currentVelocity = Vector3.zero;

        if (shipRigidbody != null)
            shipRigidbody.linearVelocity = Vector3.zero;
    }
} */