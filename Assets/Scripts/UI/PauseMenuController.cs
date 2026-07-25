using UnityEngine;
using DG.Tweening;

public class PauseMenuController : MonoBehaviour
{
    public RectTransform pauseMenu; // Asigna tu panel aqu desde el inspector

    private Vector2 originalPosition;
    private Vector2 centerPosition = Vector2.zero; 

    private bool isPaused = false;

    private void Start()
    {
        // Guardamos la posici�n original
        originalPosition = pauseMenu.anchoredPosition;

        // Nos aseguramos que el men� est� en su posici�n original
        pauseMenu.anchoredPosition = originalPosition;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        pauseMenu.DOAnchorPos(centerPosition, 0.5f).SetUpdate(true); 
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        pauseMenu.DOAnchorPos(originalPosition, 0.5f).SetUpdate(true);
    }
}