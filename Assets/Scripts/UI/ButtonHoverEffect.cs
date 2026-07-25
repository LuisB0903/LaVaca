using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float scaleAmount = 1.1f;
    public float scaleDuration = 0.2f;
    public float followRadius = 10f;
    public float followSpeed = 5f;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Vector3 originalScale;

    private bool isHovering = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
        originalScale = rectTransform.localScale;
    }

    void Update()
    {
        if (isHovering)
        {
            Vector2 localMousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                Input.mousePosition,
                null,
                out localMousePosition
            );

            // Calcula el desplazamiento limitado
            Vector2 offset = localMousePosition - originalPosition;
            offset = Vector2.ClampMagnitude(offset, followRadius);

            // Interpola suavemente hacia la nueva posici�n
            Vector2 targetPosition = originalPosition + offset;
            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPosition, Time.deltaTime * followSpeed);
        
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        rectTransform.DOScale(originalScale * scaleAmount, scaleDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true); // <-- IMPORTANTE
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;

        rectTransform.DOScale(originalScale, scaleDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true); // <-- IMPORTANTE

        rectTransform.DOAnchorPos(originalPosition, 0.3f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true); // <-- IMPORTANTE
    }
}