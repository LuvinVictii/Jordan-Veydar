using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drives WASD first-person movement and mouse-look via the new Input System.
/// Requires a CharacterController on the same GameObject and a Camera child transform.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 2f;

    /// <summary>Assign the Main Camera transform in the Inspector.</summary>
    [SerializeField] private Transform cameraTransform;

    private const float MinVerticalLookAngle = -90f;
    private const float MaxVerticalLookAngle = 90f;
    private const float GravityMultiplier = 2f;

    private CharacterController characterController;
    private float verticalVelocity;
    private float cameraVerticalAngle;
    private bool isInputEnabled = true;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (characterController == null)
        {
            Debug.LogError("[FirstPersonController] CharacterController component is missing. Disabling self.");
            enabled = false;
            return;
        }

        if (cameraTransform == null)
        {
            Debug.LogError("[FirstPersonController] cameraTransform is not assigned. Disabling self.");
            enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!isInputEnabled)
        {
            return;
        }

        HandleMovement();
        HandleMouseLook();
    }

    /// <summary>Enables or disables all player input and movement.</summary>
    public void SetEnabled(bool enabled)
    {
        isInputEnabled = enabled;
    }

    private void HandleMovement()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        Vector3 inputDirection = Vector3.zero;

        if (keyboard.wKey.isPressed) inputDirection += transform.forward;
        if (keyboard.sKey.isPressed) inputDirection -= transform.forward;
        if (keyboard.aKey.isPressed) inputDirection -= transform.right;
        if (keyboard.dKey.isPressed) inputDirection += transform.right;

        // Normalise only if moving diagonally to avoid speed boost.
        if (inputDirection.sqrMagnitude > 1f)
        {
            inputDirection.Normalize();
        }

        // Gravity accumulation.
        if (characterController.isGrounded)
        {
            verticalVelocity = -0.5f; // small constant to keep grounded flag stable
        }
        else
        {
            verticalVelocity += Physics.gravity.y * GravityMultiplier * Time.deltaTime;
        }

        Vector3 velocity = inputDirection * moveSpeed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleMouseLook()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 mouseDelta = mouse.delta.ReadValue();

        // Rotate player on Y axis (horizontal look).
        transform.Rotate(Vector3.up, mouseDelta.x * mouseSensitivity * Time.deltaTime * 100f);

        // Clamp and apply vertical camera rotation.
        cameraVerticalAngle -= mouseDelta.y * mouseSensitivity * Time.deltaTime * 100f;
        cameraVerticalAngle = Mathf.Clamp(cameraVerticalAngle, MinVerticalLookAngle, MaxVerticalLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(cameraVerticalAngle, 0f, 0f);
    }
}
