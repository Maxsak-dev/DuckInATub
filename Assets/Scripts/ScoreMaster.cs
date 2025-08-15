using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using TMPro;
using UnityEngine;
using System.IO;

public class ScoreMaster : MonoBehaviour
{
    [SerializeField] private Transform gameField;
    [SerializeField] private CinemachineVirtualCamera virtCam;
    [SerializeField] private BubbleSpawner bubbleSpawner;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private Transform player, player_spawn;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator fieldAnim;
    [SerializeField] private TMP_Text game_score_label, menu_score_label;
    [SerializeField] private GameObject menu_ui, ingame_ui;
    [SerializeField] private float discharge_rate_max, discharge_rate_start;
    [SerializeField] private UIController uIController;

    private string score_fstring = "{0}";
    private float score = 0.0f;
    public int discharge = 50;
    private int lives = 3;
    public bool game_running = false;
    private UIController uiController;
    private float discharge_rate;
    private Coroutine discharge_rate_coroutine;
    private bool in_credits = false;
    private string filePath;
    private float highscore = 0.0f;

    void Update()
    {
        if (Input.GetButton("Exit"))
        {
            // If we are running in the editor, stop play mode
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                // If we are running in a built application, quit the game
                Application.Quit();
            #endif
        }

        if (!game_running && !in_credits && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.JoystickButton7)))
        {
            StartGame();
        }

        if (!game_running && (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.JoystickButton1)))
        {
            if (!in_credits) {
                uIController.SwitchToCredits();
                in_credits = true;
            } else {
                uIController.SwitchToMenu();
                in_credits = false;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Cross-platform path
        filePath = Path.Combine(Application.persistentDataPath, "scores.txt");

        // Load the score
        if (File.Exists(filePath))
        {
            string scoreText = File.ReadAllText(filePath);
            highscore = float.Parse(scoreText);
            menu_score_label.text = $"High score: {highscore}";
        }

        UpdateLabel();
        uiController = GameObject.Find("InGameUI").GetComponent<UIController>();
        StartCoroutine(Discharger());
    }

    private void UpdateLabel()
    {
        game_score_label.text = string.Format(score_fstring, score);
    }

    public void AddScore(int new_score)
    {
        if(!game_running)
            return;

        score += new_score;
        if(discharge < 100)
            discharge += new_score;
        UpdateLabel();
    }

    public void ReduceLive()
    {
        if(lives <= 1 && game_running)
        {
            EndGame();
        }

        --lives;
        uiController.UpdateLives(lives);
    }

    public void EndGame()
    {
        game_running = false;
        // bubbleSpawner.produce = false;
        enemySpawner.produce = false;
        virtCam.Follow = player_spawn;
        ingame_ui.SetActive(false);
        fieldAnim.SetTrigger("descend");
        StopCoroutine(discharge_rate_coroutine);
    }

    public void EndComplete()
    {
        menu_ui.SetActive(true);

        if (score > highscore)
        {
            menu_score_label.text = $"High score: {score}";
            highscore = score;
            File.WriteAllText(filePath, score.ToString());
        }
        score = 0;
    }

    public void StartGame()
    {
        playerMovement.Reset();
        discharge_rate = discharge_rate_start;
        discharge = 50;
        lives = 3;
        uiController.UpdateLives(lives);
        menu_ui.SetActive(false);
        fieldAnim.SetTrigger("ascend");
    }

    public void StartComplete()
    {
        bubbleSpawner.produce = true;
        enemySpawner.ResetSpawner();
        virtCam.Follow = player;
        ingame_ui.SetActive(true);
        game_running = true;
        discharge_rate_coroutine = StartCoroutine(DischargeRateIncreaser());
    }

    IEnumerator Discharger()
    {
        while(true)
        {
            yield return new WaitForSeconds(discharge_rate);

            if(game_running)
                discharge -= 1;

            if (discharge <= 0 && game_running)
            {
                EndGame();
            }
        }
    }

    IEnumerator DischargeRateIncreaser()
    {
        while (discharge_rate > discharge_rate_max)
        {
            yield return new WaitForSeconds(10);

            discharge_rate -= 0.1f;
        }
    }
}
