using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;
    public float doubleJumpForce = 10f;

    [Header("Speed Boost")]
    public float speedBoostMultiplier = 1.8f;
    public float speedBoostDuration = 3f;
    private bool isSpeedBoosted = false;
    private float speedBoostTimer = 0f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    [Tooltip("Pilih layer mana saja yang dianggap sebagai tanah. Default: Everything (Semua).")]
    public LayerMask groundLayer = ~0; // Default ke "Everything" agar langsung mendeteksi tanah apa pun

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private bool isGrounded;
    private bool canDoubleJump;
    private float horizontalInput;

    private Color originalColor = Color.white;
    private Color boostColor = new Color(1f, 0.5f, 0.5f);

    public bool IsGrounded => isGrounded;
    public float HorizontalInput => horizontalInput;
    public Rigidbody2D Rb => rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Setup groundCheck secara dinamis berdasarkan ukuran collider karakter Anda
        if (groundCheck == null)
        {
            GameObject checkObj = new GameObject("GroundCheck");
            checkObj.transform.SetParent(transform);
            
            // Hitung batas bawah collider secara otomatis agar pas untuk karakter ukuran apa pun
            float bottomY = -0.5f; 
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                if (col is BoxCollider2D box)
                {
                    bottomY = box.offset.y - (box.size.y / 2f);
                }
                else if (col is CapsuleCollider2D capsule)
                {
                    bottomY = capsule.offset.y - (capsule.size.y / 2f);
                }
                else if (col is CircleCollider2D circle)
                {
                    bottomY = circle.offset.y - circle.radius;
                }
            }
            
            // Posisikan sedikit di bawah ujung collider
            checkObj.transform.localPosition = new Vector3(0f, bottomY - 0.05f, 0f);
            groundCheck = checkObj.transform;
        }
    }

    void Update()
    {
        // ===== INPUT KEYBOARD SAJA (New Input System) =====
        horizontalInput = 0f;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            bool leftPressed = kb.aKey.isPressed || kb.leftArrowKey.isPressed;
            bool rightPressed = kb.dKey.isPressed || kb.rightArrowKey.isPressed;

            if (leftPressed && !rightPressed)
                horizontalInput = -1f;
            else if (rightPressed && !leftPressed)
                horizontalInput = 1f;

            if (kb.spaceKey.wasPressedThisFrame)
            {
                TriggerJump();
            }
        }

        // Membalik arah hadap sprite
        if (horizontalInput > 0.1f)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (horizontalInput < -0.1f)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }

        // Deteksi Tanah (Aman: Mengabaikan collider milik player itu sendiri)
        isGrounded = false;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayer);
        foreach (var col in colliders)
        {
            if (col.gameObject != gameObject)
            {
                isGrounded = true;
                break;
            }
        }

        if (isGrounded)
        {
            canDoubleJump = true;
        }

        // Update Timer Speed Boost
        if (isSpeedBoosted)
        {
            speedBoostTimer -= Time.deltaTime;
            if (speedBoostTimer <= 0f)
            {
                DeactivateSpeedBoost();
            }
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
            animator.SetBool("isGrounded", isGrounded);
        }
    }

    void FixedUpdate()
    {
        float currentSpeed = moveSpeed;
        if (isSpeedBoosted)
        {
            currentSpeed *= speedBoostMultiplier;
        }

        rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);
    }

    private void TriggerJump()
    {
        if (isGrounded)
        {
            Jump(jumpForce);
        }
        else if (canDoubleJump)
        {
            Jump(doubleJumpForce);
            canDoubleJump = false;
            if (animator != null)
            {
                animator.SetTrigger("doubleJump");
            }
        }
    }

    private void Jump(float force)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
    }

    public void ActivateSpeedBoost()
    {
        isSpeedBoosted = true;
        speedBoostTimer = speedBoostDuration;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = boostColor;
        }
    }

    private void DeactivateSpeedBoost()
    {
        isSpeedBoosted = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
