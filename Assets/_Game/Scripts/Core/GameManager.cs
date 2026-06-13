using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Playing,
        Paused,
        GameOver,
        Win
    }

    public GameState CurrentState { get; private set; } = GameState.Playing;

    public event Action<GameState> OnGameStateChanged;
    public event Action<int> OnScoreChanged;
    public event Action<int> OnWaveChanged;

    [Header("Player")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Enemy Spawn")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int enemiesPerWave = 4;
    [SerializeField] private float timeBetweenSpawns = 0.8f;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private int totalWaves = 3;

    [Header("UI Panels")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject pausePanel;

    [Header("UI Texts")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text gameOverScoreText;
    [SerializeField] private TMP_Text winScoreText;
    [SerializeField] private TMP_Text waveCountdownText;

    public int Score { get; private set; }
    public int WaveNumber { get; private set; } = 1;

    private int enemiesAliveThisWave;
    private bool isSpawning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (playerHealth != null)
        {
            playerHealth.OnDeath += HandlePlayerDeath;
        }

        SetPanels(GameState.Playing);
        UpdateScoreUI();
        UpdateWaveUI();

        StartCoroutine(StartWave(WaveNumber));
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDeath -= HandlePlayerDeath;
        }
    }

    private IEnumerator StartWave(int wave)
    {
        isSpawning = true;
        enemiesAliveThisWave = 0;

        if (waveCountdownText != null)
        {
            waveCountdownText.gameObject.SetActive(true);
            waveCountdownText.text = "WAVE " + wave;
            yield return new WaitForSeconds(2f);
            waveCountdownText.gameObject.SetActive(false);
        }

        int enemyCount = enemiesPerWave + (wave - 1) * 2;

        for (int i = 0; i < enemyCount; i++)
        {
            if (CurrentState != GameState.Playing)
            {
                yield break;
            }

            bool spawned = SpawnEnemy();

            if (spawned)
            {
                enemiesAliveThisWave++;
            }

            yield return new WaitForSeconds(timeBetweenSpawns);
        }

        isSpawning = false;

        if (enemiesAliveThisWave <= 0)
        {
            StartCoroutine(HandleWaveCleared());
        }
    }

    private bool SpawnEnemy()
    {
        if (enemyPrefab == null) return false;
        if (spawnPoints == null || spawnPoints.Length == 0) return false;

        Transform bestSpawn = spawnPoints[0];
        float maxDistance = 0f;

        Vector3 playerPosition = playerHealth != null
            ? playerHealth.transform.position
            : Vector3.zero;

        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null) continue;

            float distance = Vector3.Distance(spawnPoint.position, playerPosition);

            if (distance > maxDistance)
            {
                maxDistance = distance;
                bestSpawn = spawnPoint;
            }
        }

        GameObject enemy = Instantiate(enemyPrefab, bestSpawn.position, bestSpawn.rotation);

        EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();

        if (enemyHealth != null)
        {
            enemyHealth.OnScoreGranted += AddScore;
            enemyHealth.OnScoreGranted += HandleEnemyKilled;
        }

        return true;
    }

    private void HandleEnemyKilled(int score)
    {
        enemiesAliveThisWave = Mathf.Max(0, enemiesAliveThisWave - 1);

        if (enemiesAliveThisWave <= 0 && !isSpawning)
        {
            StartCoroutine(HandleWaveCleared());
        }
    }

    private IEnumerator HandleWaveCleared()
    {
        if (CurrentState != GameState.Playing)
        {
            yield break;
        }

        if (WaveNumber >= totalWaves)
        {
            yield return new WaitForSeconds(1.5f);
            TriggerWin();
            yield break;
        }

        if (waveCountdownText != null)
        {
            waveCountdownText.gameObject.SetActive(true);

            for (int i = Mathf.CeilToInt(timeBetweenWaves); i > 0; i--)
            {
                waveCountdownText.text = "NEXT WAVE IN " + i;
                yield return new WaitForSeconds(1f);
            }

            waveCountdownText.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(timeBetweenWaves);
        }

        WaveNumber++;
        OnWaveChanged?.Invoke(WaveNumber);
        UpdateWaveUI();

        StartCoroutine(StartWave(WaveNumber));
    }

    private void AddScore(int amount)
    {
        Score += amount;

        OnScoreChanged?.Invoke(Score);
        UpdateScoreUI();
    }

    private void HandlePlayerDeath()
    {
        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        yield return new WaitForSeconds(1.2f);
        SetGameState(GameState.GameOver);
    }

    private void TriggerWin()
    {
        SetGameState(GameState.Win);
    }

    private void SetGameState(GameState state)
    {
        CurrentState = state;

        SetPanels(state);

        OnGameStateChanged?.Invoke(state);

        if (state == GameState.GameOver && gameOverScoreText != null)
        {
            gameOverScoreText.text = "SCORE: " + Score;
        }

        if (state == GameState.Win && winScoreText != null)
        {
            winScoreText.text = "FINAL SCORE: " + Score;
        }
    }

    private void SetPanels(GameState state)
    {
        if (hudPanel != null)
        {
            hudPanel.SetActive(state == GameState.Playing || state == GameState.Paused);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(state == GameState.GameOver);
        }

        if (winPanel != null)
        {
            winPanel.SetActive(state == GameState.Win);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(state == GameState.Paused);
        }

        Time.timeScale = state == GameState.Paused ? 0f : 1f;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE: " + Score;
        }
    }

    private void UpdateWaveUI()
    {
        if (waveText != null)
        {
            waveText.text = "WAVE " + WaveNumber + " / " + totalWaves;
        }
    }

    public void UI_Pause()
    {
        if (CurrentState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
        }
        else if (CurrentState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
        }
    }

    public void UI_Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void UI_MainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    public void UI_NextLevel()
    {
        Time.timeScale = 1f;

        int nextScene = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextScene < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextScene);
        }
        else
        {
            UI_MainMenu();
        }
    }
}
