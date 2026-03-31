using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

[RequireComponent(typeof(Rigidbody))]
public class DualSenseTest : MonoBehaviour
{
    private DualSenseGamepadHID dualSense;
    private Rigidbody rb;

    [Header("References")]
    public Transform cameraRoot;   // 카메라 부모 상하 회전용
    public Camera playerCamera;    // 메인 카메라

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

    private Vector2 moveInput;
    private Vector2 lookInput;
    private float pitch;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        FindDualSense();

        // Rigidbody 설정 권장
        rb.freezeRotation = true;

        if (dualSense != null)
        {
            SetLight(idleColor);
            Debug.Log($"DualSense connected: {dualSense.displayName}");
        }
        else
        {
            Debug.LogWarning("DualSenseGamepadHID not found. USB 연결 여부와 Input System 설정을 확인하세요.");
        }

        if (cameraRoot == null && playerCamera != null)
            cameraRoot = playerCamera.transform.parent;

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        // 컨트롤러가 중간에 연결되어도 대응
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

        // 왼쪽 스틱: 이동
        moveInput = dualSense.leftStick.ReadValue();

        // 오른쪽 스틱: 시점 회전
        lookInput = dualSense.rightStick.ReadValue();

        HandleLook();

        // Cross 버튼: 왼쪽 모터 진동
        if (dualSense.buttonSouth.wasPressedThisFrame)
        {
            Pulse(1.0f, 0f, 0.15f, activeColor);
            Debug.Log($"DualSense Press Cross: {dualSense.displayName}");
        }

        // Circle 버튼: 오른쪽 모터 진동
        if (dualSense.buttonEast.wasPressedThisFrame)
        {
            Pulse(0f, 1.0f, 0.15f, successColor);
            Debug.Log($"DualSense Press Circle: {dualSense.displayName}");
        }

        // Square 버튼: 약한 오른쪽 진동
        if (dualSense.buttonWest.wasPressedThisFrame)
        {
            Pulse(0f, 0.1f, 0.15f, successColor);
            Debug.Log($"DualSense Press Square: {dualSense.displayName}");
        }

        // Triangle 버튼: 약한 왼쪽 진동
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

        // 플레이어가 바라보는 방향 기준 이동
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
        // 좌우 회전: 플레이어 본체(Yaw)
        float yaw = lookInput.x * lookSensitivity * Time.deltaTime;
        transform.Rotate(0f, yaw, 0f);

        // 상하 회전: 카메라 루트(Pitch)
        float pitchDelta = lookInput.y * lookSensitivity * Time.deltaTime;
        pitch -= pitchDelta;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (cameraRoot != null)
        {
            cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
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
        if (dualSense == null) return;
        dualSense.SetLightBarColor(color);
    }

    private void Pulse(float low, float high, float duration, Color color)
    {
        if (dualSense == null) return;

        dualSense.SetMotorSpeeds(low, high);
        dualSense.SetLightBarColor(color);

        CancelInvoke(nameof(StopHapticsAndRestoreLight));
        Invoke(nameof(StopHapticsAndRestoreLight), duration);
    }

    private void StopHaptics()
    {
        if (dualSense == null) return;
        dualSense.SetMotorSpeeds(0f, 0f);
    }

    private void StopHapticsAndRestoreLight()
    {
        if (dualSense == null) return;

        dualSense.SetMotorSpeeds(0f, 0f);
        dualSense.SetLightBarColor(idleColor);
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
