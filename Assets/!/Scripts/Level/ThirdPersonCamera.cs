using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Player")]
    public Transform target;

    [Header("Camera Settings")]
    public float distance = 5f;
    public float height = 1.6f;
    public float mouseSensitivity = 0.2f;
    public float followSmoothness = 12f;

    [Header("Camera Reset")]
    public float resetSpeed = 3f;
    public float defaultPitch = 15f;

    [Header("Vertical Rotation")]
    public float minPitch = -30f;
    public float maxPitch = 70f;

    private float yaw;
    private float pitch;
    private bool initialized;

    void Start()
    {
        if (target == null)
            return;

        yaw = target.eulerAngles.y;
        pitch = defaultPitch;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        bool isRotating =
            Mouse.current != null &&
            Mouse.current.rightButton.isPressed;

        if (isRotating)
        {
            // Move Camera freely with right click
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            yaw += mouseDelta.x * mouseSensitivity;
            pitch -= mouseDelta.y * mouseSensitivity;

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
        else
        {
            // Camera moves back to start point
            float targetYaw = target.eulerAngles.y;

            yaw = Mathf.LerpAngle(
                yaw,
                targetYaw,
                resetSpeed * Time.deltaTime
            );

            pitch = Mathf.Lerp(
                pitch,
                defaultPitch,
                resetSpeed * Time.deltaTime
            );
        }

        Quaternion rotation = Quaternion.Euler(
            pitch,
            yaw,
            0f
        );

        Vector3 pivotPosition =
            target.position + Vector3.up * height;

        Vector3 desiredPosition =
            pivotPosition -
            rotation * Vector3.forward * distance;

        if (!initialized)
        {
            transform.position = desiredPosition;
            initialized = true;
        }
        else
        {
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                followSmoothness * Time.deltaTime
            );
        }

        transform.rotation = rotation;
    }
}
