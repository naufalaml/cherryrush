using UnityEngine;

public class MovingHazard : MonoBehaviour
{
    public enum MovementDirection { Horizontal, Vertical }

    [Header("Settings")]
    public MovementDirection direction = MovementDirection.Horizontal;
    public float speed = 3f;
    public float distance = 4f;

    private Vector3 startPosition;
    private int factor = 1;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // Hitung perpindahan menggunakan sin untuk pergerakan halus (smooth swing/patrol)
        float offset = Mathf.Sin(Time.time * speed) * (distance / 2f);

        if (direction == MovementDirection.Horizontal)
        {
            transform.position = startPosition + new Vector3(offset, 0f, 0f);
        }
        else
        {
            transform.position = startPosition + new Vector3(0f, offset, 0f);
        }
    }
}
