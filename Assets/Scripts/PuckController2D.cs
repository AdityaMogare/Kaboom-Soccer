
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PuckController2D : MonoBehaviour
{
    [Tooltip("Max allowed puck speed")]
    public float maxSpeed = 18f;

    [Tooltip("If speed drops below this, we gently re-energize it")]
    public float minKeepAliveSpeed = 2.5f;

    [Tooltip("Force used to keep puck from dying out")]
    public float keepAliveForce = 2f;

    private Rigidbody2D rb;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    private void FixedUpdate()
    {
        
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        
        if (rb.linearVelocity.magnitude < minKeepAliveSpeed)
        {
            
            Vector2 dir = rb.linearVelocity.sqrMagnitude > 0.01f
                ? rb.linearVelocity.normalized
                : Random.insideUnitCircle.normalized;

            rb.AddForce(dir * keepAliveForce, ForceMode2D.Force);
        }
    }
}
