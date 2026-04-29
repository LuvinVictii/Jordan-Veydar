using System.Collections.Generic;
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
        if (items.Count >= maxSlots) { Debug.Log("Inventory full!"); return false; }
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
}