using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpellInventoryUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject inventoryPanel;
    [SerializeField] Transform  inventoryGrid;
    [SerializeField] Transform  hotbarGrid;
    [SerializeField] Transform  combineSlotA;
    [SerializeField] Transform  combineSlotB;
    [SerializeField] Transform  resultSlot;
    [SerializeField] TMP_Text   forgeHintText;

    [Header("Prefabs")]
    [SerializeField] GameObject slotPrefab;
    [SerializeField] GameObject dragIconPrefab;

    [Header("Inventory")]
    [SerializeField] int             inventorySize = 27;
    [SerializeField] int             hotbarSize    = 5;
    [SerializeField] List<SpellData> startingSpells;

    [Header("Spell Combos")]
    [SerializeField] List<SpellCombo> combos;

    SpellData[] inventory;
    SpellData[] hotbar;
    SpellData   slotASpell;
    SpellData   slotBSpell;

    SpellData  draggedSpell;
    SlotSource dragSource;
    int        dragSourceIndex;
    GameObject dragIcon;
    bool       isDragging = false;

    // All registered slots for hit testing
    List<SlotRecord> allSlots = new List<SlotRecord>();

    bool isOpen = false;

    struct SlotRecord
    {
        public RectTransform rect;
        public SlotSource    source;
        public int           index;
    }

    void Awake()
    {
        inventory = new SpellData[inventorySize];
        hotbar    = new SpellData[hotbarSize];

        if (startingSpells != null)
            for (int i = 0; i < startingSpells.Count && i < inventorySize; i++)
                inventory[i] = startingSpells[i];

        inventoryPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            ToggleInventory();

        if (dragIcon != null)
            dragIcon.transform.position = Input.mousePosition;

        if (isDragging && Input.GetMouseButtonUp(0))
            FinishDrag();
    }

    void ToggleInventory()
    {
        isOpen = !isOpen;
        inventoryPanel.SetActive(isOpen);
        Time.timeScale              = isOpen ? 0f : 1f;
        Cursor.lockState            = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible              = isOpen;
        ThirdPersonCamera.InputLocked = isOpen;
        if (isOpen) Refresh();
    }

    void Refresh()
    {
        allSlots.Clear();
        BuildGrid(inventoryGrid, inventory, inventorySize, SlotSource.Inventory);
        BuildGrid(hotbarGrid,    hotbar,    hotbarSize,    SlotSource.Hotbar);
        RefreshForge();
    }

    void BuildGrid(Transform parent, SpellData[] data, int size, SlotSource source)
    {
        foreach (Transform child in parent)
            Destroy(child.gameObject);

        for (int i = 0; i < size; i++)
        {
            int idx       = i;
            GameObject go = Instantiate(slotPrefab, parent);
            go.GetComponent<SpellSlot>().Setup(data[idx], source, idx, this);
            allSlots.Add(new SlotRecord {
                rect   = go.GetComponent<RectTransform>(),
                source = source,
                index  = idx
            });
        }
    }

    void RefreshForge()
    {
        SetForgeSlot(combineSlotA, slotASpell, SlotSource.ForgeA,      0);
        SetForgeSlot(combineSlotB, slotBSpell, SlotSource.ForgeB,      0);

        SpellData result    = GetComboResult();
        bool      hasResult = result != null;

        SetForgeSlot(resultSlot, result, SlotSource.ForgeResult, 0);

        // Register forge slots for hit testing
        allSlots.Add(new SlotRecord { rect = combineSlotA.GetComponent<RectTransform>(), source = SlotSource.ForgeA,      index = 0 });
        allSlots.Add(new SlotRecord { rect = combineSlotB.GetComponent<RectTransform>(), source = SlotSource.ForgeB,      index = 0 });
        allSlots.Add(new SlotRecord { rect = resultSlot.GetComponent<RectTransform>(),   source = SlotSource.ForgeResult, index = 0 });

        if (forgeHintText == null) return;

        if (slotASpell != null && slotBSpell != null)
        {
            forgeHintText.text  = hasResult
                ? $"{result.displayName} - drag to inventory!"
                : "No known combination";
            forgeHintText.color = hasResult
                ? new Color(0.79f, 0.66f, 0.30f)
                : new Color(0.75f, 0.25f, 0.25f);
        }
        else
        {
            forgeHintText.text  = "Fill both slots to forge a new spell";
            forgeHintText.color = new Color(0.5f, 0.45f, 0.6f);
        }
    }

    void SetForgeSlot(Transform slotParent, SpellData spell, SlotSource source, int index)
    {
        SpellSlot slot = slotParent.GetComponent<SpellSlot>();
        if (slot == null)
            slot = slotParent.gameObject.AddComponent<SpellSlot>();

        Image img = slotParent.GetComponent<Image>();
        slot.SetupForge(spell, source, index, this, img);
    }

    SpellData GetComboResult()
    {
        if (slotASpell == null || slotBSpell == null) return null;
        foreach (SpellCombo combo in combos)
        {
            bool match = (combo.ingredientA.spellID == slotASpell.spellID &&
                          combo.ingredientB.spellID == slotBSpell.spellID)
                      || (combo.ingredientA.spellID == slotBSpell.spellID &&
                          combo.ingredientB.spellID == slotASpell.spellID);
            if (match) return combo.result;
        }
        return null;
    }

    // ── Drag ──────────────────────────────────────────────────────────────

    public void BeginDrag(SpellData spell, SlotSource source, int index)
    {
        if (spell == null) return;
        if (source == SlotSource.ForgeResult && GetComboResult() == null) return;

        draggedSpell    = spell;
        dragSource      = source;
        dragSourceIndex = index;
        isDragging      = true;

        if (dragIconPrefab != null)
        {
            dragIcon = Instantiate(dragIconPrefab, inventoryPanel.transform);
            dragIcon.GetComponent<Image>().sprite = spell.icon;
            dragIcon.GetComponent<Image>().color  = spell.tintColor;
            dragIcon.transform.SetAsLastSibling();
            dragIcon.transform.position = Input.mousePosition;
        }
    }

    void FinishDrag()
    {
        isDragging = false;
        if (dragIcon != null) { Destroy(dragIcon); dragIcon = null; }
        if (draggedSpell == null) return;

        // Find which slot the mouse is over by checking screen rects
        Canvas canvas = inventoryPanel.GetComponentInParent<Canvas>();
        Vector2 mousePos = Input.mousePosition;

        SlotRecord? hit = null;
        foreach (SlotRecord sr in allSlots)
        {
            if (sr.rect == null) continue;
            Vector3[] corners = new Vector3[4];
            sr.rect.GetWorldCorners(corners);
            // corners: 0=BL, 1=TL, 2=TR, 3=BR in screen space for Overlay canvas
            float minX = corners[0].x, maxX = corners[2].x;
            float minY = corners[0].y, maxY = corners[2].y;
            if (mousePos.x >= minX && mousePos.x <= maxX &&
                mousePos.y >= minY && mousePos.y <= maxY)
            {
                hit = sr;
                break;
            }
        }

        if (hit.HasValue)
        {
            Debug.Log($"Dropped on {hit.Value.source} index {hit.Value.index}");
            EndDrag(hit.Value.source, hit.Value.index);
        }
        else
        {
            draggedSpell = null;
            Refresh();
        }
    }

    void EndDrag(SlotSource targetSource, int targetIndex)
    {
        if (draggedSpell == null) return;
        bool isResultDrop = (dragSource == SlotSource.ForgeResult);

        switch (targetSource)
        {
            case SlotSource.Inventory:
                if (isResultDrop) { inventory[targetIndex] = draggedSpell; slotASpell = slotBSpell = null; }
                else SwapInventorySlot(targetIndex);
                break;
            case SlotSource.Hotbar:
                if (isResultDrop) { hotbar[targetIndex] = draggedSpell; slotASpell = slotBSpell = null; }
                else SwapHotbarSlot(targetIndex);
                break;
            case SlotSource.ForgeA:
                if (!isResultDrop) { slotASpell = draggedSpell; ClearDragSource(); }
                break;
            case SlotSource.ForgeB:
                if (!isResultDrop) { slotBSpell = draggedSpell; ClearDragSource(); }
                break;
        }

        draggedSpell = null;
        Refresh();
    }

    void SwapInventorySlot(int targetIndex)
    {
        SpellData displaced    = inventory[targetIndex];
        inventory[targetIndex] = draggedSpell;
        switch (dragSource)
        {
            case SlotSource.Inventory: inventory[dragSourceIndex] = displaced; break;
            case SlotSource.Hotbar:    hotbar[dragSourceIndex]    = displaced; break;
            case SlotSource.ForgeA:    slotASpell                 = displaced; break;
            case SlotSource.ForgeB:    slotBSpell                 = displaced; break;
        }
    }

    void SwapHotbarSlot(int targetIndex)
    {
        SpellData displaced = hotbar[targetIndex];
        hotbar[targetIndex] = draggedSpell;
        switch (dragSource)
        {
            case SlotSource.Inventory: inventory[dragSourceIndex] = displaced; break;
            case SlotSource.Hotbar:    hotbar[dragSourceIndex]    = displaced; break;
            case SlotSource.ForgeA:    slotASpell                 = displaced; break;
            case SlotSource.ForgeB:    slotBSpell                 = displaced; break;
        }
    }

    void ClearDragSource()
    {
        switch (dragSource)
        {
            case SlotSource.Inventory: inventory[dragSourceIndex] = null; break;
            case SlotSource.Hotbar:    hotbar[dragSourceIndex]    = null; break;
            case SlotSource.ForgeA:    slotASpell = null; break;
            case SlotSource.ForgeB:    slotBSpell = null; break;
        }
    }

    public SpellData GetHotbarSpell(int index) => hotbar[index];

    public bool TryAddSpell(SpellData spell)
    {
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i] == null)
            {
                inventory[i] = spell;
                if (isOpen) Refresh();
                return true;
            }
        }
        Debug.LogWarning("Inventory full!");
        return false;
    }
}