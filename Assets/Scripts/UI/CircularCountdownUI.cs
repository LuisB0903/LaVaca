using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class CircularCountdownUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform circularTransform;
    [SerializeField] private CowScoreManager scoreManager;

    [Header("Timer")]
    [Min(0f)]
    [SerializeField] private float totalDuration = 10f;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Rotation")]
    [SerializeField] private float startAngleZ = 0f;
    [SerializeField] private float endAngleZ = -180f;

    [Header("Events")]
    [SerializeField] private UnityEvent onTimerFinished;

    public float Duration => totalDuration;
    public float RemainingTime => remainingTime;
    public bool IsRunning => isRunning;
    public float NormalizedProgress => totalDuration <= 0f ? 1f : Mathf.Clamp01(elapsedTime / totalDuration);
    public float CurrentAngleZ => currentAngleZ;

    private float remainingTime;
    private float elapsedTime;
    private float currentAngleZ;
    private bool isRunning;
    private bool hasFinished;

    private void Awake()
    {
        if (circularTransform == null)
        {
            circularTransform = GetComponent<Transform>();
        }

        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<CowScoreManager>();
        }
    }

    private void Start()
    {
        ResetTimer();
        scoreManager?.BeginRun();

        if (autoStart)
        {
            StartTimer();
        }
        else
        {
            RefreshVisuals();
        }
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        elapsedTime += deltaTime;
        remainingTime = Mathf.Max(0f, totalDuration - elapsedTime);

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            FinishTimer();
            return;
        }

        RefreshVisuals();
    }

    public void StartTimer()
    {
        StartTimer(totalDuration);
    }

    public void StartTimer(float newDuration)
    {
        scoreManager?.BeginRun();
        totalDuration = Mathf.Max(0f, newDuration);
        remainingTime = totalDuration;
        elapsedTime = 0f;
        isRunning = true;
        hasFinished = false;

        RefreshVisuals();

        if (totalDuration <= 0f)
        {
            FinishTimer();
        }
    }

    public void ResetTimer()
    {
        remainingTime = totalDuration;
        elapsedTime = 0f;
        isRunning = false;
        hasFinished = false;
        RefreshVisuals();
    }

    public void StopTimer()
    {
        isRunning = false;
        RefreshVisuals();
    }

    private void FinishTimer()
    {
        if (hasFinished)
        {
            return;
        }

        isRunning = false;
        hasFinished = true;
        RefreshVisuals();
        onTimerFinished?.Invoke();
    }

    private void RefreshVisuals()
    {
        float progress = NormalizedProgress;
        currentAngleZ = Mathf.Lerp(startAngleZ, endAngleZ, progress);

        if (circularTransform != null)
        {
            circularTransform.localEulerAngles = new Vector3(0f, 0f, currentAngleZ);
        }
    }

    private void OnValidate()
    {
        totalDuration = Mathf.Max(0f, totalDuration);
        remainingTime = Mathf.Clamp(remainingTime, 0f, totalDuration);
        currentAngleZ = Mathf.Lerp(startAngleZ, endAngleZ, NormalizedProgress);
    }
}