using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    private PlayerController controller;
    private Transform spriteTransform; // Sebaiknya arahkan ke objek visual jika terpisah, atau transform utama
    private Vector3 originalScale;

    [Header("Squash & Stretch settings")]
    [Range(0.01f, 1f)] public float restoreSpeed = 10f;
    public float jumpStretchX = 0.8f;
    public float jumpStretchY = 1.3f;
    public float landSquashX = 1.3f;
    public float landSquashY = 0.7f;

    [Header("Idle Bobbing")]
    public float idleBobSpeed = 4f;
    public float idleBobAmount = 0.05f;

    [Header("Run Bobbing & Tilting")]
    public float runBobSpeed = 14f;
    public float runBobAmount = 0.12f;
    public float runTiltAngle = 8f; // Derajat kemiringan saat lari

    private bool wasGroundedLastFrame;
    private float currentTilt = 0f;

    void Start()
    {
        controller = GetComponent<PlayerController>();
        
        // Menggunakan transform anak/child pertama jika ada sprite render di objek anak, 
        // atau gunakan transform utama jika visual digabung.
        // Berdasarkan setup kita, SpriteRenderer berada langsung di GameObject Player utama.
        spriteTransform = transform; 
        originalScale = spriteTransform.localScale;
        wasGroundedLastFrame = controller.IsGrounded;
    }

    void Update()
    {
        bool isGrounded = controller.IsGrounded;
        float inputX = controller.HorizontalInput;
        Rigidbody2D rb = controller.Rb;

        // 1. LERPING BACK TO ORIGINAL SCALE (Memulihkan bentuk asli)
        spriteTransform.localScale = Vector3.Lerp(spriteTransform.localScale, originalScale, Time.deltaTime * restoreSpeed);

        // 2. DETEKSI LOMPAT & MENDARAT (SQUASH & STRETCH)
        if (isGrounded && !wasGroundedLastFrame)
        {
            // Baru saja mendarat (Land Squash)
            ApplySquashStretch(landSquashX, landSquashY);
        }
        else if (!isGrounded && wasGroundedLastFrame && rb.linearVelocity.y > 0.1f)
        {
            // Baru saja melompat (Jump Stretch)
            ApplySquashStretch(jumpStretchX, jumpStretchY);
        }

        // 3. EFEK VISUAL: BERJALAN vs IDLE
        if (isGrounded)
        {
            // Pulihkan rotasi ke tegak lurus secara perlahan
            currentTilt = Mathf.Lerp(currentTilt, 0f, Time.deltaTime * 10f);

            if (Mathf.Abs(inputX) > 0.1f)
            {
                // ---- ANIMASI LARI (BERJALAN) ----
                // Efek Memantul (Bobbing)
                float bob = Mathf.Sin(Time.time * runBobSpeed) * runBobAmount;
                Vector3 targetScale = new Vector3(
                    originalScale.x * (1f - bob * 0.5f), 
                    originalScale.y * (1f + bob), 
                    originalScale.z
                );
                spriteTransform.localScale = Vector3.Lerp(spriteTransform.localScale, targetScale, Time.deltaTime * 15f);

                // Efek Miring Ke Depan (Tilting)
                // Arah hadap ditentukan oleh scale.x (positif kanan, negatif kiri)
                float direction = Mathf.Sign(transform.localScale.x);
                float targetTilt = -direction * inputX * runTiltAngle;
                currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * 8f);
            }
            else
            {
                // ---- ANIMASI IDLE (BERNAFAS) ----
                float breathe = Mathf.Sin(Time.time * idleBobSpeed) * idleBobAmount;
                Vector3 targetScale = new Vector3(
                    originalScale.x * (1f + breathe), 
                    originalScale.y * (1f - breathe), 
                    originalScale.z
                );
                spriteTransform.localScale = Vector3.Lerp(spriteTransform.localScale, targetScale, Time.deltaTime * 5f);
            }
        }
        else
        {
            // Di Udara (Melayang)
            // Miringkan sedikit badan ke arah gerakan horizontal di udara
            float targetTilt = -rb.linearVelocity.x * 1.5f;
            targetTilt = Mathf.Clamp(targetTilt, -runTiltAngle, runTiltAngle);
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * 5f);
        }

        // Terapkan rotasi kemiringan
        spriteTransform.rotation = Quaternion.Euler(0f, 0f, currentTilt);

        wasGroundedLastFrame = isGrounded;
    }

    public void ApplySquashStretch(float scaleX, float scaleY)
    {
        // Tetapkan scale secara instan untuk efek hentakan yang kuat
        // Tanda arah scale.x asli dipertahankan agar arah hadap kiri/kanan tidak tertukar
        float directionX = Mathf.Sign(spriteTransform.localScale.x);
        spriteTransform.localScale = new Vector3(
            originalScale.x * scaleX * directionX, 
            originalScale.y * scaleY, 
            originalScale.z
        );
    }
}
