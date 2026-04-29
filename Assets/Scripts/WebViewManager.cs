using System.Collections;
using UnityEngine;

/// <summary>
/// Owns the WebViewObject lifecycle: initialisation, show/hide toggling,
/// player freeze/unfreeze, cursor management, and navigation controls.
/// </summary>
public class WebViewManager : MonoBehaviour
{
    [SerializeField] private string startUrl = "https://example.com";
    [SerializeField] private bool useNativeWindowsOverlay = true;

    /// <summary>
    /// The FirstPersonController to freeze while the webview is open.
    /// Assign in the Inspector. Missing reference is non-fatal — a warning is logged.
    /// </summary>
    [SerializeField] private FirstPersonController playerController;

    private WebViewObject webViewObject;

    /// <summary>Whether the webview overlay is currently visible.</summary>
    public bool IsVisible { get; private set; }

    private IEnumerator Start()
    {
        if (playerController == null)
        {
            Debug.LogWarning("[WebViewManager] playerController is not assigned — movement freeze will not work.");
        }

        webViewObject = new GameObject("WebViewObject").AddComponent<WebViewObject>();

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        webViewObject.windowsUseNativeOverlay = useNativeWindowsOverlay;
#endif

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo != null)
        {
            webViewObject.canvas = canvasGo;
        }
        else
        {
            Debug.LogWarning("[WebViewManager] No Canvas found in the scene. The WebView overlay may not appear on Mac/Windows.");
        }
#endif

        webViewObject.Init(
            cb: (msg) => Debug.Log($"[WebViewManager] JS Message: {msg}"),
            err: (msg) => Debug.Log($"[WebViewManager] Error: {msg}"),
            httpErr: (msg) => Debug.Log($"[WebViewManager] HTTP Error: {msg}"),
            started: (msg) => Debug.Log($"[WebViewManager] Page Started: {msg}"),
            ld: (msg) =>
            {
                Debug.Log($"[WebViewManager] Page Loaded: {msg}");
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS
                webViewObject.EvaluateJS(@"
                    if (!(window.webkit && window.webkit.messageHandlers)) {
                        window.Unity = {
                            call: function(msg) {
                                window.location = 'unity:' + msg;
                            }
                        };
                    }
                ");
#elif UNITY_WEBGL
                webViewObject.EvaluateJS(@"
                    window.Unity = {
                        call: function(msg) {
                            parent.unityWebView.sendMessage('WebViewObject', msg);
                        }
                    };
                ");
#endif
            }
            // Uncomment to enable WKWebView on iOS:
            //enableWKWebView: true,
            //wkContentMode: 0
        );

        while (!webViewObject.IsInitialized())
        {
            yield return null;
        }

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        webViewObject.bitmapRefreshCycle = 1;
        webViewObject.devicePixelRatio = 1;
#endif

        webViewObject.SetMargins(0, 0, 0, 0);
        webViewObject.SetVisibility(false); // hidden until the player presses E
        webViewObject.LoadURL(startUrl.Replace(" ", "%20"));
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Shows or hides the webview and freezes/unfreezes the player accordingly.</summary>
    public void SetWebViewVisible(bool visible)
    {
        webViewObject?.SetVisibility(visible);
        IsVisible = visible;
        playerController?.SetEnabled(!visible);
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = visible;
    }

    /// <summary>Navigates back in the webview history.</summary>
    public void GoBack()
    {
        webViewObject?.GoBack();
    }

    /// <summary>Navigates forward in the webview history.</summary>
    public void GoForward()
    {
        webViewObject?.GoForward();
    }

    /// <summary>Reloads the current page.</summary>
    public void Reload()
    {
        webViewObject?.Reload();
    }

    // ─── Navigation GUI ───────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (!IsVisible)
        {
            return;
        }

        const int ButtonSize = 80;
        const int ButtonY = 10;
        int x = 10;

        GUI.enabled = webViewObject != null && webViewObject.CanGoBack();
        if (GUI.Button(new Rect(x, ButtonY, ButtonSize, ButtonSize), "<"))
        {
            GoBack();
        }
        GUI.enabled = true;
        x += ButtonSize + 10;

        GUI.enabled = webViewObject != null && webViewObject.CanGoForward();
        if (GUI.Button(new Rect(x, ButtonY, ButtonSize, ButtonSize), ">"))
        {
            GoForward();
        }
        GUI.enabled = true;
        x += ButtonSize + 10;

        if (GUI.Button(new Rect(x, ButtonY, ButtonSize, ButtonSize), "r"))
        {
            Reload();
        }
    }
}
