using UnityEngine;
using UnityEngine.SceneManagement;

public class Hazard : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            RestartLevel();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            RestartLevel();
        }
    }

    private void RestartLevel()
    {
        // Mengulang Scene yang sedang aktif
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
