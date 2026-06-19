using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class FinishFlag : MonoBehaviour
{
    [Header("UI Settings (Optional)")]
    [Tooltip("Seret GameObject UI Text atau Canvas yang berisi tulisan FINISH ke sini. Jika dikosongkan, teks FINISH akan otomatis muncul di tengah layar.")]
    public GameObject finishUIPanel;

    [Header("Level Settings")]
    public float restartDelay = 4f;

    private bool hasWon = false;

    private void Start()
    {
        // Pastikan panel UI mati di awal permainan
        if (finishUIPanel != null)
        {
            finishUIPanel.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Deteksi jika yang menabrak bendera adalah Player
        if (other.CompareTag("Player") && !hasWon)
        {
            hasWon = true;
            
            // 1. Jika ada UI Panel khusus, aktifkan
            if (finishUIPanel != null)
            {
                finishUIPanel.SetActive(true);
            }

            // 2. Jalankan routine untuk memuat ulang level setelah jeda
            StartCoroutine(WinRoutine());
        }
    }

    private IEnumerator WinRoutine()
    {
        Debug.Log("FINISH! Level Selesai!");
        yield return new WaitForSeconds(restartDelay);
        
        // Memuat ulang scene saat ini
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Failsafe: Jika user tidak memakai Canvas UI, teks tetap muncul di layar otomatis
    private void OnGUI()
    {
        if (hasWon && finishUIPanel == null)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 60; // Ukuran teks besar
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.yellow; // Warna kuning mencolok
            style.alignment = TextAnchor.MiddleCenter;

            // Efek bayangan hitam (drop shadow) agar teks mudah dibaca
            GUIStyle shadowStyle = new GUIStyle(style);
            shadowStyle.normal.textColor = Color.black;

            Rect rect = new Rect(0, 0, Screen.width, Screen.height);
            
            // Gambar bayangan dulu, baru teks utama
            GUI.Label(new Rect(rect.x + 3, rect.y + 3, rect.width, rect.height), "FINISH!", shadowStyle);
            GUI.Label(rect, "FINISH!", style);
        }
    }
}
