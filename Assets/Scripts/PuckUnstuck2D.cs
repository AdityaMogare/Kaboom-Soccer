using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PuckUnstuck2D : MonoBehaviour
{
    public float minSpeed = 0.2f;    
    public float checkTime = 0.25f;  
    public float nudgeImpulse = 0.5f;
    public LayerMask wallLayers;     

    Rigidbody2D rb;
    float stillTimer;

    void Awake() { rb = GetComponent<Rigidbody2D>(); }

    void FixedUpdate()
    {
        if (rb.linearVelocity.magnitude < minSpeed) stillTimer += Time.fixedDeltaTime;
        else stillTimer = 0f;

        if (stillTimer < checkTime) return;

        ContactPoint2D[] contacts = new ContactPoint2D[8];
        int count = rb.GetContacts(contacts);
        Vector2 push = Vector2.zero;

        for (int i = 0; i < count; i++)
        {
            var c = contacts[i];
            if (((1 << c.collider.gameObject.layer) & wallLayers) != 0)
                push += c.normal; // normal points away from the wall
        }

        if (push.sqrMagnitude > 0.0001f)
        {
            rb.AddForce(push.normalized * nudgeImpulse, ForceMode2D.Impulse);
            stillTimer = 0f;
        }
    }
}
