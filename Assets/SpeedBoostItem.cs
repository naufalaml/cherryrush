using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class SpeedBoostItem : MonoBehaviour
{
    [Header("Settings")]
    public float respawnTime = 4f;

    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.ActivateSpeedBoost();
                StartCoroutine(CollectRoutine());
            }
        }
    }

    private IEnumerator CollectRoutine()
    {
        // Matikan visual dan collider
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (col != null) col.enabled = false;

        // Tunggu sebelum respawn agar bisa diambil lagi jika pemain terjatuh/gagal
        yield return new WaitForSeconds(respawnTime);

        // Aktifkan kembali
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (col != null) col.enabled = true;
    }
}
