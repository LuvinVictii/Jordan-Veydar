using UnityEngine;
public class Collectible : MonoBehaviour
{
    [Header("Collectible")]
    public string itemName = "Coin";
    public int value = 1;
    public AudioClip pickupSound;
    public GameObject fxPrefab;
    [Header("Bob")]
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
        if (!other.CompareTag("Player")) return;
        if (other.TryGetComponent(out PlayerInventory inv))
            inv.AddItem(new InventoryItem { itemName = itemName, quantity = value });
        if (pickupSound) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        if (fxPrefab) Instantiate(fxPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}