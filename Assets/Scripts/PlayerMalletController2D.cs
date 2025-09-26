using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerMalletController2D : MonoBehaviour
{
    public enum ControlScheme { Wasd, Arrows }

    [Header("Input")]
    public ControlScheme controls = ControlScheme.Wasd;

    [Header("Movement")]
    public float moveForce = 45f;
    public float maxSpeed = 12f;

    [Header("Side Clamp (optional)")]
    public bool clampToRect = true;
    // World-space rect: x,y = bottom-left; w,h = size
    public Rect allowedBounds = new Rect(-7.5f, -3.8f, 7.0f, 7.6f);

    [Header("Hit Feel (optional)")]
    [Tooltip("Extra impulse to puck on contact (0 = physics only)")]
    public float hitBoost = 0.8f;

    Rigidbody2D rb;
    Vector2 moveDir;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void Update()
    {
        // Read keys → normalized direction
        if (controls == ControlScheme.Wasd)
        {
            moveDir = new Vector2(
                (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0)
            ).normalized;
        }
        else // Arrows
        {
            moveDir = new Vector2(
                (Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                (Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.DownArrow) ? 1 : 0)
            ).normalized;
        }
    }

    void FixedUpdate()
    {
        // Force-based control
        rb.AddForce(moveDir * moveForce, ForceMode2D.Force);

        // Clamp speed
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        // Optional side clamp
        if (clampToRect)
        {
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, allowedBounds.xMin, allowedBounds.xMax);
            p.y = Mathf.Clamp(p.y, allowedBounds.yMin, allowedBounds.yMax);
            transform.position = p;
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (hitBoost <= 0f) return;

        // Give the puck a tiny extra kick on contact
        if (col.collider.CompareTag("Puck") && col.rigidbody != null)
        {
            var puckRb = col.rigidbody;
            Vector2 toPuck = (puckRb.position - rb.position).normalized;
            float speed = Mathf.Min(rb.linearVelocity.magnitude, maxSpeed);
            puckRb.AddForce(toPuck * speed * hitBoost, ForceMode2D.Impulse);
        }
    }
}
