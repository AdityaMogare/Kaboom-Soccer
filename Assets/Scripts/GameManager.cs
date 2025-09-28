using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public enum Team { Blue, Red }

    // ---------- UI ----------
    [Header("UI Elements")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI blueScoreText;
    public TextMeshProUGUI redScoreText;

    [Header("Game Timer")]
    public float matchTime = 120f;   // seconds (2:00)
    private float currentTime;
    private bool matchRunning = false;

    // ---------- Scores ----------
    [Header("Scores (read-only)")]
    public int blueScore { get; private set; }
    public int redScore  { get; private set; }

    // ---------- Puck / Goals ----------
    [Header("Puck Reset")]
    public Rigidbody2D puckRb;        // assign: Puck's Rigidbody2D
    public Transform   puckSpawn;     // assign: empty at center
    public float postGoalDelay = 0.8f;

    [Header("Goal Triggers (drag the BoxCollider2D from each goal)")]
    public Collider2D leftGoalTrigger;
    public Collider2D rightGoalTrigger;

    // ---------- Player Spawns ----------
    [Header("Team Spawns (size 3 each: B1/B2/B3 and R1/R2/R3)")]
    public Transform[] blueSpawns;    // positions for Blue 1/2/3
    public Transform[] redSpawns;     // positions for Red 1/2/3

    [Header("(Optional) Direct Disk Refs (size 3 each). Leave empty to auto-find by tag.")]
    public Rigidbody2D[] blueDisks;   // Blue 1/2/3 RB2D (optional)
    public Rigidbody2D[] redDisks;    // Red 1/2/3 RB2D (optional)

    // ---------- Locks / State ----------
    private bool goalLock = false;    // prevents double-scoring while resetting
    public bool IsLocked => goalLock;
    public bool IsMatchRunning => matchRunning;

    // Optional event if controllers want to listen for end-of-match
    public System.Action OnMatchEnded;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Reset UI and start the match
        UpdateScoreUI();
        currentTime = Mathf.Max(0f, matchTime);
        UpdateTimerUI();
        matchRunning = true;
    }

    void Update()
    {
        // Countdown timer
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

    // Called by GoalZone
    public void OnGoalScored(Team scoringTeam)
    {
        if (!matchRunning) return;     // ignore goals after time is up
        if (goalLock) return;
        goalLock = true;               // lock immediately
        StartCoroutine(HandleGoalRoutine(scoringTeam));
    }

    private IEnumerator HandleGoalRoutine(Team scoringTeam)
    {
        // 1) Increment score once
        if (scoringTeam == Team.Blue) blueScore++;
        else                          redScore++;
        UpdateScoreUI();
        Debug.Log($"GOAL!  Blue {blueScore} : Red {redScore}   ({scoringTeam} scored)");

        // 2) Disable both goals for the whole reset window
        if (leftGoalTrigger)  leftGoalTrigger.enabled  = false;
        if (rightGoalTrigger) rightGoalTrigger.enabled = false;

        // 3) Freeze physics and teleport puck to center immediately
        if (puckRb)
        {
            puckRb.linearVelocity = Vector2.zero;     // use velocity (works in all versions)
            puckRb.angularVelocity = 0f;
            puckRb.simulated = false;
            if (puckSpawn) puckRb.transform.position = puckSpawn.position;
        }

        // 4) Reset players (only active/alive disks) to their spawn markers
        ResetTeamToSpawns(Team.Blue);
        ResetTeamToSpawns(Team.Red);

        // 5) Brief pause for "goal" moment
        yield return new WaitForSeconds(postGoalDelay);

        // 6) Re-enable physics and goal triggers, unlock
        if (puckRb) puckRb.simulated = true;
        if (leftGoalTrigger)  leftGoalTrigger.enabled  = true;
        if (rightGoalTrigger) rightGoalTrigger.enabled = true;

        goalLock = false;
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

    private void EndMatch()
    {
        Debug.Log("MATCH OVER!");
        // Disable goal triggers so no more scoring
        if (leftGoalTrigger)  leftGoalTrigger.enabled  = false;
        if (rightGoalTrigger) rightGoalTrigger.enabled = false;

        // Optionally freeze puck & players
        if (puckRb)
        {
            puckRb.linearVelocity = Vector2.zero;
            puckRb.angularVelocity = 0f;
            puckRb.simulated = false;
        }

        // Notify listeners (e.g., controllers can stop reading input)
        OnMatchEnded?.Invoke();
    }

    // ---------- Respawn helpers ----------
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
            if (!rb.gameObject.activeInHierarchy) continue; // skip eliminated disks

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
            rb.transform.position = spawn.position;
        }
        for (int i = 0; i < count; i++)
        {
            var rb = disks[i];
            if (rb == null) continue;
            if (!rb.gameObject.activeInHierarchy) continue;
            rb.simulated = true;
        }
    }

    private Rigidbody2D[] GetTeamDisks(Team team)
    {
        // Use Inspector-provided arrays if present
        if (team == Team.Blue && blueDisks != null && blueDisks.Length > 0) return blueDisks;
        if (team == Team.Red  && redDisks  != null && redDisks.Length  > 0) return redDisks;

        // Otherwise auto-find by tag. Make sure your disks are tagged "BlueDisk" / "RedDisk".
        string tag = (team == Team.Blue) ? "BlueDisk" : "RedDisk";
        GameObject[] gos = GameObject.FindGameObjectsWithTag(tag);
        List<Rigidbody2D> list = new List<Rigidbody2D>(gos.Length);
        foreach (var go in gos)
        {
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) list.Add(rb);
        }
        // Sort by name so "Blue 1","Blue 2","Blue 3" map to B1,B2,B3 consistently
        list.Sort((a, b) => a.name.CompareTo(b.name));
        return list.ToArray();
    }
}
