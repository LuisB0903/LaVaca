using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CowRaycastCollector : MonoBehaviour
{
    [Header("Puntos de referencia")]

    [Tooltip("Punto desde donde se lanza la deteccion hacia abajo.")]
    [SerializeField] private Transform rayOrigin;

    [Tooltip("Punto hacia donde sera absorbida la vaca.")]
    [SerializeField] private Transform absorptionPoint;

    [Header("Deteccion")]

    [Tooltip("Capas que pueden contener vacas recolectables.")]
    [SerializeField] private LayerMask cowLayer;

    [Tooltip("Distancia maxima de deteccion hacia abajo.")]
    [Min(0.1f)]
    [SerializeField] private float detectionDistance = 10f;

    [Tooltip("Radio del SphereCast. Usa 0 para convertirlo en un Raycast normal.")]
    [Min(0f)]
    [SerializeField] private float detectionRadius = 0.3f;

    [Tooltip("Tiempo que la nave debe mantenerse sobre la vaca antes de iniciar la absorcion.")]
    [Min(0f)]
    [SerializeField] private float timeBeforeAbsorption = 1.5f;

    [Tooltip("Permite detectar colliders configurados como Trigger.")]
    [SerializeField]
    private QueryTriggerInteraction triggerInteraction =
        QueryTriggerInteraction.Collide;

    [Header("Condiciones para iniciar la absorcion")]

    [Tooltip("Solo acumula tiempo cuando la nave esta practicamente detenida.")]
    [SerializeField] private bool requireShipStopped = true;

    [Tooltip("Velocidad maxima permitida para considerar que la nave esta detenida.")]
    [Min(0f)]
    [SerializeField] private float maximumSpeedForDetection = 0.1f;

    [Tooltip("Distancia horizontal maxima permitida entre el centro del rayo y la vaca.")]
    [Min(0f)]
    [SerializeField] private float maximumHorizontalDistance = 0.5f;

    [Tooltip("Reinicia el contador si la nave se mueve o deja de estar centrada.")]
    [SerializeField] private bool resetTimerWhenConditionsFail = true;

    [Header("Absorcion")]

    [Tooltip("Velocidad con la que la vaca se mueve hacia la nave.")]
    [Min(0.1f)]
    [SerializeField] private float absorptionSpeed = 5f;

    [Tooltip("Hace que la vaca gire mientras esta siendo abducida.")]
    [SerializeField] private bool rotateCowDuringAbsorption = true;

    [Tooltip("Velocidad de rotacion de la vaca durante la absorcion.")]
    [Min(0f)]
    [SerializeField] private float absorptionRotationSpeed = 360f;

    [Tooltip("Escala final de la vaca antes de desaparecer.")]
    [Range(0f, 1f)]
    [SerializeField] private float finalScaleMultiplier = 0.15f;

    [Tooltip("Distancia a la que se considera terminada la absorcion.")]
    [Min(0.001f)]
    [SerializeField] private float absorptionCompletionDistance = 0.05f;

    [Tooltip("Destruye la vaca cuando termina la absorcion.")]
    [SerializeField] private bool destroyCowAfterAbsorption = true;

    [Header("Debug")]

    [Tooltip("Muestra el SphereCast en la vista Scene durante la ejecucion.")]
    [SerializeField] private bool showDebugCast = true;

    [Tooltip("Muestra el SphereCast aunque la nave no este seleccionada.")]
    [SerializeField] private bool showGizmosAlways = true;

    [Tooltip("Muestra informacion del contador y las condiciones en la consola.")]
    [SerializeField] private bool showTimerDebug = false;

    [Tooltip("Color cuando no se detecta ninguna vaca.")]
    [SerializeField] private Color noDetectionColor = Color.red;

    [Tooltip("Color cuando se detecta una vaca, pero aun no se puede acumular tiempo.")]
    [SerializeField] private Color waitingColor = new Color(1f, 0.5f, 0f);

    [Tooltip("Color cuando se esta acumulando el tiempo de deteccion.")]
    [SerializeField] private Color detectingColor = Color.yellow;

    [Tooltip("Color cuando comienza la absorcion.")]
    [SerializeField] private Color absorbingColor = Color.green;

    [Header("Eventos")]

    [Tooltip("Se ejecuta cuando comienza la absorcion.")]
    public UnityEvent onAbsorptionStarted;

    [Tooltip("Se ejecuta cuando termina la recoleccion.")]
    public UnityEvent onCowCollected;


    [Header("Puntaje")]
    [SerializeField] private CowScoreManager scoreManager;


    [Header("Nave")]
[SerializeField] private ShipWASDMovement shipMovement;

    private CollectibleCow currentCow;

    private float detectionTimer;
    private float currentShipSpeed;
    private float currentHorizontalDistance;

    private bool isAbsorbing;
    private bool cowDetectedThisFrame;
    private bool detectionConditionsMet;

    private Vector3 previousShipPosition;

    private RaycastHit currentHit;
    private Coroutine absorptionCoroutine;

    public CollectibleCow CurrentCow => currentCow;
    public bool IsAbsorbing => isAbsorbing;
    public bool DetectionConditionsMet => detectionConditionsMet;

    public float DetectionTimer => detectionTimer;
    public float CurrentShipSpeed => currentShipSpeed;
    public float CurrentHorizontalDistance => currentHorizontalDistance;

    public float DetectionProgress
    {
        get
        {
            if (timeBeforeAbsorption <= 0f)
                return currentCow != null ? 1f : 0f;

            return Mathf.Clamp01(
                detectionTimer / timeBeforeAbsorption
            );
        }
    }

    private void Reset()
    {
        rayOrigin = transform;
        absorptionPoint = transform;
    }

    private void Awake()
    {
        if (rayOrigin == null)
            rayOrigin = transform;

        if (absorptionPoint == null)
            absorptionPoint = transform;

        previousShipPosition = transform.position;


        if (shipMovement == null)
{
    shipMovement = GetComponentInParent<ShipWASDMovement>();
}

        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<CowScoreManager>();
        }
    }

    private void OnEnable()
    {
        previousShipPosition = transform.position;
        currentShipSpeed = 0f;
    }

    private void Update()
    {
        CalculateShipSpeed();

        if (isAbsorbing)
        {
            DrawRuntimeDebug();
            return;
        }

        CheckForCow();
        DrawRuntimeDebug();
    }

    private void CalculateShipSpeed()
{
    if (shipMovement != null)
    {
        currentShipSpeed = shipMovement.CurrentSpeed;
        return;
    }

    if (Time.deltaTime <= 0f)
        return;

    Vector3 currentPosition = transform.position;
    Vector3 movement = currentPosition - previousShipPosition;

    Vector3 horizontalMovement =
        Vector3.ProjectOnPlane(movement, Vector3.up);

    currentShipSpeed =
        horizontalMovement.magnitude / Time.deltaTime;

    previousShipPosition = currentPosition;
}

    /* private void CalculateShipSpeed()
    {
        if (Time.deltaTime <= 0f)
            return;

        Vector3 currentPosition = transform.position;
        Vector3 movement = currentPosition - previousShipPosition;

        // Ignora el movimiento vertical y mide el desplazamiento sobre el terreno.
        Vector3 horizontalMovement =
            Vector3.ProjectOnPlane(movement, Vector3.up);

        currentShipSpeed =
            horizontalMovement.magnitude / Time.deltaTime;

        previousShipPosition = currentPosition;
    } */

    private void CheckForCow()
    {
        cowDetectedThisFrame =
            PerformCast(out RaycastHit hit);

        detectionConditionsMet = false;

        if (!cowDetectedThisFrame)
        {
            ResetDetection();
            return;
        }

        currentHit = hit;

        CollectibleCow detectedCow =
            hit.collider.GetComponentInParent<CollectibleCow>();

        if (detectedCow == null ||
            detectedCow.IsBeingCollected ||
            detectedCow.HasBeenCollected)
        {
            ResetDetection();
            return;
        }

        // Si se detecta una vaca diferente, el contador comienza desde cero.
        if (detectedCow != currentCow)
        {
            currentCow = detectedCow;
            detectionTimer = 0f;
        }

        Vector3 originPosition = GetRayOrigin();
        Vector3 cowPosition = detectedCow.transform.position;

        // Elimina la diferencia vertical para medir solamente el centrado horizontal.
        Vector3 horizontalDifference =
            Vector3.ProjectOnPlane(
                cowPosition - originPosition,
                Vector3.up
            );

        currentHorizontalDistance =
            horizontalDifference.magnitude;

        bool shipIsStopped =
            !requireShipStopped ||
            currentShipSpeed <= maximumSpeedForDetection;

        bool shipIsCentered =
            currentHorizontalDistance <= maximumHorizontalDistance;

        detectionConditionsMet =
            shipIsStopped && shipIsCentered;

        if (!detectionConditionsMet)
        {
            if (resetTimerWhenConditionsFail)
                detectionTimer = 0f;

            ShowWaitingDebug(
                detectedCow,
                shipIsStopped,
                shipIsCentered
            );

            return;
        }

        detectionTimer += Time.deltaTime;

        if (showTimerDebug)
        {
            Debug.Log(
                $"[Cow Collector] Detectando: {detectedCow.name} | " +
                $"Tiempo: {detectionTimer:F2} / {timeBeforeAbsorption:F2} | " +
                $"Velocidad: {currentShipSpeed:F2} | " +
                $"Distancia horizontal: {currentHorizontalDistance:F2}",
                this
            );
        }

        if (detectionTimer < timeBeforeAbsorption)
            return;

        if (absorptionCoroutine == null)
        {
            absorptionCoroutine =
                StartCoroutine(AbsorbCow(currentCow));
        }
    }

    private void ShowWaitingDebug(
        CollectibleCow detectedCow,
        bool shipIsStopped,
        bool shipIsCentered
    )
    {
        if (!showTimerDebug)
            return;

        Debug.Log(
            $"[Cow Collector] Esperando sobre: {detectedCow.name} | " +
            $"Nave detenida: {shipIsStopped} | " +
            $"Nave centrada: {shipIsCentered} | " +
            $"Velocidad: {currentShipSpeed:F2} / {maximumSpeedForDetection:F2} | " +
            $"Distancia: {currentHorizontalDistance:F2} / {maximumHorizontalDistance:F2}",
            this
        );
    }

    private bool PerformCast(out RaycastHit hit)
    {
        Vector3 origin = GetRayOrigin();
        Vector3 direction = Vector3.down;

        if (detectionRadius <= 0f)
        {
            return Physics.Raycast(
                origin,
                direction,
                out hit,
                detectionDistance,
                cowLayer,
                triggerInteraction
            );
        }

        return Physics.SphereCast(
            origin,
            detectionRadius,
            direction,
            out hit,
            detectionDistance,
            cowLayer,
            triggerInteraction
        );
    }

    private IEnumerator AbsorbCow(CollectibleCow cow)
    {
        if (cow == null || isAbsorbing)
        {
            absorptionCoroutine = null;
            yield break;
        }

        isAbsorbing = true;
        detectionTimer = timeBeforeAbsorption;

        cow.BeginCollection();
        onAbsorptionStarted?.Invoke();

        Transform cowTransform = cow.transform;

        Vector3 initialScale = cowTransform.localScale;
        Vector3 finalScale =
            initialScale * finalScaleMultiplier;

        while (cow != null)
        {
            Vector3 targetPosition =
                GetAbsorptionPosition();

            cowTransform.position =
                Vector3.MoveTowards(
                    cowTransform.position,
                    targetPosition,
                    absorptionSpeed * Time.deltaTime
                );

            if (rotateCowDuringAbsorption &&
                absorptionRotationSpeed > 0f)
            {
                cowTransform.Rotate(
                    Vector3.forward,
                    absorptionRotationSpeed * Time.deltaTime,
                    Space.World
                );
            }

            float distance =
                Vector3.Distance(
                    cowTransform.position,
                    targetPosition
                );

            float normalizedDistance =
                detectionDistance > 0f
                    ? Mathf.Clamp01(
                        distance / detectionDistance
                    )
                    : 0f;

            cowTransform.localScale =
                Vector3.Lerp(
                    finalScale,
                    initialScale,
                    normalizedDistance
                );

            if (distance <= absorptionCompletionDistance)
                break;

            yield return null;
        }

        if (cow != null)
        {
            cowTransform.position =
                GetAbsorptionPosition();

            cowTransform.localScale =
                finalScale;

            cow.CompleteCollection();
            scoreManager?.RegisterCowCollected();
            onCowCollected?.Invoke();

            if (destroyCowAfterAbsorption)
            {
                Destroy(cow.gameObject);
            }
            else
            {
                cow.gameObject.SetActive(false);
            }
        }

        currentCow = null;
        detectionTimer = 0f;
        currentHorizontalDistance = 0f;

        isAbsorbing = false;
        cowDetectedThisFrame = false;
        detectionConditionsMet = false;
        absorptionCoroutine = null;
    }

    private void ResetDetection()
    {
        currentCow = null;
        detectionTimer = 0f;
        currentHorizontalDistance = 0f;

        cowDetectedThisFrame = false;
        detectionConditionsMet = false;
    }

    private Vector3 GetRayOrigin()
    {
        return rayOrigin != null
            ? rayOrigin.position
            : transform.position;
    }

    private Vector3 GetAbsorptionPosition()
    {
        return absorptionPoint != null
            ? absorptionPoint.position
            : transform.position;
    }

    private void DrawRuntimeDebug()
    {
        if (!showDebugCast)
            return;

        Vector3 origin = GetRayOrigin();

        Vector3 end =
            origin + Vector3.down * detectionDistance;

        Color debugColor = GetCurrentDebugColor();

        Debug.DrawLine(
            origin,
            end,
            debugColor
        );

        if (cowDetectedThisFrame && !isAbsorbing)
        {
            Debug.DrawLine(
                origin,
                currentHit.point,
                debugColor
            );

            DrawDebugCross(
                currentHit.point,
                detectionRadius > 0f
                    ? detectionRadius
                    : 0.2f,
                debugColor
            );
        }
    }

    private Color GetCurrentDebugColor()
    {
        if (isAbsorbing)
            return absorbingColor;

        if (currentCow == null)
            return noDetectionColor;

        if (!detectionConditionsMet)
            return waitingColor;

        return detectingColor;
    }

    private void DrawDebugCross(
        Vector3 position,
        float size,
        Color color
    )
    {
        Debug.DrawLine(
            position - Vector3.right * size,
            position + Vector3.right * size,
            color
        );

        Debug.DrawLine(
            position - Vector3.forward * size,
            position + Vector3.forward * size,
            color
        );

        Debug.DrawLine(
            position - Vector3.up * size,
            position + Vector3.up * size,
            color
        );
    }

    private void OnDrawGizmos()
    {
        if (!showGizmosAlways)
            return;

        DrawCastGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (showGizmosAlways)
            return;

        DrawCastGizmos();
    }

    private void DrawCastGizmos()
    {
        Vector3 origin =
            rayOrigin != null
                ? rayOrigin.position
                : transform.position;

        Vector3 end =
            origin + Vector3.down * detectionDistance;

        Gizmos.color = GetCurrentDebugColor();

        Gizmos.DrawLine(origin, end);

        if (detectionRadius > 0f)
        {
            Gizmos.DrawWireSphere(
                origin,
                detectionRadius
            );

            Gizmos.DrawWireSphere(
                end,
                detectionRadius
            );

            Gizmos.DrawLine(
                origin + Vector3.right * detectionRadius,
                end + Vector3.right * detectionRadius
            );

            Gizmos.DrawLine(
                origin - Vector3.right * detectionRadius,
                end - Vector3.right * detectionRadius
            );

            Gizmos.DrawLine(
                origin + Vector3.forward * detectionRadius,
                end + Vector3.forward * detectionRadius
            );

            Gizmos.DrawLine(
                origin - Vector3.forward * detectionRadius,
                end - Vector3.forward * detectionRadius
            );
        }

        if (Application.isPlaying &&
            cowDetectedThisFrame &&
            !isAbsorbing)
        {
            Gizmos.color = GetCurrentDebugColor();

            Gizmos.DrawWireSphere(
                currentHit.point,
                Mathf.Max(
                    0.1f,
                    detectionRadius
                )
            );
        }
    }
}


/* using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CowRaycastCollector : MonoBehaviour
{
    [Header("Puntos de referencia")]

    [Tooltip("Punto desde donde se lanza la detección hacia abajo.")]
    [SerializeField] private Transform rayOrigin;

    [Tooltip("Punto hacia donde será absorbida la vaca.")]
    [SerializeField] private Transform absorptionPoint;

    [Header("Detección")]

    [Tooltip("Capas que pueden contener vacas recolectables.")]
    [SerializeField] private LayerMask cowLayer;

    [Tooltip("Distancia máxima de detección hacia abajo.")]
    [Min(0.1f)]
    [SerializeField] private float detectionDistance = 10f;

    [Tooltip("Radio del SphereCast. Usa 0 para convertirlo en un Raycast normal.")]
    [Min(0f)]
    [SerializeField] private float detectionRadius = 0.3f;

    [Tooltip("Tiempo que la nave debe mantenerse sobre la vaca.")]
    [Min(0f)]
    [SerializeField] private float timeBeforeAbsorption = 1.5f;

    [Tooltip("Permite detectar colliders configurados como Trigger.")]
    [SerializeField] private QueryTriggerInteraction triggerInteraction =
        QueryTriggerInteraction.Collide;

    [Header("Absorción")]

    [Tooltip("Velocidad con la que la vaca se mueve hacia la nave.")]
    [Min(0.1f)]
    [SerializeField] private float absorptionSpeed = 5f;

    [Tooltip("Hace que la vaca gire mientras está siendo abducida.")]
    [SerializeField] private bool rotateCowDuringAbsorption = true;

    [Tooltip("Velocidad de rotación de la vaca durante la absorción.")]
    [Min(0f)]
    [SerializeField] private float absorptionRotationSpeed = 360f;

    [Tooltip("Escala final de la vaca antes de desaparecer.")]
    [Range(0f, 1f)]
    [SerializeField] private float finalScaleMultiplier = 0.15f;

    [Tooltip("Distancia a la que se considera terminada la absorción.")]
    [Min(0.001f)]
    [SerializeField] private float absorptionCompletionDistance = 0.05f;

    [Tooltip("Destruye la vaca cuando termina la absorción.")]
    [SerializeField] private bool destroyCowAfterAbsorption = true;

    [Header("Debug")]

    [Tooltip("Muestra el SphereCast en la vista Scene durante la ejecución.")]
    [SerializeField] private bool showDebugCast = true;

    [Tooltip("Muestra el SphereCast aunque la nave no esté seleccionada.")]
    [SerializeField] private bool showGizmosAlways = true;

    [Tooltip("Color cuando no se detecta ninguna vaca.")]
    [SerializeField] private Color noDetectionColor = Color.red;

    [Tooltip("Color cuando se está detectando una vaca.")]
    [SerializeField] private Color detectingColor = Color.yellow;

    [Tooltip("Color cuando comienza la absorción.")]
    [SerializeField] private Color absorbingColor = Color.green;

    [Header("Eventos")]

    [Tooltip("Se ejecuta cuando comienza la absorción.")]
    public UnityEvent onAbsorptionStarted;

    [Tooltip("Se ejecuta cuando termina la recolección.")]
    public UnityEvent onCowCollected;

    private CollectibleCow currentCow;
    private float detectionTimer;

    private bool isAbsorbing;
    private bool cowDetectedThisFrame;

    private RaycastHit currentHit;

    public CollectibleCow CurrentCow => currentCow;
    public bool IsAbsorbing => isAbsorbing;
    public float DetectionTimer => detectionTimer;

    public float DetectionProgress
    {
        get
        {
            if (timeBeforeAbsorption <= 0f)
                return currentCow != null ? 1f : 0f;

            return Mathf.Clamp01(
                detectionTimer / timeBeforeAbsorption
            );
        }
    }

    private void Reset()
    {
        rayOrigin = transform;
        absorptionPoint = transform;
    }

    private void Awake()
    {
        if (rayOrigin == null)
            rayOrigin = transform;

        if (absorptionPoint == null)
            absorptionPoint = transform;
    }

    private void Update()
    {
        if (isAbsorbing)
        {
            DrawRuntimeDebug();
            return;
        }

        CheckForCow();
        DrawRuntimeDebug();
    }

    private void CheckForCow()
    {
        cowDetectedThisFrame = PerformCast(out RaycastHit hit);

        if (!cowDetectedThisFrame)
        {
            ResetDetection();
            return;
        }

        currentHit = hit;

        CollectibleCow detectedCow =
            hit.collider.GetComponentInParent<CollectibleCow>();

        if (detectedCow == null ||
            detectedCow.IsBeingCollected ||
            detectedCow.HasBeenCollected)
        {
            ResetDetection();
            return;
        }

        // Si se comienza a apuntar a una vaca distinta,
        // se reinicia el contador.
        if (detectedCow != currentCow)
        {
            currentCow = detectedCow;
            detectionTimer = 0f;
        }

        detectionTimer += Time.deltaTime;

        if (detectionTimer >= timeBeforeAbsorption)
        {
            StartCoroutine(AbsorbCow(currentCow));
        }
    }

    private bool PerformCast(out RaycastHit hit)
    {
        Vector3 origin = GetRayOrigin();
        Vector3 direction = Vector3.down;

        if (detectionRadius <= 0f)
        {
            return Physics.Raycast(
                origin,
                direction,
                out hit,
                detectionDistance,
                cowLayer,
                triggerInteraction
            );
        }

        return Physics.SphereCast(
            origin,
            detectionRadius,
            direction,
            out hit,
            detectionDistance,
            cowLayer,
            triggerInteraction
        );
    }

    private IEnumerator AbsorbCow(CollectibleCow cow)
    {
        if (cow == null || isAbsorbing)
            yield break;

        isAbsorbing = true;
        detectionTimer = timeBeforeAbsorption;

        cow.BeginCollection();
        onAbsorptionStarted?.Invoke();

        Transform cowTransform = cow.transform;

        Vector3 initialScale = cowTransform.localScale;
        Vector3 finalScale =
            initialScale * finalScaleMultiplier;

        while (cow != null)
        {
            Vector3 targetPosition =
                GetAbsorptionPosition();

            cowTransform.position =
                Vector3.MoveTowards(
                    cowTransform.position,
                    targetPosition,
                    absorptionSpeed * Time.deltaTime
                );

            if (rotateCowDuringAbsorption &&
                absorptionRotationSpeed > 0f)
            {
                cowTransform.Rotate(
                    Vector3.forward,
                    absorptionRotationSpeed * Time.deltaTime,
                    Space.World
                );
            }

            float distance =
                Vector3.Distance(
                    cowTransform.position,
                    targetPosition
                );

            float normalizedDistance =
                detectionDistance > 0f
                    ? Mathf.Clamp01(
                        distance / detectionDistance
                    )
                    : 0f;

            cowTransform.localScale =
                Vector3.Lerp(
                    finalScale,
                    initialScale,
                    normalizedDistance
                );

            if (distance <=
                absorptionCompletionDistance)
            {
                break;
            }

            yield return null;
        }

        if (cow != null)
        {
            cowTransform.position =
                GetAbsorptionPosition();

            cowTransform.localScale =
                finalScale;

            cow.CompleteCollection();
            onCowCollected?.Invoke();

            if (destroyCowAfterAbsorption)
            {
                Destroy(cow.gameObject);
            }
            else
            {
                cow.gameObject.SetActive(false);
            }
        }

        currentCow = null;
        detectionTimer = 0f;
        isAbsorbing = false;
        cowDetectedThisFrame = false;
    }

    private void ResetDetection()
    {
        currentCow = null;
        detectionTimer = 0f;
        cowDetectedThisFrame = false;
    }

    private Vector3 GetRayOrigin()
    {
        return rayOrigin != null
            ? rayOrigin.position
            : transform.position;
    }

    private Vector3 GetAbsorptionPosition()
    {
        return absorptionPoint != null
            ? absorptionPoint.position
            : transform.position;
    }

    private void DrawRuntimeDebug()
    {
        if (!showDebugCast)
            return;

        Vector3 origin = GetRayOrigin();

        Vector3 end =
            origin +
            Vector3.down * detectionDistance;

        Color debugColor = noDetectionColor;

        if (isAbsorbing)
            debugColor = absorbingColor;
        else if (currentCow != null)
            debugColor = detectingColor;

        Debug.DrawLine(
            origin,
            end,
            debugColor
        );

        if (cowDetectedThisFrame &&
            !isAbsorbing)
        {
            Debug.DrawLine(
                origin,
                currentHit.point,
                detectingColor
            );

            DrawDebugCross(
                currentHit.point,
                detectionRadius > 0f
                    ? detectionRadius
                    : 0.2f,
                detectingColor
            );
        }
    }

    private void DrawDebugCross(
        Vector3 position,
        float size,
        Color color
    )
    {
        Debug.DrawLine(
            position - Vector3.right * size,
            position + Vector3.right * size,
            color
        );

        Debug.DrawLine(
            position - Vector3.forward * size,
            position + Vector3.forward * size,
            color
        );

        Debug.DrawLine(
            position - Vector3.up * size,
            position + Vector3.up * size,
            color
        );
    }

    private void OnDrawGizmos()
    {
        if (!showGizmosAlways)
            return;

        DrawCastGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (showGizmosAlways)
            return;

        DrawCastGizmos();
    }

    private void DrawCastGizmos()
    {
        Vector3 origin =
            rayOrigin != null
                ? rayOrigin.position
                : transform.position;

        Vector3 end =
            origin +
            Vector3.down * detectionDistance;

        Color gizmoColor =
            noDetectionColor;

        if (isAbsorbing)
            gizmoColor = absorbingColor;
        else if (currentCow != null)
            gizmoColor = detectingColor;

        Gizmos.color = gizmoColor;

        Gizmos.DrawLine(
            origin,
            end
        );

        if (detectionRadius > 0f)
        {
            Gizmos.DrawWireSphere(
                origin,
                detectionRadius
            );

            Gizmos.DrawWireSphere(
                end,
                detectionRadius
            );

            Gizmos.DrawLine(
                origin +
                Vector3.right * detectionRadius,
                end +
                Vector3.right * detectionRadius
            );

            Gizmos.DrawLine(
                origin -
                Vector3.right * detectionRadius,
                end -
                Vector3.right * detectionRadius
            );

            Gizmos.DrawLine(
                origin +
                Vector3.forward * detectionRadius,
                end +
                Vector3.forward * detectionRadius
            );

            Gizmos.DrawLine(
                origin -
                Vector3.forward * detectionRadius,
                end -
                Vector3.forward * detectionRadius
            );
        }

        if (Application.isPlaying &&
            cowDetectedThisFrame &&
            !isAbsorbing)
        {
            Gizmos.color = detectingColor;

            Gizmos.DrawWireSphere(
                currentHit.point,
                Mathf.Max(
                    0.1f,
                    detectionRadius
                )
            );
        }
    }
} */



/* using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CowRaycastCollector : MonoBehaviour
{
    [Header("Puntos de referencia")]

    [Tooltip("Punto desde donde se lanza la detección hacia abajo.")]
    [SerializeField] private Transform rayOrigin;

    [Tooltip("Punto hacia donde será absorbida la vaca.")]
    [SerializeField] private Transform absorptionPoint;

    [Header("Detección")]

    [Tooltip("Capas que pueden contener vacas recolectables.")]
    [SerializeField] private LayerMask cowLayer;

    [Tooltip("Distancia máxima de detección hacia abajo.")]
    [Min(0.1f)]
    [SerializeField] private float detectionDistance = 10f;

    [Tooltip("Radio del SphereCast. Usa 0 para convertirlo en un Raycast normal.")]
    [Min(0f)]
    [SerializeField] private float detectionRadius = 0.3f;

    [Tooltip("Tiempo que la nave debe mantenerse sobre la vaca.")]
    [Min(0f)]
    [SerializeField] private float timeBeforeAbsorption = 1.5f;

    [Tooltip("Permite detectar colliders configurados como Trigger.")]
    [SerializeField] private QueryTriggerInteraction triggerInteraction =
        QueryTriggerInteraction.Collide;

    [Header("Absorción")]

    [Tooltip("Velocidad con la que la vaca se mueve hacia la nave.")]
    [Min(0.1f)]
    [SerializeField] private float absorptionSpeed = 5f;

    [Tooltip("Velocidad de rotación de la vaca durante la absorción.")]
    [Min(0f)]
    [SerializeField] private float absorptionRotationSpeed = 360f;

    [Tooltip("Escala final de la vaca antes de desaparecer.")]
    [Range(0f, 1f)]
    [SerializeField] private float finalScaleMultiplier = 0.15f;

    [Tooltip("Distancia a la que se considera terminada la absorción.")]
    [Min(0.001f)]
    [SerializeField] private float absorptionCompletionDistance = 0.05f;

    [Tooltip("Destruye la vaca cuando termina la absorción.")]
    [SerializeField] private bool destroyCowAfterAbsorption = true;

    [Header("Debug")]

    [Tooltip("Muestra el SphereCast en la vista Scene durante la ejecución.")]
    [SerializeField] private bool showDebugCast = true;

    [Tooltip("Muestra el SphereCast aunque la nave no esté seleccionada.")]
    [SerializeField] private bool showGizmosAlways = true;

    [Tooltip("Color cuando no se detecta ninguna vaca.")]
    [SerializeField] private Color noDetectionColor = Color.red;

    [Tooltip("Color cuando se está detectando una vaca.")]
    [SerializeField] private Color detectingColor = Color.yellow;

    [Tooltip("Color cuando comienza la absorción.")]
    [SerializeField] private Color absorbingColor = Color.green;

    [Header("Eventos")]

    [Tooltip("Se ejecuta cuando comienza la absorción.")]
    public UnityEvent onAbsorptionStarted;

    [Tooltip("Se ejecuta cuando termina la recolección.")]
    public UnityEvent onCowCollected;

    private CollectibleCow currentCow;
    private float detectionTimer;

    private bool isAbsorbing;
    private bool cowDetectedThisFrame;

    private RaycastHit currentHit;

    public CollectibleCow CurrentCow => currentCow;
    public bool IsAbsorbing => isAbsorbing;

    public float DetectionProgress
    {
        get
        {
            if (timeBeforeAbsorption <= 0f)
                return currentCow != null ? 1f : 0f;

            return Mathf.Clamp01(detectionTimer / timeBeforeAbsorption);
        }
    }

    public float DetectionTimer => detectionTimer;

    private void Reset()
    {
        rayOrigin = transform;
        absorptionPoint = transform;
    }

    private void Awake()
    {
        if (rayOrigin == null)
            rayOrigin = transform;

        if (absorptionPoint == null)
            absorptionPoint = transform;
    }

    private void Update()
    {
        if (isAbsorbing)
        {
            DrawRuntimeDebug();
            return;
        }

        CheckForCow();
        DrawRuntimeDebug();
    }

    private void CheckForCow()
    {
        cowDetectedThisFrame = PerformCast(out RaycastHit hit);

        if (!cowDetectedThisFrame)
        {
            ResetDetection();
            return;
        }

        currentHit = hit;

        CollectibleCow detectedCow =
            hit.collider.GetComponentInParent<CollectibleCow>();

        if (detectedCow == null ||
            detectedCow.IsBeingCollected ||
            detectedCow.HasBeenCollected)
        {
            ResetDetection();
            return;
        }

        // Si comenzamos a apuntar a una vaca distinta,
        // el contador se reinicia.
        if (detectedCow != currentCow)
        {
            currentCow = detectedCow;
            detectionTimer = 0f;
        }

        detectionTimer += Time.deltaTime;

        if (detectionTimer >= timeBeforeAbsorption)
        {
            StartCoroutine(AbsorbCow(currentCow));
        }
    }

    private bool PerformCast(out RaycastHit hit)
    {
        Vector3 origin = GetRayOrigin();
        Vector3 direction = Vector3.down;

        if (detectionRadius <= 0f)
        {
            return Physics.Raycast(
                origin,
                direction,
                out hit,
                detectionDistance,
                cowLayer,
                triggerInteraction
            );
        }

        return Physics.SphereCast(
            origin,
            detectionRadius,
            direction,
            out hit,
            detectionDistance,
            cowLayer,
            triggerInteraction
        );
    }

    private IEnumerator AbsorbCow(CollectibleCow cow)
    {
        if (cow == null || isAbsorbing)
            yield break;

        isAbsorbing = true;
        detectionTimer = timeBeforeAbsorption;

        cow.BeginCollection();

        onAbsorptionStarted?.Invoke();

        Transform cowTransform = cow.transform;
        Vector3 initialScale = cowTransform.localScale;
        Vector3 finalScale = initialScale * finalScaleMultiplier;

        while (cow != null)
        {
            Vector3 targetPosition = GetAbsorptionPosition();

            cowTransform.position = Vector3.MoveTowards(
                cowTransform.position,
                targetPosition,
                absorptionSpeed * Time.deltaTime
            );

            cowTransform.Rotate(
                Vector3.up,
                absorptionRotationSpeed * Time.deltaTime,
                Space.World
            );

            float distance = Vector3.Distance(
                cowTransform.position,
                targetPosition
            );

            float normalizedDistance = detectionDistance > 0f
                ? Mathf.Clamp01(distance / detectionDistance)
                : 0f;

            cowTransform.localScale = Vector3.Lerp(
                finalScale,
                initialScale,
                normalizedDistance
            );

            if (distance <= absorptionCompletionDistance)
                break;

            yield return null;
        }

        if (cow != null)
        {
            cowTransform.position = GetAbsorptionPosition();
            cowTransform.localScale = finalScale;

            cow.CompleteCollection();

            onCowCollected?.Invoke();

            if (destroyCowAfterAbsorption)
                Destroy(cow.gameObject);
            else
                cow.gameObject.SetActive(false);
        }

        currentCow = null;
        detectionTimer = 0f;
        isAbsorbing = false;
        cowDetectedThisFrame = false;
    }

    private void ResetDetection()
    {
        currentCow = null;
        detectionTimer = 0f;
        cowDetectedThisFrame = false;
    }

    private Vector3 GetRayOrigin()
    {
        return rayOrigin != null
            ? rayOrigin.position
            : transform.position;
    }

    private Vector3 GetAbsorptionPosition()
    {
        return absorptionPoint != null
            ? absorptionPoint.position
            : transform.position;
    }

    private void DrawRuntimeDebug()
    {
        if (!showDebugCast)
            return;

        Vector3 origin = GetRayOrigin();
        Vector3 end = origin + Vector3.down * detectionDistance;

        Color debugColor = noDetectionColor;

        if (isAbsorbing)
            debugColor = absorbingColor;
        else if (currentCow != null)
            debugColor = detectingColor;

        Debug.DrawLine(origin, end, debugColor);

        if (cowDetectedThisFrame && !isAbsorbing)
        {
            Debug.DrawLine(
                origin,
                currentHit.point,
                detectingColor
            );

            DrawDebugCross(
                currentHit.point,
                detectionRadius > 0f ? detectionRadius : 0.2f,
                detectingColor
            );
        }
    }

    private void DrawDebugCross(
        Vector3 position,
        float size,
        Color color
    )
    {
        Debug.DrawLine(
            position - Vector3.right * size,
            position + Vector3.right * size,
            color
        );

        Debug.DrawLine(
            position - Vector3.forward * size,
            position + Vector3.forward * size,
            color
        );

        Debug.DrawLine(
            position - Vector3.up * size,
            position + Vector3.up * size,
            color
        );
    }

    private void OnDrawGizmos()
    {
        if (!showGizmosAlways)
            return;

        DrawCastGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (showGizmosAlways)
            return;

        DrawCastGizmos();
    }

    private void DrawCastGizmos()
    {
        Vector3 origin = rayOrigin != null
            ? rayOrigin.position
            : transform.position;

        Vector3 end = origin + Vector3.down * detectionDistance;

        Color gizmoColor = noDetectionColor;

        if (isAbsorbing)
            gizmoColor = absorbingColor;
        else if (currentCow != null)
            gizmoColor = detectingColor;

        Gizmos.color = gizmoColor;

        // Línea central.
        Gizmos.DrawLine(origin, end);

        if (detectionRadius > 0f)
        {
            // Esferas que representan el inicio y final del SphereCast.
            Gizmos.DrawWireSphere(origin, detectionRadius);
            Gizmos.DrawWireSphere(end, detectionRadius);

            // Líneas laterales para visualizar el volumen completo.
            Gizmos.DrawLine(
                origin + Vector3.right * detectionRadius,
                end + Vector3.right * detectionRadius
            );

            Gizmos.DrawLine(
                origin - Vector3.right * detectionRadius,
                end - Vector3.right * detectionRadius
            );

            Gizmos.DrawLine(
                origin + Vector3.forward * detectionRadius,
                end + Vector3.forward * detectionRadius
            );

            Gizmos.DrawLine(
                origin - Vector3.forward * detectionRadius,
                end - Vector3.forward * detectionRadius
            );
        }

        if (Application.isPlaying && cowDetectedThisFrame && !isAbsorbing)
        {
            Gizmos.color = detectingColor;
            Gizmos.DrawWireSphere(
                currentHit.point,
                Mathf.Max(0.1f, detectionRadius)
            );
        }
    }
} */