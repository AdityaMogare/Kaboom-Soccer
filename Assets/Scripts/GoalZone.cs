using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class GoalZone : MonoBehaviour
{
    public enum Side { Left, Right }
    public Side side;

    private Collider2D _col;

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Puck")) return;

      
        if (GameManager.Instance && GameManager.Instance.IsLocked) return;

        
        if (_col) _col.enabled = false;

        var scoringTeam = (side == Side.Left) ? GameManager.Team.Red : GameManager.Team.Blue;
        GameManager.Instance?.OnGoalScored(scoringTeam);

       
        if (_col) StartCoroutine(ReEnableAfter((GameManager.Instance ? GameManager.Instance.postGoalDelay : 0.8f) + 0.1f));
    }

    private System.Collections.IEnumerator ReEnableAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (_col) _col.enabled = true;
    }
}
