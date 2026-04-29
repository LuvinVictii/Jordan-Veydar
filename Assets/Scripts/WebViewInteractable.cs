using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Place this on any interactable object (e.g. the Cube).
/// When the player is within <see cref="interactionRange"/> and presses E,
/// the webview is toggled open or closed via <see cref="WebViewManager"/>.
/// </summary>
public class WebViewInteractable : MonoBehaviour
{
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private WebViewManager webViewManager;
    [SerializeField] private Transform playerTransform;

    private void Start()
    {
        if (webViewManager == null)
        {
            Debug.LogError("[WebViewInteractable] webViewManager is not assigned. Disabling self.");
            enabled = false;
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogError("[WebViewInteractable] playerTransform is not assigned. Disabling self.");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // Close with Escape from anywhere.
        if (keyboard.escapeKey.wasPressedThisFrame && webViewManager.IsVisible)
        {
            webViewManager.SetWebViewVisible(false);
            return;
        }

        // Open with E when close enough to the interactable.
        if (keyboard.eKey.wasPressedThisFrame && !webViewManager.IsVisible)
        {
            float distance = Vector3.Distance(playerTransform.position, transform.position);
            if (distance <= interactionRange)
            {
                webViewManager.SetWebViewVisible(true);
            }
        }
    }
}
