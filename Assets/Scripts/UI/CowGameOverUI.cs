using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CowGameOverUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CowScoreManager scoreManager;
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text leaderboardText;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button restartButton;

    [Header("Defaults")]
    [SerializeField] private string defaultPlayerName = "Jugador";

    private bool gameOverVisible;
    private bool scoreSubmitted;

    private void Awake()
    {
        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<CowScoreManager>();
        }

        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
    }

    public void HandleGameFinished()
    {
        if (gameOverVisible)
        {
            return;
        }

        gameOverVisible = true;
        Time.timeScale = 0f;

        if (rootPanel != null)
        {
            rootPanel.SetActive(true);
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = $"Vacas recolectadas: {(scoreManager != null ? scoreManager.CurrentRunCowCount : 0)}";
        }

        RefreshLeaderboard();

        if (playerNameInput != null)
        {
            playerNameInput.text = string.Empty;
            playerNameInput.interactable = true;
            playerNameInput.Select();
            playerNameInput.ActivateInputField();
        }

        if (submitButton != null)
        {
            submitButton.interactable = true;
        }

        scoreSubmitted = false;
    }

    public void SubmitScore()
    {
        if (!gameOverVisible || scoreSubmitted)
        {
            return;
        }

        string playerName = playerNameInput != null ? playerNameInput.text : defaultPlayerName;

        if (scoreManager != null)
        {
            scoreManager.SubmitCurrentRun(playerName);
        }

        scoreSubmitted = true;

        if (submitButton != null)
        {
            submitButton.interactable = false;
        }

        if (playerNameInput != null)
        {
            playerNameInput.interactable = false;
        }

        RefreshLeaderboard();
    }

    public void RestartGame()
    {
        if (gameOverVisible && !scoreSubmitted)
        {
            SubmitScore();
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void RefreshLeaderboard()
    {
        if (leaderboardText == null)
        {
            return;
        }

        if (scoreManager == null || scoreManager.LeaderboardEntries.Count == 0)
        {
            leaderboardText.text = "Aun no hay puntuaciones guardadas.";
            return;
        }

        StringBuilder builder = new StringBuilder();
        int position = 1;

        foreach (CowScoreManager.LeaderboardEntry entry in scoreManager.LeaderboardEntries)
        {
            builder.AppendLine($"{position}. {entry.playerName} - {entry.score}");
            position++;
        }

        leaderboardText.text = builder.ToString().TrimEnd();
    }
}