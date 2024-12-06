using UnityEngine;
using TMPro;

public class GameUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Format Settings")]
    [SerializeField] private string timeFormat = "Time: {0:00}";
    [SerializeField] private string scoreFormat = "Score: {0}";

    private GameManager gameManager;

    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();

        if (gameManager == null)
        {
            Debug.LogError("GameManager not found in the scene!");
            enabled = false;
            return;
        }

        if (timeText == null || scoreText == null)
        {
            Debug.LogError("Some UI references are missing!");
            enabled = false;
            return;
        }

        UpdateTimeDisplay(gameManager.remainingTime);
        UpdateScoreDisplay(gameManager.currentScore);
    }

    private void Update()
    {
        if (!gameManager.isGameActive) return;

        UpdateTimeDisplay(gameManager.remainingTime);
        UpdateScoreDisplay(gameManager.currentScore);
    }

    private void UpdateTimeDisplay(float timeValue)
    {
        if (timeText != null)
        {
            timeText.text = string.Format(timeFormat, Mathf.Max(0, timeValue));
        }
    }

    private void UpdateScoreDisplay(int scoreValue)
    {
        if (scoreText != null)
        {
            scoreText.text = string.Format(scoreFormat, scoreValue);
        }
    }
}
