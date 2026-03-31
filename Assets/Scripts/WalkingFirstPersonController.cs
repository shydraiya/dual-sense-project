using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public class WalkingFirstPersonController : MonoBehaviour
{
    private DualSenseGamepadHID dualSense;
    private Rigidbody rb;
    private float yaw;
    private float pitch;

    [Header("Camera")]
    public Camera targetCamera;
    public Transform cameraRoot;
    public Vector3 cameraLocalOffset = new Vector3(0f, 1.6f, 0f);
    public bool reparentMainCameraOnStart = true;

    [Header("Move")]
    public float moveSpeed = 4f;
    public float moveDeadzone = 0.15f;

    [Header("Look")]
    public float lookSensitivity = 120f;
    public float lookDeadzone = 0.15f;
    public bool invertY = false;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (!CompareTag("Player"))
        {
            gameObject.tag = "Player";
        }

        FindDualSense();
        SetupCamera();

        yaw = transform.eulerAngles.y;
        pitch = NormalizeAngle(cameraRoot != null ? cameraRoot.localEulerAngles.x : 0f);
    }

    private void Update()
    {
        if (dualSense == null)
        {
            FindDualSense();
            return;
        }

        HandleLook();
    }

    private void FixedUpdate()
    {
        if (dualSense == null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Vector2 moveInput = dualSense.leftStick.ReadValue();
        if (moveInput.sqrMagnitude < moveDeadzone * moveDeadzone)
        {
            moveInput = Vector2.zero;
        }

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = forward * moveInput.y + right * moveInput.x;
        Vector3 horizontalVelocity = moveDirection * moveSpeed;
        rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
    }

    private void HandleLook()
    {
        Vector2 lookInput = dualSense.rightStick.ReadValue();
        if (lookInput.sqrMagnitude < lookDeadzone * lookDeadzone)
        {
            lookInput = Vector2.zero;
        }

        yaw += lookInput.x * lookSensitivity * Time.deltaTime;

        float pitchDirection = invertY ? 1f : -1f;
        pitch += lookInput.y * lookSensitivity * Time.deltaTime * pitchDirection;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraRoot != null)
        {
            cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void SetupCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (cameraRoot == null)
        {
            Transform existingRoot = transform.Find("CameraRoot");
            if (existingRoot != null)
            {
                cameraRoot = existingRoot;
            }
            else
            {
                GameObject rootObject = new GameObject("CameraRoot");
                cameraRoot = rootObject.transform;
                cameraRoot.SetParent(transform, false);
            }
        }

        cameraRoot.localPosition = cameraLocalOffset;
        cameraRoot.localRotation = Quaternion.identity;

        if (reparentMainCameraOnStart && targetCamera != null)
        {
            targetCamera.transform.SetParent(cameraRoot, false);
            targetCamera.transform.localPosition = Vector3.zero;
            targetCamera.transform.localRotation = Quaternion.identity;
        }
    }

    private void FindDualSense()
    {
        dualSense = Gamepad.current as DualSenseGamepadHID;

        if (dualSense != null)
        {
            return;
        }

        foreach (var device in Gamepad.all)
        {
            if (device is DualSenseGamepadHID ds)
            {
                dualSense = ds;
                break;
            }
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
}
