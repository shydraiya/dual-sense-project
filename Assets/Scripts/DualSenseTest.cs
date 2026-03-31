using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

[RequireComponent(typeof(Rigidbody))]
public class DualSenseTest : MonoBehaviour
{
    private DualSenseGamepadHID dualSense;
    private Rigidbody rb;

    [Header("References")]
    public Transform cameraRoot;
    public Camera playerCamera;

    [Header("Move")]
    public float moveSpeed = 5f;

    [Header("Look")]
    public float lookSensitivity = 120f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Motor Speeds")]
    [Range(0f, 1f)] public float lowFrequency = 0.25f;
    [Range(0f, 1f)] public float highFrequency = 0.75f;

    [Header("Light Bar")]
    public Color idleColor = Color.blue;
    public Color activeColor = Color.red;
    public Color successColor = Color.green;

    [Header("Respawn")]
    public string enemyTag = "Enemy";

    private Vector2 moveInput;
    private Vector2 lookInput;
    private float pitch;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Quaternion initialCameraLocalRotation = Quaternion.identity;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        FindDualSense();

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        rb.freezeRotation = true;

        if (dualSense != null)
        {
            SetLight(idleColor);
            Debug.Log($"DualSense connected: {dualSense.displayName}");
        }
        else
        {
            Debug.LogWarning("DualSenseGamepadHID not found. Check the USB connection and Input System settings.");
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (cameraRoot == null && playerCamera != null)
        {
            cameraRoot = playerCamera.transform.parent;
        }

        if (cameraRoot != null)
        {
            initialCameraLocalRotation = cameraRoot.localRotation;
            pitch = NormalizeAngle(cameraRoot.localEulerAngles.x);
        }
    }

    private void Update()
    {
        if (dualSense == null)
        {
            FindDualSense();

            if (dualSense != null)
            {
                SetLight(idleColor);
                Debug.Log($"DualSense reconnected: {dualSense.displayName}");
            }

            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            return;
        }

        moveInput = dualSense.leftStick.ReadValue();
        lookInput = dualSense.rightStick.ReadValue();

        HandleLook();

        if (dualSense.buttonSouth.wasPressedThisFrame)
        {
            Pulse(1.0f, 0f, 0.15f, activeColor);
            Debug.Log($"DualSense Press Cross: {dualSense.displayName}");
        }

        if (dualSense.buttonEast.wasPressedThisFrame)
        {
            Pulse(0f, 1.0f, 0.15f, successColor);
            Debug.Log($"DualSense Press Circle: {dualSense.displayName}");
        }

        if (dualSense.buttonWest.wasPressedThisFrame)
        {
            Pulse(0f, 0.1f, 0.15f, successColor);
            Debug.Log($"DualSense Press Square: {dualSense.displayName}");
        }

        if (dualSense.buttonNorth.wasPressedThisFrame)
        {
            Pulse(0.1f, 0f, 0.15f, Color.yellow);
            Debug.Log($"DualSense Press Triangle: {dualSense.displayName}");
        }
    }

    private void FixedUpdate()
    {
        if (dualSense == null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * moveInput.y + right * moveInput.x;
        Vector3 velocity = move * moveSpeed;

        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);
    }

    private void HandleLook()
    {
        float yaw = lookInput.x * lookSensitivity * Time.deltaTime;
        transform.Rotate(0f, yaw, 0f);

        float pitchDelta = lookInput.y * lookSensitivity * Time.deltaTime;
        pitch -= pitchDelta;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (cameraRoot != null)
        {
            cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(enemyTag))
        {
            RespawnToInitialPosition();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(enemyTag))
        {
            RespawnToInitialPosition();
        }
    }

    private void FindDualSense()
    {
        dualSense = Gamepad.current as DualSenseGamepadHID;

        if (dualSense == null)
        {
            foreach (var device in Gamepad.all)
            {
                if (device is DualSenseGamepadHID ds)
                {
                    dualSense = ds;
                    break;
                }
            }
        }
    }

    private void SetLight(Color color)
    {
        if (dualSense == null)
        {
            return;
        }

        dualSense.SetLightBarColor(color);
    }

    private void Pulse(float low, float high, float duration, Color color)
    {
        if (dualSense == null)
        {
            return;
        }

        dualSense.SetMotorSpeeds(low, high);
        dualSense.SetLightBarColor(color);

        CancelInvoke(nameof(StopHapticsAndRestoreLight));
        Invoke(nameof(StopHapticsAndRestoreLight), duration);
    }

    private void StopHapticsAndRestoreLight()
    {
        if (dualSense == null)
        {
            return;
        }

        dualSense.SetMotorSpeeds(0f, 0f);
        dualSense.SetLightBarColor(idleColor);
    }

    private void RespawnToInitialPosition()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(initialPosition, initialRotation);

        if (cameraRoot != null)
        {
            cameraRoot.localRotation = initialCameraLocalRotation;
            pitch = NormalizeAngle(initialCameraLocalRotation.eulerAngles.x);
        }
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }

    private void OnDisable()
    {
        StopHapticsAndRestoreLight();
    }

    private void OnApplicationQuit()
    {
        StopHapticsAndRestoreLight();
    }
}
