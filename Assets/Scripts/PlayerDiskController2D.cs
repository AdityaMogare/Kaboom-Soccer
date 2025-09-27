using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerDiskController2D : MonoBehaviour
{
    [Header("Team")]
    [Tooltip("1 = Blue, 2 = Red")]
    public int playerId = 1;

    [Header("Movement")]
    public float moveForce = 50f;
    public float maxSpeed  = 12f;

    [Header("Side Clamp (optional)")]
    public bool clampToRect = true;
    public Rect allowedBounds = new Rect(-7.6f, -3.8f, 7.2f, 7.6f);

    [Header("Hit Feel (optional)")]
    public float hitBoost = 0.8f;   // extra impulse to puck when active

    [Header("UI (optional)")]
    public GameObject activeRing;   // small highlight object

    public System.Action<PlayerDiskController2D> OnDestroyed;

    private Rigidbody2D rb;
    private Vector2 moveDir;
    public bool isActive;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        if (activeRing) activeRing.SetActive(isActive);
    }

    public void SetMoveDir(Vector2 dir, bool makeActive)
    {
        isActive = makeActive;
        moveDir  = makeActive ? dir : Vector2.zero;
        if (activeRing) activeRing.SetActive(isActive);
    }

    public void Halt()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    public void Die()
    {
        Halt();
        var col = GetComponent<Collider2D>(); if (col) col.enabled = false;
        var r   = GetComponentInChildren<Renderer>(); if (r) r.enabled = false;
        OnDestroyed?.Invoke(this);
        Destroy(gameObject, 0.05f);
    }

    void FixedUpdate()
    {
        if (isActive)
        {
            rb.AddForce(moveDir * moveForce, ForceMode2D.Force);
            if (rb.linearVelocity.magnitude > maxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        if (clampToRect)
        {
            var p = transform.position;
            p.x = Mathf.Clamp(p.x, allowedBounds.xMin, allowedBounds.xMax);
            p.y = Mathf.Clamp(p.y, allowedBounds.yMin, allowedBounds.yMax);
            transform.position = p;
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!isActive || hitBoost <= 0f) return;
        if (col.collider.CompareTag("Puck") && col.rigidbody != null)
        {
            var puckRb = col.rigidbody;
            Vector2 toPuck = (puckRb.position - (Vector2)transform.position).normalized;
            float speed = Mathf.Min(rb.linearVelocity.magnitude, maxSpeed);
            puckRb.AddForce(toPuck * speed * hitBoost, ForceMode2D.Impulse);
        }
    }
}
