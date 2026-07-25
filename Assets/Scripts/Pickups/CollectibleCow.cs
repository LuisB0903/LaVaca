using UnityEngine;
using UnityEngine.Events;

public class CollectibleCow : MonoBehaviour
{
    [Header("Configuración")]

    [Tooltip("Desactiva todos los colliders cuando comienza la absorción.")]
    [SerializeField] private bool disableCollidersOnCollection = true;

    [Tooltip("Desactiva los Behaviours indicados cuando comienza la absorción.")]
    [SerializeField] private Behaviour[] behavioursToDisable;

    [Header("Eventos")]

    public UnityEvent onCollectionStarted;
    public UnityEvent onCollectionCompleted;

    public bool IsBeingCollected { get; private set; }
    public bool HasBeenCollected { get; private set; }

    private Collider[] cowColliders;
    private Rigidbody cowRigidbody;

    private void Awake()
    {
        cowColliders = GetComponentsInChildren<Collider>(true);
        cowRigidbody = GetComponent<Rigidbody>();

        if (cowRigidbody == null)
            cowRigidbody = GetComponentInChildren<Rigidbody>();
    }

    public void BeginCollection()
    {
        if (IsBeingCollected || HasBeenCollected)
            return;

        IsBeingCollected = true;

        DisableMovement();
        DisableConfiguredBehaviours();

        if (disableCollidersOnCollection)
            DisableColliders();

        onCollectionStarted?.Invoke();
    }

    public void CompleteCollection()
    {
        if (HasBeenCollected)
            return;

        IsBeingCollected = false;
        HasBeenCollected = true;

        onCollectionCompleted?.Invoke();
    }

    private void DisableMovement()
    {
        if (cowRigidbody == null)
            return;

        cowRigidbody.linearVelocity = Vector3.zero;
        cowRigidbody.angularVelocity = Vector3.zero;
        cowRigidbody.useGravity = false;
        cowRigidbody.isKinematic = true;
    }

    private void DisableColliders()
    {
        foreach (Collider cowCollider in cowColliders)
        {
            if (cowCollider != null)
                cowCollider.enabled = false;
        }
    }

    private void DisableConfiguredBehaviours()
    {
        if (behavioursToDisable == null)
            return;

        foreach (Behaviour behaviour in behavioursToDisable)
        {
            if (behaviour != null)
                behaviour.enabled = false;
        }
    }
}