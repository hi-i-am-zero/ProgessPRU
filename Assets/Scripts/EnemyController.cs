using UnityEngine;

// Enemy di tu phai sang trai. Bi dap dau thi chet (+1 score), cham ngang thi player chet
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    public float moveSpeed = 2.5f;
    public int direction = -1; // -1: di sang trai

    Rigidbody2D rb;
    Animator anim;
    Collider2D col;
    SpriteRenderer sr;
    bool dead;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        sr.flipX = direction > 0;
    }

    void FixedUpdate()
    {
        if (dead) return;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (dead) return;
        var player = collision.collider.GetComponent<PlayerController>();
        if (player == null || player.IsDead) return;

        // Chan player cao hon tam enemy => bi dap dau
        float playerBottom = collision.collider.bounds.min.y;
        float enemyCenter = col.bounds.center.y;
        if (playerBottom > enemyCenter + 0.05f)
        {
            player.Bounce();
            Squash();
        }
        else
        {
            // Cham ngang => player chet
            if (GameManager.Instance != null) GameManager.Instance.PlayerDied();
        }
    }

    void Squash()
    {
        dead = true;
        if (GameManager.Instance != null) GameManager.Instance.AddScore(1);
        anim.SetTrigger("Die");
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        col.enabled = false;
        Destroy(gameObject, 0.4f);
    }
}
