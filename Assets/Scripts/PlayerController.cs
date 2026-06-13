using UnityEngine;
using UnityEngine.InputSystem;

// Dieu khien nhan vat: di chuyen trai/phai, nhay, animation va chet
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float jumpForce = 14f;
    public LayerMask groundMask;
    public float fallDeathY = -10f;

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;
    BoxCollider2D box;
    bool isGrounded;

    [HideInInspector] public float simMove; // input mo phong (-1..1), 0 = dung input that
    [HideInInspector] public bool simJump;

    public bool IsDead { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        box = GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        if (IsDead) return;

        // Roi xuong vuc thi chet
        if (transform.position.y < fallDeathY)
        {
            if (GameManager.Instance != null) GameManager.Instance.PlayerDied();
            return;
        }

        float move = 0f;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) move -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) move += 1f;
        }

        // Kenh input mo phong cho AutoPilot test
        if (Mathf.Abs(simMove) > 0.01f) move = simMove;

        rb.linearVelocity = new Vector2(move * moveSpeed, rb.linearVelocity.y);

        isGrounded = CheckGrounded();
        bool jumpPressed = kb != null &&
            (kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame);
        if (simJump) { jumpPressed = true; simJump = false; }
        if (jumpPressed && isGrounded)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        if (move > 0.01f) sr.flipX = false;
        else if (move < -0.01f) sr.flipX = true;

        anim.SetFloat("Speed", Mathf.Abs(move));
        anim.SetBool("IsGrounded", isGrounded);
    }

    bool CheckGrounded()
    {
        Bounds b = box.bounds;
        return Physics2D.OverlapBox(
            new Vector2(b.center.x, b.min.y - 0.05f),
            new Vector2(b.size.x * 0.9f, 0.1f), 0f, groundMask) != null;
    }

    // Nay len khi dap trung dau enemy
    public void Bounce()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.7f);
    }

    public void Die()
    {
        if (IsDead) return;
        IsDead = true;
        anim.SetBool("IsDead", true);
        anim.SetFloat("Speed", 0f);
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
    }

    public void Respawn(Vector3 position)
    {
        transform.position = position;
        IsDead = false;
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        anim.SetBool("IsDead", false);
        sr.flipX = false;
    }
}
