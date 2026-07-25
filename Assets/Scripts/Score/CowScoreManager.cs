using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[DisallowMultipleComponent]
public class CowScoreManager : MonoBehaviour
{
    private const string DefaultSaveFileName = "cow_leaderboard.json";

    [Serializable]
    public class LeaderboardEntry
    {
        public string playerName;
        public int score;
        public string recordedAtUtc;
    }

    [Serializable]
    private class LeaderboardSaveData
    {
        public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    }

    [Header("Storage")]
    [SerializeField] private string saveFileName = DefaultSaveFileName;
    [SerializeField, Min(1)] private int maxLeaderboardEntries = 10;
    [SerializeField] private bool logSavePathOnStart = true;

    public int CurrentRunCowCount => currentRunCowCount;
    public IReadOnlyList<LeaderboardEntry> LeaderboardEntries => leaderboardEntries;
    public string SavePath => Path.Combine(Application.persistentDataPath, GetValidatedFileName());

    private readonly List<LeaderboardEntry> leaderboardEntries = new List<LeaderboardEntry>();
    private int currentRunCowCount;
    private bool hasLoadedLeaderboard;

    private static CowScoreManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadLeaderboard();

        if (logSavePathOnStart)
        {
            Debug.Log($"[CowScoreManager] Leaderboard path: {SavePath}", this);
        }
    }

    public void BeginRun()
    {
        currentRunCowCount = 0;
    }

    public void RegisterCowCollected(int amount = 1)
    {
        if (amount <= 0)
        {
            return;
        }

        currentRunCowCount += amount;
    }

    public LeaderboardEntry SubmitCurrentRun(string playerName)
    {
        LoadLeaderboard();

        string sanitizedName = string.IsNullOrWhiteSpace(playerName)
            ? "Jugador"
            : playerName.Trim();

        LeaderboardEntry entry = new LeaderboardEntry
        {
            playerName = sanitizedName,
            score = currentRunCowCount,
            recordedAtUtc = DateTime.UtcNow.ToString("o")
        };

        leaderboardEntries.Add(entry);
        SortAndTrimLeaderboard();
        SaveLeaderboard();

        return entry;
    }

    public void LoadLeaderboard()
    {
        if (hasLoadedLeaderboard)
        {
            return;
        }

        hasLoadedLeaderboard = true;

        if (!File.Exists(SavePath))
        {
            leaderboardEntries.Clear();
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            LeaderboardSaveData loadedData = JsonUtility.FromJson<LeaderboardSaveData>(json);

            leaderboardEntries.Clear();

            if (loadedData != null && loadedData.entries != null)
            {
                leaderboardEntries.AddRange(loadedData.entries);
            }

            SortAndTrimLeaderboard();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[CowScoreManager] No se pudo cargar el leaderboard: {exception.Message}", this);
            leaderboardEntries.Clear();
        }
    }

    public void SaveLeaderboard()
    {
        try
        {
            string directoryPath = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            LeaderboardSaveData data = new LeaderboardSaveData
            {
                entries = new List<LeaderboardEntry>(leaderboardEntries)
            };

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[CowScoreManager] No se pudo guardar el leaderboard: {exception.Message}", this);
        }
    }

    private string GetValidatedFileName()
    {
        string trimmed = string.IsNullOrWhiteSpace(saveFileName)
            ? DefaultSaveFileName
            : saveFileName.Trim();

        if (Path.GetExtension(trimmed).Length == 0)
        {
            trimmed += ".json";
        }

        return trimmed;
    }

    private void SortAndTrimLeaderboard()
    {
        leaderboardEntries.Sort(CompareEntries);

        if (leaderboardEntries.Count <= maxLeaderboardEntries)
        {
            return;
        }

        leaderboardEntries.RemoveRange(maxLeaderboardEntries, leaderboardEntries.Count - maxLeaderboardEntries);
    }

    private static int CompareEntries(LeaderboardEntry left, LeaderboardEntry right)
    {
        if (left == null && right == null)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        int scoreComparison = right.score.CompareTo(left.score);
        if (scoreComparison != 0)
        {
            return scoreComparison;
        }

        return string.CompareOrdinal(left.recordedAtUtc, right.recordedAtUtc);
    }
}