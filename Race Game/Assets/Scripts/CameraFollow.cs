using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform playerCar;

    [Header("Position Settings")]
    public Vector3 offset = new Vector3(0f, 5f, -10f); 

    [Header("Smooth Settings")]
    [Range(0f, 1f)]
    public float smoothSpeed = 0.125f;

    void LateUpdate()
    {
        if (playerCar == null) return;
        Vector3 desiredPosition = playerCar.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
        transform.LookAt(playerCar);
    }
}
