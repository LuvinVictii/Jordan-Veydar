using System.Collections;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Owns the WebViewObject lifecycle: initialisation, show/hide toggling,
/// player freeze/unfreeze, cursor management, and navigation controls.
/// </summary>
public class WebViewManager : MonoBehaviour
{
    [SerializeField] private string startUrl = "https://veydarbalancebuddies.lovable.app";
    [SerializeField] private bool useNativeWindowsOverlay = true;

    [Header("Web Crop & Fit (%)")]
    [SerializeField] private bool useEdgeCovers;
    [Range(0f, 100f)]
    [SerializeField] private float coverLeftPercent;
    [Range(0f, 100f)]
    [SerializeField] private float coverTopPercent;
    [Range(0f, 100f)]
    [SerializeField] private float coverRightPercent;
    [Range(0f, 100f)]
    [SerializeField] private float coverBottomPercent;
    [SerializeField] private Color edgeCoverColor = Color.black;

    [Header("Runtime Crop Controls")]
    [SerializeField] private bool showRuntimeCropControls = true;
    [SerializeField] private bool persistRuntimeCropConfig = true;
    [SerializeField] private Rect cropConfigWindowRect = new Rect(10, 100, 360, 330);

    /// <summary>
    /// The FirstPersonController to freeze while the webview is open.
    /// Assign in the Inspector. Missing reference is non-fatal — a warning is logged.
    /// </summary>
    [SerializeField] private FirstPersonController playerController;

    private WebViewObject webViewObject;
    private bool isCropConfigOpen;
    private bool originalUseEdgeCovers;
    private float originalCoverLeftPercent;
    private float originalCoverTopPercent;
    private float originalCoverRightPercent;
    private float originalCoverBottomPercent;
    private bool draftUseEdgeCovers;
    private float draftCoverLeftPercent;
    private float draftCoverTopPercent;
    private float draftCoverRightPercent;
    private float draftCoverBottomPercent;

    /// <summary>Whether the webview overlay is currently visible.</summary>
    public bool IsVisible { get; private set; }

    private IEnumerator Start()
    {
        LoadRuntimeCropConfig();

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
                ApplyWebEdgeCovers();
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
        if (visible)
        {
            ApplyWebEdgeCovers();
        }

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

    private void ApplyWebEdgeCovers()
    {
        if (webViewObject == null)
        {
            return;
        }

        string background = FormatCssColor(edgeCoverColor);
        string enabled = useEdgeCovers ? "true" : "false";
        string left = FormatPercent(useEdgeCovers ? coverLeftPercent : 0f);
        string top = FormatPercent(useEdgeCovers ? coverTopPercent : 0f);
        string right = FormatPercent(useEdgeCovers ? coverRightPercent : 0f);
        string bottom = FormatPercent(useEdgeCovers ? coverBottomPercent : 0f);

        webViewObject.EvaluateJS($@"
(function() {{
    const rootId = 'unity-webview-edge-covers';
    const enabled = {enabled};
    const crop = {{
        left: {left},
        top: {top},
        right: {right},
        bottom: {bottom}
    }};
    const background = '{background}';
    const body = document.body;

    if (!body) {{
        return;
    }}

    let root = document.getElementById(rootId);
    if (!root) {{
        root = document.createElement('div');
        root.id = rootId;
        document.documentElement.appendChild(root);
    }}

    root.innerHTML = '';
    root.style.cssText = 'position:fixed;inset:0;z-index:2147483647;pointer-events:none;';

    if (!enabled) {{
        body.style.transform = '';
        body.style.transformOrigin = '';
        body.style.width = '';
        body.style.minWidth = '';
        body.style.minHeight = '';
        return;
    }}

    const sourceWidth = Math.max(0.01, 100 - crop.left - crop.right);
    const sourceHeight = Math.max(0.01, 100 - crop.top - crop.bottom);
    const scale = Math.min(100 / sourceWidth, 100 / sourceHeight);
    const fittedWidth = sourceWidth * scale;
    const fittedHeight = sourceHeight * scale;

    function getOffset(startCrop, endCrop, fittedSize) {{
        if (startCrop > 0 && endCrop <= 0) {{
            return 0;
        }}
        if (endCrop > 0 && startCrop <= 0) {{
            return 100 - fittedSize;
        }}
        return (100 - fittedSize) / 2;
    }}

    const fittedLeft = getOffset(crop.left, crop.right, fittedWidth);
    const fittedTop = getOffset(crop.top, crop.bottom, fittedHeight);
    const fittedRight = Math.max(0, 100 - fittedLeft - fittedWidth);
    const fittedBottom = Math.max(0, 100 - fittedTop - fittedHeight);

    body.style.transformOrigin = '0 0';
    body.style.width = '100vw';
    body.style.minWidth = '100vw';
    body.style.minHeight = '100vh';
    body.style.transform =
        'translate(' + fittedLeft + 'vw,' + fittedTop + 'vh) ' +
        'scale(' + scale + ') ' +
        'translate(' + (-crop.left) + 'vw,' + (-crop.top) + 'vh)';

    const covers = [
        {{ side: 'left', size: fittedLeft }},
        {{ side: 'top', size: fittedTop }},
        {{ side: 'right', size: fittedRight }},
        {{ side: 'bottom', size: fittedBottom }}
    ];

    for (const cover of covers) {{
        if (cover.size <= 0) {{
            continue;
        }}

        const el = document.createElement('div');
        el.setAttribute('data-unity-edge-cover', cover.side);
        el.style.position = 'fixed';
        el.style.background = background;
        el.style.pointerEvents = 'auto';
        el.style.zIndex = '2147483647';
        ['click', 'dblclick', 'mousedown', 'mouseup', 'mousemove', 'pointerdown', 'pointerup', 'touchstart', 'touchend', 'wheel'].forEach(function(eventName) {{
            el.addEventListener(eventName, function(event) {{
                event.preventDefault();
                event.stopImmediatePropagation();
            }}, {{ capture: true, passive: false }});
        }});

        if (cover.side === 'left') {{
            el.style.left = '0';
            el.style.top = '0';
            el.style.width = cover.size + '%';
            el.style.height = '100%';
        }} else if (cover.side === 'right') {{
            el.style.right = '0';
            el.style.top = '0';
            el.style.width = cover.size + '%';
            el.style.height = '100%';
        }} else if (cover.side === 'top') {{
            el.style.left = '0';
            el.style.top = '0';
            el.style.width = '100%';
            el.style.height = cover.size + '%';
        }} else if (cover.side === 'bottom') {{
            el.style.left = '0';
            el.style.bottom = '0';
            el.style.width = '100%';
            el.style.height = cover.size + '%';
        }}

        root.appendChild(el);
    }}
}})();");
    }

    private static string FormatPercent(float percent)
    {
        return Mathf.Clamp(percent, 0f, 100f).ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatCssColor(Color color)
    {
        int r = Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255f);
        int g = Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255f);
        int b = Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255f);
        string a = Mathf.Clamp01(color.a).ToString("0.###", CultureInfo.InvariantCulture);
        return $"rgba({r},{g},{b},{a})";
    }

    private void OpenCropConfigWindow()
    {
        originalUseEdgeCovers = useEdgeCovers;
        originalCoverLeftPercent = coverLeftPercent;
        originalCoverTopPercent = coverTopPercent;
        originalCoverRightPercent = coverRightPercent;
        originalCoverBottomPercent = coverBottomPercent;

        draftUseEdgeCovers = useEdgeCovers;
        draftCoverLeftPercent = coverLeftPercent;
        draftCoverTopPercent = coverTopPercent;
        draftCoverRightPercent = coverRightPercent;
        draftCoverBottomPercent = coverBottomPercent;
        isCropConfigOpen = true;
    }

    private void ApplyDraftCropConfig()
    {
        useEdgeCovers = draftUseEdgeCovers;
        coverLeftPercent = draftCoverLeftPercent;
        coverTopPercent = draftCoverTopPercent;
        coverRightPercent = draftCoverRightPercent;
        coverBottomPercent = draftCoverBottomPercent;
        ApplyWebEdgeCovers();
    }

    private void SaveCropConfig()
    {
        ApplyDraftCropConfig();

        if (persistRuntimeCropConfig)
        {
            string prefix = nameof(WebViewManager) + ".";
            PlayerPrefs.SetInt(prefix + "UseEdgeCovers", useEdgeCovers ? 1 : 0);
            PlayerPrefs.SetFloat(prefix + "CoverLeftPercent", coverLeftPercent);
            PlayerPrefs.SetFloat(prefix + "CoverTopPercent", coverTopPercent);
            PlayerPrefs.SetFloat(prefix + "CoverRightPercent", coverRightPercent);
            PlayerPrefs.SetFloat(prefix + "CoverBottomPercent", coverBottomPercent);
            PlayerPrefs.Save();
        }

        isCropConfigOpen = false;
    }

    private void CancelCropConfig()
    {
        useEdgeCovers = originalUseEdgeCovers;
        coverLeftPercent = originalCoverLeftPercent;
        coverTopPercent = originalCoverTopPercent;
        coverRightPercent = originalCoverRightPercent;
        coverBottomPercent = originalCoverBottomPercent;
        ApplyWebEdgeCovers();
        isCropConfigOpen = false;
    }

    private void LoadRuntimeCropConfig()
    {
        if (!persistRuntimeCropConfig)
        {
            return;
        }

        string prefix = nameof(WebViewManager) + ".";
        if (!PlayerPrefs.HasKey(prefix + "UseEdgeCovers"))
        {
            return;
        }

        useEdgeCovers = PlayerPrefs.GetInt(prefix + "UseEdgeCovers", useEdgeCovers ? 1 : 0) == 1;
        coverLeftPercent = PlayerPrefs.GetFloat(prefix + "CoverLeftPercent", coverLeftPercent);
        coverTopPercent = PlayerPrefs.GetFloat(prefix + "CoverTopPercent", coverTopPercent);
        coverRightPercent = PlayerPrefs.GetFloat(prefix + "CoverRightPercent", coverRightPercent);
        coverBottomPercent = PlayerPrefs.GetFloat(prefix + "CoverBottomPercent", coverBottomPercent);
    }

    private void DrawCropConfigWindow(int windowId)
    {
        GUILayout.Space(4);

        bool changed = false;
        bool nextEnabled = GUILayout.Toggle(draftUseEdgeCovers, "Aktifkan crop");
        if (nextEnabled != draftUseEdgeCovers)
        {
            draftUseEdgeCovers = nextEnabled;
            changed = true;
        }

        changed |= DrawPercentSlider("Kiri", ref draftCoverLeftPercent);
        changed |= DrawPercentSlider("Atas", ref draftCoverTopPercent);
        changed |= DrawPercentSlider("Kanan", ref draftCoverRightPercent);
        changed |= DrawPercentSlider("Bawah", ref draftCoverBottomPercent);

        if (changed)
        {
            ApplyDraftCropConfig();
        }

        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save", GUILayout.Height(40)))
        {
            SaveCropConfig();
        }

        if (GUILayout.Button("Cancel", GUILayout.Height(40)))
        {
            CancelCropConfig();
        }
        GUILayout.EndHorizontal();

        GUI.DragWindow(new Rect(0, 0, cropConfigWindowRect.width, 28));
    }

    private static bool DrawPercentSlider(string label, ref float value)
    {
        GUILayout.Space(6);
        GUILayout.Label($"{label}: {value:0.#}%");
        float nextValue = GUILayout.HorizontalSlider(value, 0f, 100f);
        nextValue = Mathf.Clamp(nextValue, 0f, 100f);

        if (Mathf.Approximately(nextValue, value))
        {
            return false;
        }

        value = nextValue;
        return true;
    }

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

        x += ButtonSize + 10;
        if (showRuntimeCropControls && GUI.Button(new Rect(x, ButtonY, ButtonSize, ButtonSize), "crop"))
        {
            if (isCropConfigOpen)
            {
                CancelCropConfig();
            }
            else
            {
                OpenCropConfigWindow();
            }
        }

        if (isCropConfigOpen)
        {
            cropConfigWindowRect = GUI.Window(847201, cropConfigWindowRect, DrawCropConfigWindow, "Crop / Zoom");
        }
    }
}
