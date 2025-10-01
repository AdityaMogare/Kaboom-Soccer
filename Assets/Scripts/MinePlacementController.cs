using System.Collections.Generic;
using UnityEngine;

public enum PlacementTeam { Blue, Red }

public class MinePlacementController : MonoBehaviour
{
    [Header("Scene Refs")]
    public Camera mainCam;
    public BoxCollider2D fieldBounds;       // On Field (IsTrigger = true)
    public GameObject blueMinePrefab;       // Blue mine (kills Red)
    public GameObject redMinePrefab;        // Red mine (kills Blue)

    [Header("Rules")]
    public int minesPerTeam = 2;

    [Header("Debug/State (read-only)")]
    public PlacementTeam currentTeam = PlacementTeam.Blue;
    public bool isPlacing = false;
    public int remainingThisTeam = 0;

    private Bounds _bounds;
    private readonly List<GameObject> _placedThisTeam = new();

    void Start()
    {
        if (!mainCam) mainCam = Camera.main;
        _bounds = fieldBounds.bounds;
    }

    void Update()
    {
        if (!isPlacing) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 w = mainCam.ScreenToWorldPoint(Input.mousePosition);
            w.z = 0f;

      
            if (!_bounds.Contains(w)) return;

           
            float centerX = _bounds.center.x;
            bool onRight = w.x >= centerX;
            bool isValidHalf = (currentTeam == PlacementTeam.Blue) ? !onRight : onRight;
            if (!isValidHalf) return;

            
            GameObject prefab = (currentTeam == PlacementTeam.Blue) ? blueMinePrefab : redMinePrefab;
            GameObject mine = Instantiate(prefab, w, Quaternion.identity);

            _placedThisTeam.Add(mine);
            remainingThisTeam--;

            if (remainingThisTeam <= 0)
            {
               
                foreach (var m in _placedThisTeam)
                {
                    var sr = m.GetComponent<SpriteRenderer>();
                    if (sr) sr.enabled = false;
                    
                }

                isPlacing = false; 
            }
        }
    }

    public void BeginPlacement(PlacementTeam team)
    {
        currentTeam = team;
        remainingThisTeam = minesPerTeam;
        _placedThisTeam.Clear();
        isPlacing = true;
    }

    public bool IsFinished() => !isPlacing;

    public List<GameObject> GetPlacedMinesThisTeam()
    {
        return new List<GameObject>(_placedThisTeam);
    }
}
