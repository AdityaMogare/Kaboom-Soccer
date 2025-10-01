using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;   

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public enum Team { Blue, Red }

  
    [Header("UI Elements")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI blueScoreText;
    public TextMeshProUGUI redScoreText;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;       
    public TextMeshProUGUI winnerText;     

    [Header("Game Timer")]
    public float matchTime = 120f;   // seconds (2:00)
    private float currentTime;
    private bool matchRunning = false;

    
    [Header("Scores (read-only)")]
    public int blueScore { get; private set; }
    public int redScore  { get; private set; }

    [Header("Puck Reset")]
    public Rigidbody2D puckRb;        // assign: Puck's Rigidbody2D
    public Transform   puckSpawn;     // assign: empty at center
    public float postGoalDelay = 0.8f;

    [Header("Goal Triggers (drag the BoxCollider2D from each goal)")]
    public Collider2D leftGoalTrigger;
    public Collider2D rightGoalTrigger;

    [Header("Team Spawns (size 3 each: B1/B2/B3 and R1/R2/R3)")]
    public Transform[] blueSpawns;    // positions for Blue 1/2/3
    public Transform[] redSpawns;     // positions for Red 1/2/3

    [Header("(Optional) Direct Disk Refs (size 3 each). Leave empty to auto-find by tag.")]
    public Rigidbody2D[] blueDisks;   // Blue 1/2/3 RB2D (optional)
    public Rigidbody2D[] redDisks;    // Red 1/2/3 RB2D (optional)

    
    private bool goalLock = false;    
    public bool IsLocked => goalLock;
    public bool IsMatchRunning => matchRunning;

    public System.Action OnMatchEnded;

    [Header("Setup (Mines)")]
    public MinePlacementController placement;   
    public TextMeshProUGUI turnLabel;           
    private readonly List<GameObject> allMines = new();

  
    private bool gameStarted = false;
    public bool IsGameStarted => gameStarted;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
       
        UpdateScoreUI();
        currentTime = Mathf.Max(0f, matchTime);
        UpdateTimerUI();

        if (gameOverPanel) gameOverPanel.SetActive(false);

      
        matchRunning = false;

        
        PreparePreGameState();
    }

    void Update()
    {
        if (matchRunning)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0f)
            {
                currentTime = 0f;
                matchRunning = false;
                UpdateTimerUI();
                EndMatch();
            }
            else
            {
                UpdateTimerUI();
            }
        }
    }

    public void BeginGame()
    {
        if (gameStarted) return;   
        gameStarted = true;

        StartCoroutine(SetupMinesFlow());
    }

  
    public void OnGoalScored(Team scoringTeam)
    {
        if (!matchRunning) return;
        if (goalLock) return;
        goalLock = true;
        StartCoroutine(HandleGoalRoutine(scoringTeam));
    }

    private IEnumerator HandleGoalRoutine(Team scoringTeam)
    {
        if (scoringTeam == Team.Blue) blueScore++;
        else                          redScore++;
        UpdateScoreUI();
        Debug.Log($"GOAL!  Blue {blueScore} : Red {redScore}   ({scoringTeam} scored)");

        if (leftGoalTrigger)  leftGoalTrigger.enabled  = false;
        if (rightGoalTrigger) rightGoalTrigger.enabled = false;

        if (puckRb)
        {
            puckRb.linearVelocity = Vector2.zero;
            puckRb.angularVelocity = 0f;
            puckRb.simulated = false;
            if (puckSpawn) puckRb.transform.position = puckSpawn.position;
        }

        ResetTeamToSpawns(Team.Blue);
        ResetTeamToSpawns(Team.Red);

        yield return new WaitForSeconds(postGoalDelay);

        if (puckRb) puckRb.simulated = true;
        if (leftGoalTrigger)  leftGoalTrigger.enabled  = true;
        if (rightGoalTrigger) rightGoalTrigger.enabled = true;

        goalLock = false;
    }

    // ---------- End of Match ----------
    private void EndMatch()
    {
        Debug.Log("MATCH OVER!");

        // stop scoring
        if (leftGoalTrigger)  leftGoalTrigger.enabled  = false;
        if (rightGoalTrigger) rightGoalTrigger.enabled = false;

        // freeze puck
        if (puckRb)
        {
            puckRb.linearVelocity = Vector2.zero;
            puckRb.angularVelocity = 0f;
            puckRb.simulated = false;
        }

        // bring EVERY disk back (even eliminated ones)
        ReactivateAllTeam(Team.Blue);
        ReactivateAllTeam(Team.Red);
        ResetTeamToSpawns(Team.Blue);
        ResetTeamToSpawns(Team.Red);

        // snap puck to center
        if (puckRb && puckSpawn) puckRb.transform.position = puckSpawn.position;

        // show panel with winner text
        string result = (blueScore > redScore) ? "Blue Wins!"
                       : (redScore > blueScore) ? "Red Wins!"
                       : "Draw!";
        if (winnerText) winnerText.text = result;
        if (gameOverPanel) gameOverPanel.SetActive(true);

        OnMatchEnded?.Invoke();
    }

    public void OnClickReplay()
    {
        // reload current scene
        Scene active = SceneManager.GetActiveScene();
        SceneManager.LoadScene(active.buildIndex);
    }

    // ---------- UI helpers ----------
    private void UpdateTimerUI()
    {
        if (!timerText) return;
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void UpdateScoreUI()
    {
        if (blueScoreText) blueScoreText.text = blueScore.ToString();
        if (redScoreText)  redScoreText.text  = redScore.ToString();
    }

    // ---------- Respawn helpers ----------
    private void ReactivateAllTeam(Team team)
    {
        var disks = GetTeamDisks(team);
        if (disks == null) return;
        foreach (var rb in disks)
        {
            if (rb == null) continue;
            rb.gameObject.SetActive(true); // revive eliminated disks
        }
    }

    private void ResetTeamToSpawns(Team team)
    {
        Transform[] spawns = (team == Team.Blue) ? blueSpawns : redSpawns;
        Rigidbody2D[] disks = GetTeamDisks(team);
        if (spawns == null || spawns.Length == 0 || disks == null) return;

        int count = Mathf.Min(disks.Length, spawns.Length);
        for (int i = 0; i < count; i++)
        {
            var rb = disks[i];
            var spawn = spawns[i];
            if (rb == null || spawn == null) continue;
            if (!rb.gameObject.activeInHierarchy) rb.gameObject.SetActive(true);

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
            rb.transform.position = spawn.position;
        }
        for (int i = 0; i < count; i++)
        {
            var rb = disks[i];
            if (rb == null) continue;
            rb.simulated = true;
        }
    }

    private Rigidbody2D[] GetTeamDisks(Team team)
    {
        if (team == Team.Blue && blueDisks != null && blueDisks.Length > 0) return blueDisks;
        if (team == Team.Red  && redDisks  != null && redDisks.Length  > 0) return redDisks;

        string tag = (team == Team.Blue) ? "BlueDisk" : "RedDisk";
        GameObject[] gos = GameObject.FindGameObjectsWithTag(tag);
        List<Rigidbody2D> list = new List<Rigidbody2D>(gos.Length);
        foreach (var go in gos)
        {
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) list.Add(rb);
        }
        // Deterministic order by name
        list.Sort((a, b) => a.name.CompareTo(b.name));
        return list.ToArray();
    }

    // ---------- Pre-game freeze so nothing reacts before Start ----------
    private void PreparePreGameState()
    {
        // Disable goal triggers so no scoring happens
        if (leftGoalTrigger)  leftGoalTrigger.enabled  = false;
        if (rightGoalTrigger) rightGoalTrigger.enabled = false;

        // Freeze + center puck
        if (puckRb)
        {
            puckRb.linearVelocity = Vector2.zero;
            puckRb.angularVelocity = 0f;
            puckRb.simulated = false;
            if (puckSpawn) puckRb.transform.position = puckSpawn.position;
        }

        // Snap/enable all players at their spawns
        ReactivateAllTeam(Team.Blue);
        ReactivateAllTeam(Team.Red);
        ResetTeamToSpawns(Team.Blue);
        ResetTeamToSpawns(Team.Red);

        // Clear any stale mines from previous rounds (if scene wasn’t reloaded)
        allMines.Clear();

        if (turnLabel) turnLabel.text = ""; // Landing page shows instructions instead
    }

    // ===================== Placement + match flow =====================
    private IEnumerator SetupMinesFlow()
    {
        // Ensure clean state going into placement
        PreparePreGameState();

        // 1) BLUE places on LEFT half (adjust text to match your placement rules)
        if (turnLabel) turnLabel.text = "Blue: place your 2 mines (Left half)";
        placement.BeginPlacement(PlacementTeam.Blue);
        yield return new WaitUntil(() => placement.IsFinished());
        allMines.AddRange(placement.GetPlacedMinesThisTeam());

        // 2) RED places on RIGHT half
        if (turnLabel) turnLabel.text = "Red: place your 2 mines (Right half)";
        placement.BeginPlacement(PlacementTeam.Red);
        yield return new WaitUntil(() => placement.IsFinished());
        allMines.AddRange(placement.GetPlacedMinesThisTeam());

        // 3) Hide & arm all mines
        foreach (var m in allMines)
        {
            var sr = m.GetComponent<SpriteRenderer>();
            if (sr) sr.enabled = false;

            var mb = m.GetComponent<Mine_Blue>(); if (mb) mb.Arm();
            var mr = m.GetComponent<Mine_Red>();  if (mr) mr.Arm();
        }
        if (turnLabel) turnLabel.text = "";

        // 4) Re-enable gameplay
        if (leftGoalTrigger)  leftGoalTrigger.enabled  = true;
        if (rightGoalTrigger) rightGoalTrigger.enabled = true;

        if (puckRb) puckRb.simulated = true;

        currentTime = Mathf.Max(0f, matchTime);
        UpdateTimerUI();
        matchRunning = true;

        Debug.Log("Mines placed by both teams — match started.");
    }
}
