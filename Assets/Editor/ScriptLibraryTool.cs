using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class ScriptLibraryTool : EditorWindow
{
    private Vector2 leftScroll;
    private Vector2 previewScroll;
    private int selectedCategory = 0;
    private int selectedScript = 0;
    private string searchQuery = "";
    private string savePath = "Assets/Scripts";
    private GUIStyle previewStyle;
    private GUIStyle selectedStyle;
    private bool stylesInit = false;
    private ScriptTemplate currentPreview;

    public class ScriptCategory
    {
        public string name;
        public List<ScriptTemplate> scripts;
        public ScriptCategory(string n, List<ScriptTemplate> s)
        { name = n; scripts = s; }
    }

    public class ScriptTemplate
    {
        public string displayName;
        public string fileName;
        public string code;
        public ScriptTemplate(string display, string file, string c)
        { displayName = display; fileName = file; code = c; }
    }

    private readonly List<ScriptCategory> categories = new List<ScriptCategory>
    {
        new ScriptCategory("🎮 Player", new List<ScriptTemplate>
        {
            new ScriptTemplate("PlayerMovement2D", "PlayerMovement2D",
@"using UnityEngine;
public class PlayerMovement2D : MonoBehaviour
{
    [Header(""Movement"")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    [Header(""Ground Check"")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    private Rigidbody2D rb;
    private bool isGrounded;
    void Awake() => rb = GetComponent<Rigidbody2D>();
    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        float h = Input.GetAxisRaw(""Horizontal"");
        rb.velocity = new Vector2(h * moveSpeed, rb.velocity.y);
        if (Input.GetButtonDown(""Jump"") && isGrounded)
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }
}"),

            new ScriptTemplate("PlayerMovement3D", "PlayerMovement3D",
@"using UnityEngine;
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement3D : MonoBehaviour
{
    [Header(""Movement"")]
    public float moveSpeed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;
    [Header(""Look"")]
    public Transform cameraTransform;
    public float mouseSensitivity = 100f;
    private CharacterController cc;
    private Vector3 velocity;
    private float xRotation;
    void Awake()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }
    void Update()
    {
        if (cc.isGrounded && velocity.y < 0f) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
        float h = Input.GetAxis(""Horizontal"");
        float v = Input.GetAxis(""Vertical"");
        Vector3 move = transform.right * h + transform.forward * v;
        cc.Move(move * moveSpeed * Time.deltaTime);
        if (Input.GetButtonDown(""Jump"") && cc.isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        float mouseX = Input.GetAxis(""Mouse X"") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis(""Mouse Y"") * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        if (cameraTransform)
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}"),

            new ScriptTemplate("PlayerHealth", "PlayerHealth",
@"using UnityEngine;
using UnityEngine.Events;
public class PlayerHealth : MonoBehaviour
{
    [Header(""Health"")]
    public float maxHealth = 100f;
    public float currentHealth { get; private set; }
    public UnityEvent<float> onHealthChanged;
    public UnityEvent onDeath;
    void Awake() => currentHealth = maxHealth;
    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        onHealthChanged?.Invoke(currentHealth / maxHealth);
        if (currentHealth <= 0f) Die();
    }
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        onHealthChanged?.Invoke(currentHealth / maxHealth);
    }
    private void Die()
    {
        onDeath?.Invoke();
        Debug.Log(gameObject.name + "" has died."");
    }
}"),

            new ScriptTemplate("PlayerDash", "PlayerDash",
@"using System.Collections;
using UnityEngine;
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    [Header(""Dash"")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1f;
    public KeyCode dashKey = KeyCode.LeftShift;
    public TrailRenderer trail;
    private Rigidbody2D rb;
    private bool canDash = true;
    private bool isDashing;
    void Awake() => rb = GetComponent<Rigidbody2D>();
    void Update()
    {
        if (Input.GetKeyDown(dashKey) && canDash && !isDashing)
            StartCoroutine(DoDash());
    }
    IEnumerator DoDash()
    {
        canDash = false;
        isDashing = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        float dir = transform.localScale.x > 0 ? 1f : -1f;
        rb.velocity = new Vector2(dir * dashSpeed, 0f);
        if (trail) trail.emitting = true;
        yield return new WaitForSeconds(dashDuration);
        if (trail) trail.emitting = false;
        rb.gravityScale = originalGravity;
        isDashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}"),

            new ScriptTemplate("PlayerStamina", "PlayerStamina",
@"using UnityEngine;
using UnityEngine.Events;
public class PlayerStamina : MonoBehaviour
{
    [Header(""Stamina"")]
    public float maxStamina = 100f;
    public float regenRate = 10f;
    public float regenDelay = 2f;
    public float CurrentStamina { get; private set; }
    public float NormalizedStamina => CurrentStamina / maxStamina;
    public UnityEvent<float> onStaminaChanged;
    private float regenTimer;
    void Awake() => CurrentStamina = maxStamina;
    void Update()
    {
        if (regenTimer > 0f) { regenTimer -= Time.deltaTime; return; }
        if (CurrentStamina < maxStamina)
        {
            CurrentStamina = Mathf.Min(CurrentStamina + regenRate * Time.deltaTime, maxStamina);
            onStaminaChanged?.Invoke(NormalizedStamina);
        }
    }
    public bool UseStamina(float amount)
    {
        if (CurrentStamina < amount) return false;
        CurrentStamina -= amount;
        regenTimer = regenDelay;
        onStaminaChanged?.Invoke(NormalizedStamina);
        return true;
    }
}"),

            new ScriptTemplate("PlayerInventory", "PlayerInventory",
@"using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[System.Serializable]
public class InventoryItem
{
    public string itemName;
    public Sprite icon;
    public int quantity;
}
public class PlayerInventory : MonoBehaviour
{
    public int maxSlots = 20;
    public List<InventoryItem> items = new List<InventoryItem>();
    public UnityEvent onInventoryChanged;
    public bool AddItem(InventoryItem newItem)
    {
        var existing = items.Find(i => i.itemName == newItem.itemName);
        if (existing != null)
        {
            existing.quantity += newItem.quantity;
            onInventoryChanged?.Invoke();
            return true;
        }
        if (items.Count >= maxSlots) { Debug.Log(""Inventory full!""); return false; }
        items.Add(newItem);
        onInventoryChanged?.Invoke();
        return true;
    }
    public bool RemoveItem(string itemName, int amount = 1)
    {
        var item = items.Find(i => i.itemName == itemName);
        if (item == null) return false;
        item.quantity -= amount;
        if (item.quantity <= 0) items.Remove(item);
        onInventoryChanged?.Invoke();
        return true;
    }
    public bool HasItem(string itemName, int amount = 1)
    {
        var item = items.Find(i => i.itemName == itemName);
        return item != null && item.quantity >= amount;
    }
}"),

            new ScriptTemplate("PlayerInteraction", "PlayerInteraction",
@"using UnityEngine;
public interface IInteractable
{
    string InteractPrompt { get; }
    void Interact(GameObject interactor);
}
public class PlayerInteraction : MonoBehaviour
{
    [Header(""Settings"")]
    public float interactRange = 2f;
    public LayerMask interactLayer;
    public KeyCode interactKey = KeyCode.E;
    public GameObject promptUI;
    private IInteractable currentTarget;
    void Update()
    {
        DetectInteractable();
        if (currentTarget != null && Input.GetKeyDown(interactKey))
            currentTarget.Interact(gameObject);
    }
    void DetectInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactLayer);
        if (hits.Length > 0 && hits[0].TryGetComponent(out IInteractable target))
        {
            currentTarget = target;
            if (promptUI) promptUI.SetActive(true);
        }
        else
        {
            currentTarget = null;
            if (promptUI) promptUI.SetActive(false);
        }
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}")
        }),

        new ScriptCategory("🤖 Enemy AI", new List<ScriptTemplate>
        {
            new ScriptTemplate("EnemyPatrol", "EnemyPatrol",
@"using UnityEngine;
public class EnemyPatrol : MonoBehaviour
{
    [Header(""Patrol Points"")]
    public Transform[] waypoints;
    public float moveSpeed = 2f;
    public float waitTime = 1f;
    private int currentWaypoint;
    private float waitTimer;
    private bool waiting;
    void Update()
    {
        if (waypoints.Length == 0) return;
        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f) waiting = false;
            return;
        }
        Transform target = waypoints[currentWaypoint];
        transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
        Vector3 dir = target.position - transform.position;
        if (dir.x != 0) transform.localScale = new Vector3(Mathf.Sign(dir.x), 1f, 1f);
        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
            waiting = true;
            waitTimer = waitTime;
        }
    }
    void OnDrawGizmos()
    {
        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.15f);
            Gizmos.DrawLine(waypoints[i].position, waypoints[(i + 1) % waypoints.Length].position);
        }
    }
}"),

            new ScriptTemplate("EnemyChaseAttack", "EnemyChaseAttack",
@"using UnityEngine;
public class EnemyChaseAttack : MonoBehaviour
{
    public enum State { Idle, Chase, Attack }
    [Header(""Detection"")]
    public Transform player;
    public float detectionRange = 8f;
    public float attackRange = 1.5f;
    [Header(""Stats"")]
    public float moveSpeed = 3f;
    public float attackDamage = 10f;
    public float attackCooldown = 1f;
    private State currentState = State.Idle;
    private float attackTimer;
    void Update()
    {
        if (!player) return;
        float dist = Vector3.Distance(transform.position, player.position);
        currentState = dist <= attackRange ? State.Attack
                     : dist <= detectionRange ? State.Chase
                     : State.Idle;
        attackTimer -= Time.deltaTime;
        switch (currentState)
        {
            case State.Chase: MoveTowards(player.position); break;
            case State.Attack:
                FaceTarget(player.position);
                if (attackTimer <= 0f) DoAttack();
                break;
        }
    }
    void MoveTowards(Vector3 target)
    {
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        FaceTarget(target);
    }
    void FaceTarget(Vector3 target)
    {
        float dir = target.x - transform.position.x;
        if (dir != 0) transform.localScale = new Vector3(Mathf.Sign(dir), 1f, 1f);
    }
    void DoAttack()
    {
        attackTimer = attackCooldown;
        player.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage);
        Debug.Log(""Enemy attacks!"");
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}"),

            new ScriptTemplate("EnemyFieldOfView", "EnemyFieldOfView",
@"using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class EnemyFieldOfView : MonoBehaviour
{
    [Header(""FOV Settings"")]
    public float viewRadius = 10f;
    [Range(0, 360)] public float viewAngle = 90f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;
    public List<Transform> visibleTargets = new List<Transform>();
    void Start() => StartCoroutine(FindTargetsWithDelay(0.2f));
    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }
    void FindVisibleTargets()
    {
        visibleTargets.Clear();
        Collider[] hits = Physics.OverlapSphere(transform.position, viewRadius, targetMask);
        foreach (var hit in hits)
        {
            Transform t = hit.transform;
            Vector3 dir = (t.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dir) < viewAngle / 2f)
            {
                float dist = Vector3.Distance(transform.position, t.position);
                if (!Physics.Raycast(transform.position, dir, dist, obstacleMask))
                    visibleTargets.Add(t);
            }
        }
    }
}"),

            new ScriptTemplate("EnemyHealth", "EnemyHealth",
@"using UnityEngine;
using UnityEngine.Events;
public class EnemyHealth : MonoBehaviour
{
    [Header(""Health"")]
    public float maxHealth = 50f;
    private float currentHealth;
    [Header(""Drop"")]
    public GameObject[] dropPrefabs;
    public float dropChance = 0.5f;
    public UnityEvent onDamaged;
    public UnityEvent onDeath;
    void Awake() => currentHealth = maxHealth;
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        onDamaged?.Invoke();
        if (currentHealth <= 0f) Die();
    }
    void Die()
    {
        onDeath?.Invoke();
        if (dropPrefabs.Length > 0 && Random.value <= dropChance)
            Instantiate(dropPrefabs[Random.Range(0, dropPrefabs.Length)],
                transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}")
        }),

        new ScriptCategory("🎯 Gameplay", new List<ScriptTemplate>
        {
            new ScriptTemplate("Checkpoint", "Checkpoint",
@"using UnityEngine;
public class Checkpoint : MonoBehaviour
{
    public static Vector3 LastCheckpoint { get; private set; }
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.grey;
    private SpriteRenderer sr;
    private bool activated;
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = inactiveColor;
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(""Player"") || activated) return;
        activated = true;
        LastCheckpoint = transform.position;
        if (sr) sr.color = activeColor;
        Debug.Log(""Checkpoint activated at "" + transform.position);
    }
}"),

            new ScriptTemplate("Collectible", "Collectible",
@"using UnityEngine;
public class Collectible : MonoBehaviour
{
    [Header(""Collectible"")]
    public string itemName = ""Coin"";
    public int value = 1;
    public AudioClip pickupSound;
    public GameObject fxPrefab;
    [Header(""Bob"")]
    public float bobHeight = 0.3f;
    public float bobSpeed = 2f;
    private Vector3 startPos;
    void Awake() => startPos = transform.position;
    void Update()
    {
        float y = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(""Player"")) return;
        if (other.TryGetComponent(out PlayerInventory inv))
            inv.AddItem(new InventoryItem { itemName = itemName, quantity = value });
        if (pickupSound) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        if (fxPrefab) Instantiate(fxPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}"),

            new ScriptTemplate("Respawn", "Respawn",
@"using System.Collections;
using UnityEngine;
public class Respawn : MonoBehaviour
{
    [Header(""Settings"")]
    public float respawnDelay = 2f;
    public bool useCheckpoint = true;
    public Vector3 defaultSpawn;
    void OnEnable()
    {
        if (TryGetComponent(out PlayerHealth hp))
            hp.onDeath.AddListener(OnPlayerDeath);
    }
    void OnDisable()
    {
        if (TryGetComponent(out PlayerHealth hp))
            hp.onDeath.RemoveListener(OnPlayerDeath);
    }
    void OnPlayerDeath() => StartCoroutine(DoRespawn());
    IEnumerator DoRespawn()
    {
        yield return new WaitForSeconds(respawnDelay);
        Vector3 spawnPos = useCheckpoint && Checkpoint.LastCheckpoint != Vector3.zero
            ? Checkpoint.LastCheckpoint : defaultSpawn;
        transform.position = spawnPos;
        if (TryGetComponent(out PlayerHealth hp))
            hp.Heal(hp.maxHealth);
        Debug.Log(""Player respawned at "" + spawnPos);
    }
}"),

            new ScriptTemplate("DayNightCycle", "DayNightCycle",
@"using UnityEngine;
public class DayNightCycle : MonoBehaviour
{
    [Header(""Cycle"")]
    public float dayLengthSeconds = 120f;
    [Range(0f, 1f)] public float timeOfDay = 0.25f;
    [Header(""Sun"")]
    public Light sunLight;
    public Gradient sunColor;
    public AnimationCurve sunIntensity;
    [Header(""Ambient"")]
    public Gradient ambientColor;
    public bool IsDay => timeOfDay > 0.25f && timeOfDay < 0.75f;
    void Update()
    {
        timeOfDay += Time.deltaTime / dayLengthSeconds;
        if (timeOfDay >= 1f) timeOfDay = 0f;
        if (sunLight)
        {
            sunLight.transform.rotation = Quaternion.Euler(timeOfDay * 360f - 90f, 170f, 0f);
            sunLight.color = sunColor.Evaluate(timeOfDay);
            sunLight.intensity = sunIntensity.Evaluate(timeOfDay);
        }
        RenderSettings.ambientLight = ambientColor.Evaluate(timeOfDay);
    }
}")
        }),

        new ScriptCategory("💥 Combat", new List<ScriptTemplate>
        {
            new ScriptTemplate("Projectile", "Projectile",
@"using UnityEngine;
public class Projectile : MonoBehaviour
{
    [Header(""Projectile"")]
    public float speed = 15f;
    public float damage = 10f;
    public float lifetime = 3f;
    public LayerMask hitLayers;
    public GameObject hitFxPrefab;
    private Vector3 direction;
    public void Init(Vector3 dir) => direction = dir.normalized;
    void Start() => Destroy(gameObject, lifetime);
    void Update()
    {
        float dist = speed * Time.deltaTime;
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, dist, hitLayers))
        {
            hit.collider.GetComponent<EnemyHealth>()?.TakeDamage(damage);
            hit.collider.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            if (hitFxPrefab) Instantiate(hitFxPrefab, hit.point, Quaternion.identity);
            Destroy(gameObject);
            return;
        }
        transform.Translate(direction * dist, Space.World);
    }
}"),

            new ScriptTemplate("MeleeAttack", "MeleeAttack",
@"using UnityEngine;
public class MeleeAttack : MonoBehaviour
{
    [Header(""Attack"")]
    public float damage = 20f;
    public float attackRange = 1.5f;
    public float attackRate = 1f;
    public LayerMask enemyLayer;
    public KeyCode attackKey = KeyCode.Mouse0;
    [Header(""Audio"")]
    public AudioClip swingSound;
    public AudioClip hitSound;
    private float nextAttackTime;
    private AudioSource audioSource;
    void Awake() => audioSource = GetComponent<AudioSource>();
    void Update()
    {
        if (Input.GetKeyDown(attackKey) && Time.time >= nextAttackTime)
            Attack();
    }
    void Attack()
    {
        nextAttackTime = Time.time + 1f / attackRate;
        if (swingSound && audioSource) audioSource.PlayOneShot(swingSound);
        Collider[] hits = Physics.OverlapSphere(
            transform.position + transform.forward * attackRange * 0.5f,
            attackRange * 0.5f, enemyLayer);
        foreach (var hit in hits)
        {
            hit.GetComponent<EnemyHealth>()?.TakeDamage(damage);
            if (hitSound && audioSource) audioSource.PlayOneShot(hitSound);
        }
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position + transform.forward * attackRange * 0.5f,
            attackRange * 0.5f);
    }
}"),

            new ScriptTemplate("DamageNumber", "DamageNumber",
@"using System.Collections;
using UnityEngine;
using TMPro;
public class DamageNumber : MonoBehaviour
{
    public TextMeshProUGUI text;
    public float floatSpeed = 1f;
    public float lifetime = 1f;
    public Color damageColor = Color.red;
    public Color healColor = Color.green;
    public Color critColor = Color.yellow;
    public void Init(float value, bool isCrit = false, bool isHeal = false)
    {
        text.text = isCrit ? ""CRIT "" + value.ToString(""0"") : value.ToString(""0"");
        text.color = isHeal ? healColor : isCrit ? critColor : damageColor;
        if (isCrit) transform.localScale = Vector3.one * 1.5f;
        StartCoroutine(Animate());
    }
    IEnumerator Animate()
    {
        float t = 0f;
        Color c = text.color;
        while (t < lifetime)
        {
            t += Time.deltaTime;
            transform.position += Vector3.up * floatSpeed * Time.deltaTime;
            c.a = 1f - (t / lifetime);
            text.color = c;
            yield return null;
        }
        Destroy(gameObject);
    }
}"),

            new ScriptTemplate("Knockback", "Knockback",
@"using System.Collections;
using UnityEngine;
[RequireComponent(typeof(Rigidbody2D))]
public class Knockback : MonoBehaviour
{
    [Header(""Knockback"")]
    public float knockbackForce = 8f;
    public float knockbackDuration = 0.2f;
    private Rigidbody2D rb;
    private bool isKnockedBack;
    void Awake() => rb = GetComponent<Rigidbody2D>();
    public void ApplyKnockback(Vector2 sourcePosition)
    {
        if (isKnockedBack) return;
        StartCoroutine(DoKnockback(sourcePosition));
    }
    IEnumerator DoKnockback(Vector2 source)
    {
        isKnockedBack = true;
        Vector2 dir = ((Vector2)transform.position - source).normalized;
        rb.velocity = dir * knockbackForce;
        yield return new WaitForSeconds(knockbackDuration);
        isKnockedBack = false;
    }
}")
        }),

        new ScriptCategory("📷 Camera", new List<ScriptTemplate>
        {
            new ScriptTemplate("CameraFollow", "CameraFollow",
@"using UnityEngine;
public class CameraFollow : MonoBehaviour
{
    [Header(""Target"")]
    public Transform target;
    public Vector3 offset = new Vector3(0f, 2f, -8f);
    public float smoothSpeed = 5f;
    void LateUpdate()
    {
        if (!target) return;
        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        transform.LookAt(target);
    }
}"),

            new ScriptTemplate("CameraShake", "CameraShake",
@"using System.Collections;
using UnityEngine;
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }
    private Vector3 originalPos;
    void Awake() { Instance = this; originalPos = transform.localPosition; }
    public void Shake(float duration, float magnitude)
        => StartCoroutine(DoShake(duration, magnitude));
    IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            transform.localPosition = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = originalPos;
    }
}"),

            new ScriptTemplate("CameraZoom", "CameraZoom",
@"using UnityEngine;
public class CameraZoom : MonoBehaviour
{
    [Header(""Zoom"")]
    public float zoomSpeed = 5f;
    public float minFOV = 20f;
    public float maxFOV = 80f;
    private Camera cam;
    void Awake() => cam = GetComponent<Camera>();
    void Update()
    {
        float scroll = Input.GetAxis(""Mouse ScrollWheel"");
        if (scroll == 0f) return;
        cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - scroll * zoomSpeed, minFOV, maxFOV);
    }
}")
        }),

        new ScriptCategory("⚙️ Managers", new List<ScriptTemplate>
        {
            new ScriptTemplate("GameManager", "GameManager",
@"using UnityEngine;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsGamePaused { get; private set; }
    public int Score { get; private set; }
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void AddScore(int amount) { Score += amount; Debug.Log(""Score: "" + Score); }
    public void TogglePause()
    {
        IsGamePaused = !IsGamePaused;
        Time.timeScale = IsGamePaused ? 0f : 1f;
    }
    public void LoadScene(string name) => SceneManager.LoadScene(name);
    public void ReloadScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    public void QuitGame() => Application.Quit();
}"),

            new ScriptTemplate("AudioManager", "AudioManager",
@"using System.Collections.Generic;
using UnityEngine;
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f,1f)] public float volume = 1f;
        [Range(.1f,3f)] public float pitch = 1f;
        public bool loop;
        [HideInInspector] public AudioSource source;
    }
    public List<Sound> sounds;
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        foreach (var s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
    }
    public void Play(string name)
    {
        var s = sounds.Find(x => x.name == name);
        if (s == null) { Debug.LogWarning(""Sound not found: "" + name); return; }
        s.source.Play();
    }
    public void Stop(string name)
    {
        var s = sounds.Find(x => x.name == name);
        s?.source.Stop();
    }
}"),

            new ScriptTemplate("SaveManager", "SaveManager",
@"using UnityEngine;
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void SaveInt(string key, int value)       => PlayerPrefs.SetInt(key, value);
    public void SaveFloat(string key, float value)   => PlayerPrefs.SetFloat(key, value);
    public void SaveString(string key, string value) => PlayerPrefs.SetString(key, value);
    public int    LoadInt(string key, int d = 0)       => PlayerPrefs.GetInt(key, d);
    public float  LoadFloat(string key, float d = 0f)  => PlayerPrefs.GetFloat(key, d);
    public string LoadString(string key, string d = "")=> PlayerPrefs.GetString(key, d);
    public void SaveObject<T>(string key, T obj) =>
        PlayerPrefs.SetString(key, JsonUtility.ToJson(obj));
    public T LoadObject<T>(string key) =>
        JsonUtility.FromJson<T>(PlayerPrefs.GetString(key));
    public void DeleteAll() => PlayerPrefs.DeleteAll();
}"),

            new ScriptTemplate("SceneLoader", "SceneLoader",
@"using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }
    [Header(""UI"")]
    public GameObject loadingScreen;
    public Slider progressBar;
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void LoadScene(string sceneName) => StartCoroutine(LoadAsync(sceneName));
    IEnumerator LoadAsync(string sceneName)
    {
        if (loadingScreen) loadingScreen.SetActive(true);
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        while (!op.isDone)
        {
            float progress = Mathf.Clamp01(op.progress / 0.9f);
            if (progressBar) progressBar.value = progress;
            if (op.progress >= 0.9f)
            {
                if (progressBar) progressBar.value = 1f;
                yield return new WaitForSeconds(0.3f);
                op.allowSceneActivation = true;
            }
            yield return null;
        }
        if (loadingScreen) loadingScreen.SetActive(false);
    }
}"),

            new ScriptTemplate("EventBus", "EventBus",
@"using System;
using System.Collections.Generic;
public static class EventBus
{
    private static readonly Dictionary<Type, Delegate> events = new Dictionary<Type, Delegate>();
    public static void Subscribe<T>(Action<T> handler)
    {
        if (events.TryGetValue(typeof(T), out var d))
            events[typeof(T)] = Delegate.Combine(d, handler);
        else
            events[typeof(T)] = handler;
    }
    public static void Unsubscribe<T>(Action<T> handler)
    {
        if (events.TryGetValue(typeof(T), out var d))
        {
            var newDel = Delegate.Remove(d, handler);
            if (newDel == null) events.Remove(typeof(T));
            else events[typeof(T)] = newDel;
        }
    }
    public static void Publish<T>(T eventData)
    {
        if (events.TryGetValue(typeof(T), out var d))
            (d as Action<T>)?.Invoke(eventData);
    }
}")
        }),

        new ScriptCategory("🖥️ UI", new List<ScriptTemplate>
        {
            new ScriptTemplate("HealthBar", "HealthBar",
@"using UnityEngine;
using UnityEngine.UI;
public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public Gradient colorGradient;
    public Image fillImage;
    public void SetHealth(float normalizedHealth)
    {
        slider.value = normalizedHealth;
        fillImage.color = colorGradient.Evaluate(normalizedHealth);
    }
}"),

            new ScriptTemplate("MainMenuUI", "MainMenuUI",
@"using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenuUI : MonoBehaviour
{
    [Header(""Scene Names"")]
    public string gameSceneName = ""Game"";
    public string optionsSceneName = ""Options"";
    public void OnPlayButton()    => SceneManager.LoadScene(gameSceneName);
    public void OnOptionsButton() => SceneManager.LoadScene(optionsSceneName);
    public void OnQuitButton()    => Application.Quit();
}"),

            new ScriptTemplate("FadeScreen", "FadeScreen",
@"using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class FadeScreen : MonoBehaviour
{
    public static FadeScreen Instance { get; private set; }
    [SerializeField] private Image panel;
    [SerializeField] private float fadeDuration = 1f;
    void Awake() { Instance = this; panel.gameObject.SetActive(true); }
    public void FadeIn()  => StartCoroutine(Fade(1f, 0f));
    public void FadeOut() => StartCoroutine(Fade(0f, 1f));
    IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        panel.raycastTarget = true;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            panel.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / fadeDuration));
            yield return null;
        }
        panel.raycastTarget = (to >= 1f);
    }
}"),

            new ScriptTemplate("PauseMenu", "PauseMenu",
@"using UnityEngine;
using UnityEngine.SceneManagement;
public class PauseMenu : MonoBehaviour
{
    [Header(""UI"")]
    public GameObject pausePanel;
    public KeyCode pauseKey = KeyCode.Escape;
    private bool isPaused;
    void Update() { if (Input.GetKeyDown(pauseKey)) TogglePause(); }
    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        pausePanel.SetActive(isPaused);
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }
    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
    }
    public void ReturnToMenu(string menuScene)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuScene);
    }
}"),

            new ScriptTemplate("DialogueSystem", "DialogueSystem",
@"using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
[System.Serializable]
public class DialogueLine
{
    public string speaker;
    public string text;
    public Sprite portrait;
}
public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }
    [Header(""UI References"")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;
    public Image portraitImage;
    [Header(""Settings"")]
    public float typeSpeed = 0.04f;
    private DialogueLine[] currentLines;
    private int lineIndex;
    private bool isTyping;
    private Coroutine typingCoroutine;
    void Awake() { Instance = this; dialoguePanel.SetActive(false); }
    void Update()
    {
        if (!dialoguePanel.activeSelf) return;
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (isTyping) SkipTyping();
            else NextLine();
        }
    }
    public void StartDialogue(DialogueLine[] lines)
    {
        currentLines = lines;
        lineIndex = 0;
        dialoguePanel.SetActive(true);
        ShowLine();
    }
    void ShowLine()
    {
        if (lineIndex >= currentLines.Length) { EndDialogue(); return; }
        var line = currentLines[lineIndex];
        speakerText.text = line.speaker;
        if (portraitImage)
        {
            portraitImage.sprite = line.portrait;
            portraitImage.enabled = line.portrait != null;
        }
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLine(line.text));
    }
    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = """";
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
        isTyping = false;
    }
    void SkipTyping()
    {
        StopCoroutine(typingCoroutine);
        dialogueText.text = currentLines[lineIndex].text;
        isTyping = false;
    }
    void NextLine() { lineIndex++; ShowLine(); }
    void EndDialogue() { dialoguePanel.SetActive(false); Debug.Log(""Dialogue ended.""); }
}"),

            new ScriptTemplate("ScoreUI", "ScoreUI",
@"using System.Collections;
using UnityEngine;
using TMPro;
public class ScoreUI : MonoBehaviour
{
    [Header(""References"")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    [Header(""Animation"")]
    public float countSpeed = 0.05f;
    private int displayedScore;
    private int targetScore;
    public void SetScore(int score)
    {
        targetScore = score;
        StopAllCoroutines();
        StartCoroutine(AnimateScore());
        UpdateHighScore(score);
    }
    IEnumerator AnimateScore()
    {
        while (displayedScore != targetScore)
        {
            int step = Mathf.Max(1, Mathf.Abs(targetScore - displayedScore) / 10);
            displayedScore = displayedScore < targetScore
                ? Mathf.Min(displayedScore + step, targetScore)
                : Mathf.Max(displayedScore - step, targetScore);
            scoreText.text = displayedScore.ToString(""N0"");
            yield return new WaitForSeconds(countSpeed);
        }
    }
    void UpdateHighScore(int score)
    {
        int best = PlayerPrefs.GetInt(""HighScore"", 0);
        if (score > best) { PlayerPrefs.SetInt(""HighScore"", score); best = score; }
        if (highScoreText) highScoreText.text = ""Best: "" + best.ToString(""N0"");
    }
}")
        }),

        new ScriptCategory("🔧 Utility", new List<ScriptTemplate>
        {
            new ScriptTemplate("ObjectPooler", "ObjectPooler",
@"using System.Collections.Generic;
using UnityEngine;
public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance { get; private set; }
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }
    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    void Awake()
    {
        Instance = this;
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        foreach (var pool in pools)
        {
            var queue = new Queue<GameObject>();
            for (int i = 0; i < pool.size; i++)
            {
                var obj = Instantiate(pool.prefab, transform);
                obj.SetActive(false);
                queue.Enqueue(obj);
            }
            poolDictionary[pool.tag] = queue;
        }
    }
    public GameObject Spawn(string tag, Vector3 pos, Quaternion rot)
    {
        if (!poolDictionary.ContainsKey(tag)) { Debug.LogWarning(""Pool not found: "" + tag); return null; }
        var obj = poolDictionary[tag].Dequeue();
        obj.SetActive(true);
        obj.transform.SetPositionAndRotation(pos, rot);
        poolDictionary[tag].Enqueue(obj);
        return obj;
    }
}"),

            new ScriptTemplate("Singleton", "Singleton",
@"using UnityEngine;
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }
    protected virtual void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this as T;
        DontDestroyOnLoad(gameObject);
    }
}
// Usage: public class MyManager : Singleton<MyManager> { }
"),

            new ScriptTemplate("Timer", "Timer",
@"using UnityEngine;
using UnityEngine.Events;
public class Timer : MonoBehaviour
{
    public float duration = 10f;
    public bool autoStart = true;
    public bool loop = false;
    public UnityEvent onComplete;
    public float Remaining => Mathf.Max(remaining, 0f);
    public float Progress  => 1f - (remaining / duration);
    public bool  IsRunning => running;
    private float remaining;
    private bool running;
    void Start() { if (autoStart) StartTimer(); }
    public void StartTimer() { remaining = duration; running = true; }
    public void StopTimer()  => running = false;
    public void ResetTimer() => remaining = duration;
    void Update()
    {
        if (!running) return;
        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            running = false;
            onComplete?.Invoke();
            if (loop) StartTimer();
        }
    }
}"),

            new ScriptTemplate("Cooldown", "Cooldown",
@"using UnityEngine;
[System.Serializable]
public class Cooldown
{
    public float duration;
    private float endTime;
    public bool  IsReady   => Time.time >= endTime;
    public float Remaining => Mathf.Max(endTime - Time.time, 0f);
    public float Progress  => IsReady ? 1f : 1f - (Remaining / duration);
    public void Use()   => endTime = Time.time + duration;
    public void Reset() => endTime = 0f;
}
// Usage:
// public Cooldown dashCooldown = new Cooldown { duration = 2f };
// if (dashCooldown.IsReady) { dashCooldown.Use(); }
"),

            new ScriptTemplate("DebugConsole", "DebugConsole",
@"using System.Collections.Generic;
using UnityEngine;
public class DebugConsole : MonoBehaviour
{
    [Header(""Settings"")]
    public KeyCode toggleKey = KeyCode.BackQuote;
    public int maxMessages = 50;
    public float windowWidth = 600f;
    private bool isOpen;
    private List<string> messages = new List<string>();
    private string input = """";
    private Vector2 scroll;
    void OnEnable()  => Application.logMessageReceived += HandleLog;
    void OnDisable() => Application.logMessageReceived -= HandleLog;
    void HandleLog(string msg, string trace, LogType type)
    {
        string prefix = type == LogType.Error ? ""[ERR] ""
                      : type == LogType.Warning ? ""[WARN] "" : ""[LOG] "";
        messages.Add(prefix + msg);
        if (messages.Count > maxMessages) messages.RemoveAt(0);
    }
    void Update() { if (Input.GetKeyDown(toggleKey)) isOpen = !isOpen; }
    void OnGUI()
    {
        if (!isOpen) return;
        float h = Screen.height * 0.4f;
        GUILayout.BeginArea(new Rect(0, 0, windowWidth, h), GUI.skin.box);
        scroll = GUILayout.BeginScrollView(scroll);
        foreach (var msg in messages) GUILayout.Label(msg);
        GUILayout.EndScrollView();
        GUILayout.BeginHorizontal();
        input = GUILayout.TextField(input);
        if (GUILayout.Button(""Run"", GUILayout.Width(50))) ExecuteCommand(input);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
    void ExecuteCommand(string cmd)
    {
        Debug.Log(""> "" + cmd);
        input = """";
        switch (cmd.ToLower().Trim())
        {
            case ""fps"": Application.targetFrameRate = 60; break;
            default: Debug.LogWarning(""Unknown command: "" + cmd); break;
        }
    }
}")
        }),

        new ScriptCategory("🌍 World", new List<ScriptTemplate>
        {
            new ScriptTemplate("MovingPlatform", "MovingPlatform",
@"using UnityEngine;
public class MovingPlatform : MonoBehaviour
{
    [Header(""Waypoints"")]
    public Transform[] waypoints;
    public float speed = 2f;
    public bool pingPong = true;
    private int index = 0;
    private int direction = 1;
    void Update()
    {
        if (waypoints.Length == 0) return;
        transform.position = Vector3.MoveTowards(
            transform.position, waypoints[index].position, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, waypoints[index].position) < 0.05f)
        {
            if (pingPong)
            {
                if (index == waypoints.Length - 1 || index == 0) direction *= -1;
                index += direction;
            }
            else index = (index + 1) % waypoints.Length;
        }
    }
    void OnCollisionEnter(Collision col) => col.transform.SetParent(transform);
    void OnCollisionExit(Collision col)  => col.transform.SetParent(null);
}"),

            new ScriptTemplate("Destructible", "Destructible",
@"using UnityEngine;
public class Destructible : MonoBehaviour
{
    [Header(""Health"")]
    public float health = 30f;
    [Header(""On Destroy"")]
    public GameObject destroyedVersionPrefab;
    public GameObject particleFxPrefab;
    public AudioClip destroySound;
    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0f) Break();
    }
    void Break()
    {
        if (destroyedVersionPrefab)
            Instantiate(destroyedVersionPrefab, transform.position, transform.rotation);
        if (particleFxPrefab)
            Instantiate(particleFxPrefab, transform.position, Quaternion.identity);
        if (destroySound)
            AudioSource.PlayClipAtPoint(destroySound, transform.position);
        Destroy(gameObject);
    }
    void OnCollisionEnter(Collision col)
    {
        if (col.relativeVelocity.magnitude > 5f)
            TakeDamage(col.relativeVelocity.magnitude);
    }
}"),

            new ScriptTemplate("TriggerZone", "TriggerZone",
@"using UnityEngine;
using UnityEngine.Events;
public class TriggerZone : MonoBehaviour
{
    [Header(""Filter"")]
    public string targetTag = ""Player"";
    [Header(""Events"")]
    public UnityEvent<GameObject> onEnter;
    public UnityEvent<GameObject> onExit;
    public UnityEvent<GameObject> onStay;
    [Header(""One Shot"")]
    public bool triggerOnce = false;
    private bool triggered;
    void OnTriggerEnter(Collider other)
    {
        if (!Filter(other) || (triggerOnce && triggered)) return;
        triggered = true;
        onEnter?.Invoke(other.gameObject);
    }
    void OnTriggerExit(Collider other)
    {
        if (Filter(other)) onExit?.Invoke(other.gameObject);
    }
    void OnTriggerStay(Collider other)
    {
        if (Filter(other)) onStay?.Invoke(other.gameObject);
    }
    bool Filter(Collider col) =>
        string.IsNullOrEmpty(targetTag) || col.CompareTag(targetTag);
}"),

            new ScriptTemplate("Buoyancy", "Buoyancy",
@"using UnityEngine;
public class Buoyancy : MonoBehaviour
{
    [Header(""Water"")]
    public float waterLevel = 0f;
    public float buoyancyForce = 15f;
    public float damping = 0.95f;
    [Header(""Floaters"")]
    public Transform[] floatPoints;
    private Rigidbody rb;
    void Awake() => rb = GetComponent<Rigidbody>();
    void FixedUpdate()
    {
        foreach (var point in floatPoints)
        {
            if (point.position.y < waterLevel)
            {
                float submerge = waterLevel - point.position.y;
                rb.AddForceAtPosition(Vector3.up * buoyancyForce * submerge,
                    point.position, ForceMode.Force);
                rb.velocity *= damping;
            }
        }
    }
}")
        }),

        new ScriptCategory("🎵 Audio", new List<ScriptTemplate>
        {
            new ScriptTemplate("MusicPlayer", "MusicPlayer",
@"using System.Collections;
using UnityEngine;
public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer Instance { get; private set; }
    [Header(""Tracks"")]
    public AudioClip[] tracks;
    [Header(""Settings"")]
    public float volume = 0.5f;
    public float crossfadeTime = 1f;
    private AudioSource source1;
    private AudioSource source2;
    private AudioSource activeSource;
    private int currentTrack = -1;
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        source1 = gameObject.AddComponent<AudioSource>();
        source2 = gameObject.AddComponent<AudioSource>();
        source1.loop = source2.loop = true;
        source1.volume = volume;
        source2.volume = 0f;
        activeSource = source1;
    }
    public void PlayTrack(int index)
    {
        if (index == currentTrack || index >= tracks.Length) return;
        currentTrack = index;
        StartCoroutine(Crossfade(tracks[index]));
    }
    IEnumerator Crossfade(AudioClip clip)
    {
        AudioSource next = activeSource == source1 ? source2 : source1;
        next.clip = clip;
        next.Play();
        float t = 0f;
        while (t < crossfadeTime)
        {
            t += Time.deltaTime;
            float p = t / crossfadeTime;
            activeSource.volume = Mathf.Lerp(volume, 0f, p);
            next.volume = Mathf.Lerp(0f, volume, p);
            yield return null;
        }
        activeSource.Stop();
        activeSource = next;
    }
    public void StopMusic() { source1.Stop(); source2.Stop(); }
}"),

            new ScriptTemplate("SoundTrigger", "SoundTrigger",
@"using UnityEngine;
public class SoundTrigger : MonoBehaviour
{
    [Header(""Sound"")]
    public AudioClip clip;
    [Range(0f,1f)] public float volume = 1f;
    [Header(""Settings"")]
    public string targetTag = ""Player"";
    public bool playOnce = true;
    private bool played;
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(targetTag) || (playOnce && played)) return;
        played = true;
        AudioSource.PlayClipAtPoint(clip, transform.position, volume);
    }
    void OnCollisionEnter(Collision col)
    {
        if (!col.gameObject.CompareTag(targetTag) || (playOnce && played)) return;
        played = true;
        AudioSource.PlayClipAtPoint(clip, transform.position, volume);
    }
}"),

            new ScriptTemplate("FootstepSystem", "FootstepSystem",
@"using UnityEngine;
public class FootstepSystem : MonoBehaviour
{
    [System.Serializable]
    public class SurfaceSound
    {
        public string tag;
        public AudioClip[] clips;
    }
    [Header(""Surface Sounds"")]
    public SurfaceSound[] surfaces;
    public AudioClip[] defaultClips;
    [Header(""Settings"")]
    public float stepInterval = 0.5f;
    public float rayDistance = 1.1f;
    public LayerMask groundLayer;
    private AudioSource audioSource;
    private float stepTimer;
    void Awake() => audioSource = GetComponent<AudioSource>();
    void Update()
    {
        stepTimer -= Time.deltaTime;
        bool isMoving = new Vector2(
            Input.GetAxis(""Horizontal""),
            Input.GetAxis(""Vertical"")).magnitude > 0.1f;
        if (isMoving && stepTimer <= 0f)
        {
            PlayFootstep();
            stepTimer = stepInterval;
        }
    }
    void PlayFootstep()
    {
        AudioClip[] clips = defaultClips;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, rayDistance, groundLayer))
        {
            foreach (var surface in surfaces)
            {
                if (hit.collider.CompareTag(surface.tag))
                {
                    clips = surface.clips;
                    break;
                }
            }
        }
        if (clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        audioSource.PlayOneShot(clip);
    }
}")
        })
    };

    [MenuItem("Tools/Script Library")]
    public static void OpenWindow()
    {
        var w = GetWindow<ScriptLibraryTool>("Script Library");
        w.minSize = new Vector2(750, 500);
    }

    void OnGUI()
    {
        InitStyles();
        DrawToolbar();
        EditorGUILayout.BeginHorizontal();
        DrawLeftPanel();
        DrawPreviewPanel();
        EditorGUILayout.EndHorizontal();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Search", EditorStyles.toolbarButton);
        searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarTextField, GUILayout.Width(200));
        GUILayout.FlexibleSpace();
        GUILayout.Label("Save Path", EditorStyles.toolbarButton);
        savePath = EditorGUILayout.TextField(savePath, EditorStyles.toolbarTextField, GUILayout.Width(300));
        EditorGUILayout.EndHorizontal();
    }

    void InitStyles()
    {
        if (stylesInit) return;
        previewStyle = new GUIStyle(EditorStyles.textArea)
        {
            wordWrap = true,
            richText = false
        };
        selectedStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Bold
        };
        selectedStyle.normal.textColor = Color.cyan;
        stylesInit = true;
    }

    void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(260));
        leftScroll = EditorGUILayout.BeginScrollView(leftScroll);

        for (int i = 0; i < categories.Count; i++)
        {
            if (GUILayout.Button(categories[i].name, (i == selectedCategory) ? EditorStyles.whiteLabel : EditorStyles.label))
            {
                selectedCategory = i;
                selectedScript = 0;
                currentPreview = null;
            }
        }

        EditorGUILayout.Space();

        var scripts = categories[selectedCategory].scripts;
        for (int j = 0; j < scripts.Count; j++)
        {
            var s = scripts[j];
            if (!string.IsNullOrEmpty(searchQuery) && !s.displayName.ToLower().Contains(searchQuery.ToLower()))
                continue;
            GUIStyle style = (j == selectedScript) ? selectedStyle : EditorStyles.label;
            if (GUILayout.Button(s.displayName, style))
            {
                selectedScript = j;
                currentPreview = s;
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawPreviewPanel()
    {
        EditorGUILayout.BeginVertical();
        previewScroll = EditorGUILayout.BeginScrollView(previewScroll);

        if (currentPreview == null && categories.Count > 0)
            currentPreview = categories[selectedCategory].scripts[Mathf.Clamp(selectedScript, 0, categories[selectedCategory].scripts.Count - 1)];

        if (currentPreview != null)
        {
            EditorGUILayout.LabelField(currentPreview.displayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(currentPreview.fileName + ".cs", EditorStyles.miniLabel);
            EditorGUILayout.Space();
            EditorGUILayout.TextArea(currentPreview.code, previewStyle, GUILayout.ExpandHeight(true));
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create Script", GUILayout.Width(120)))
        {
            if (currentPreview == null) return;
            try
            {
                if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
                string path = Path.Combine(savePath, currentPreview.fileName + ".cs");
                File.WriteAllText(path, currentPreview.code);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Script Created", "Created: " + path, "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Error", "Failed to create script: " + ex.Message, "OK");
            }
        }

        if (GUILayout.Button("Open Folder", GUILayout.Width(100)))
        {
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
            EditorUtility.RevealInFinder(savePath);
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }
}