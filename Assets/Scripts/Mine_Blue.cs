using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Mine_Blue : MonoBehaviour
{
    [Header("Behavior")]
    public bool oneShot = true;
    public bool affectPuck = false;   
    public float knockback = 10f;
    public float destroyDelay = 0.05f;

    [Header("FX (optional)")]
    public ParticleSystem explosionVFX;
    public AudioSource    explosionSFX;
    public Renderer       placementGhost; 

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        if (placementGhost) placementGhost.enabled = true;
    }

    public void Arm() { if (placementGhost) placementGhost.enabled = false; }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!affectPuck && other.CompareTag("Puck")) return;

        var disk = other.GetComponent<PlayerDiskController2D>();
        if (!disk) return;

        // Blue mine only destroys Red (playerId == 2)
        if (disk.playerId != 2) return;

        var rb = other.attachedRigidbody;
        if (rb)
        {
            Vector2 dir = ((Vector2)rb.position - (Vector2)transform.position).normalized;
            rb.AddForce(dir * knockback, ForceMode2D.Impulse);
        }

        disk.Die();

        if (explosionVFX) explosionVFX.Play();
        if (explosionSFX) explosionSFX.Play();

        if (oneShot)
        {
            if (destroyDelay <= 0f) Destroy(gameObject);
            else Destroy(gameObject, destroyDelay);
        }
    }
}
