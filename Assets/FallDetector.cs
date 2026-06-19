using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class FallDetector : MonoBehaviour
{
    [Header("Fall Settings")]
    [Tooltip("Batas posisi koordinat Y minimal. Jika posisi Y karakter kurang dari angka ini, maka dianggap jatuh keluar map.")]
    public float fallThreshold = -6f;
    public float delayBeforeRestart = 3f;

    [Header("UI Settings (Optional)")]
    [Tooltip("Seret GameObject UI Canvas / Panel Game Over Anda ke sini. Jika dikosongkan, teks GAME OVER akan otomatis muncul di tengah layar.")]
    public GameObject gameOverUIPanel;

    private bool isDead = false;

    private void Start()
    {
        // Pastikan panel Game Over mati di awal permainan
        if (gameOverUIPanel != null)
        {
            gameOverUIPanel.SetActive(false);
        }
    }

    void Update()
    {
        // Cek jika posisi Y karakter lebih rendah dari batas jatuh dan belum berstatus mati
        if (transform.position.y < fallThreshold && !isDead)
        {
            isDead = true;
            
            // Nonaktifkan kontrol gerakan karakter agar tidak bisa digerakkan saat mati
            PlayerController controller = GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            // Hentikan gaya gerak fisik karakter
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false; // Mematikan simulasi fisika agar karakter berhenti jatuh bebas
            }

            // Aktifkan Panel UI Game Over jika ada
            if (gameOverUIPanel != null)
            {
                gameOverUIPanel.SetActive(true);
            }

            StartCoroutine(GameOverRoutine());
        }
    }

    private IEnumerator GameOverRoutine()
    {
        Debug.Log("Player jatuh keluar map. GAME OVER!");
        yield return new WaitForSeconds(delayBeforeRestart);

        // Muat ulang level saat ini
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Failsafe: Jika tidak memakai Canvas UI, teks GAME OVER merah tetap muncul otomatis di tengah layar
    private void OnGUI()
    {
        if (isDead && gameOverUIPanel == null)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 60;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.red; // Warna merah mencolok
            style.alignment = TextAnchor.MiddleCenter;

            // Efek bayangan hitam (drop shadow)
            GUIStyle shadowStyle = new GUIStyle(style);
            shadowStyle.normal.textColor = Color.black;

            Rect rect = new Rect(0, 0, Screen.width, Screen.height);
            
            GUI.Label(new Rect(rect.x + 3, rect.y + 3, rect.width, rect.height), "GAME OVER", shadowStyle);
            GUI.Label(rect, "GAME OVER", style);
        }
    }
}
