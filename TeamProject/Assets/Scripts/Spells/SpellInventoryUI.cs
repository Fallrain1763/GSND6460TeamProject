using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpellInventoryUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject inventoryPanel;
    [SerializeField] Transform inventoryGrid;
    [SerializeField] Transform hotbarGrid;
    [SerializeField] Transform combineSlotA;
    [SerializeField] Transform combineSlotB;
    [SerializeField] Transform resultSlot;
    [SerializeField] TMP_Text forgeHintText;

    [Header("Prefabs")]
    [SerializeField] GameObject slotPrefab;
    [SerializeField] GameObject dragIconPrefab;

    [Header("Inventory")]
    [SerializeField] int inventorySize = 27;
    [SerializeField] int hotbarSize = 3;
    [SerializeField] List<SpellBase> startingSpells;

    [Header("Spell Combos")]
    [SerializeField] List<SpellCombo> combos;

    [Header("Casting Integration")]
    [SerializeField] SpellCaster spellCaster;

    public event Action OnHotbarChanged;
    [SerializeField] SpellTooltipUI tooltipUI;

    SpellBase[] inventory;
    SpellBase[] hotbar;
    SpellBase slotASpell;
    SpellBase slotBSpell;

    SpellBase draggedSpell;
    SlotSource dragSource;
    int dragSourceIndex;
    GameObject dragIcon;
    bool isDragging = false;
    
    [Header("Highlight Colors")]
    [SerializeField] Color selectedHotbarColor = new Color(0.85f, 0.72f, 0.28f, 0.95f);
    [SerializeField] Color validForgeMatchColor = new Color(0.25f, 0.75f, 0.35f, 0.95f);
    [SerializeField] Color normalFilledColor = new Color(0.22f, 0.16f, 0.32f, 0.85f);
    [SerializeField] Color normalEmptyColor = new Color(0.16f, 0.12f, 0.24f, 0.85f);

    List<SlotRecord> allSlots = new List<SlotRecord>();

    bool isOpen = false;
    int selectedHotbarIndex = 0;

    struct SlotRecord
    {
        public RectTransform rect;
        public SlotSource source;
        public int index;
    }

    void Awake()
    {
        inventory = new SpellBase[inventorySize];
        hotbar = new SpellBase[hotbarSize];

        if (startingSpells != null)
        {
            for (int i = 0; i < startingSpells.Count && i < inventorySize; i++)
                inventory[i] = startingSpells[i];
        }

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        selectedHotbarIndex = Mathf.Clamp(selectedHotbarIndex, 0, hotbar.Length - 1);

        RefreshSelectedSpell();
        NotifyHotbarChanged();
    }

    void Update()
    {
        HandleHotbarSelectionInput();

        if (Input.GetKeyDown(KeyCode.Tab))
            ToggleInventory();

        if (dragIcon != null)
            dragIcon.transform.position = Input.mousePosition;

        if (isDragging && Input.GetMouseButtonUp(0))
            FinishDrag();
    }

    void HandleHotbarSelectionInput()
    {
        if (hotbar == null || hotbar.Length == 0)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            SelectHotbarSlot(0);

        if (Input.GetKeyDown(KeyCode.Alpha2) && hotbar.Length > 1)
            SelectHotbarSlot(1);

        if (Input.GetKeyDown(KeyCode.Alpha3) && hotbar.Length > 2)
            SelectHotbarSlot(2);
    }

    void ToggleInventory()
    {
        isOpen = !isOpen;

        inventoryPanel.SetActive(isOpen);
        Time.timeScale = isOpen ? 0f : 1f;
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpen;
        ThirdPersonCamera.InputLocked = isOpen;

        if (isOpen)
            Refresh();

        if (!isOpen)
            HideSpellTooltip();
    }

    void Refresh()
    {
        allSlots.Clear();

        BuildGrid(inventoryGrid, inventory, inventorySize, SlotSource.Inventory);
        BuildGrid(hotbarGrid, hotbar, hotbarSize, SlotSource.Hotbar);
        RefreshForge();
        RefreshSelectedSpell();
        NotifyHotbarChanged();
    }

    void BuildGrid(Transform parent, SpellBase[] data, int size, SlotSource source)
    {
        foreach (Transform child in parent)
            Destroy(child.gameObject);

        SpellBase forgeReferenceSpell = GetSingleForgeReferenceSpell();

        for (int i = 0; i < size; i++)
        {
            int idx = i;
            GameObject go = Instantiate(slotPrefab, parent);

            SpellSlot slot = go.GetComponent<SpellSlot>();
            slot.Setup(data[idx], source, idx, this);

            bool isSelectedHotbarSlot = source == SlotSource.Hotbar && idx == selectedHotbarIndex;
            bool isValidForgeMatch =
                forgeReferenceSpell != null &&
                data[idx] != null &&
                IsValidComboPair(forgeReferenceSpell, data[idx]);

            if (isSelectedHotbarSlot)
            {
                slot.SetHighlight(true, selectedHotbarColor, normalFilledColor, normalEmptyColor);
            }
            else if (isValidForgeMatch)
            {
                slot.SetHighlight(true, validForgeMatchColor, normalFilledColor, normalEmptyColor);
            }
            else
            {
                slot.SetHighlight(false, selectedHotbarColor, normalFilledColor, normalEmptyColor);
            }

            allSlots.Add(new SlotRecord
            {
                rect = go.GetComponent<RectTransform>(),
                source = source,
                index = idx
            });
        }
    }

    void RefreshForge()
    {
        SetForgeSlot(combineSlotA, slotASpell, SlotSource.ForgeA, 0);
        SetForgeSlot(combineSlotB, slotBSpell, SlotSource.ForgeB, 0);

        SpellBase result = GetComboResult();
        bool hasResult = result != null;

        SetForgeSlot(resultSlot, result, SlotSource.ForgeResult, 0);

        allSlots.Add(new SlotRecord
        {
            rect = combineSlotA.GetComponent<RectTransform>(),
            source = SlotSource.ForgeA,
            index = 0
        });

        allSlots.Add(new SlotRecord
        {
            rect = combineSlotB.GetComponent<RectTransform>(),
            source = SlotSource.ForgeB,
            index = 0
        });

        allSlots.Add(new SlotRecord
        {
            rect = resultSlot.GetComponent<RectTransform>(),
            source = SlotSource.ForgeResult,
            index = 0
        });

        if (forgeHintText == null)
            return;

        if (slotASpell != null && slotBSpell != null)
        {
            forgeHintText.text = hasResult
                ? $"{result.spellName} - drag to inventory!"
                : "No known combination";

            forgeHintText.color = hasResult
                ? new Color(0.79f, 0.66f, 0.30f)
                : new Color(0.75f, 0.25f, 0.25f);
        }
        else
        {
            forgeHintText.text = "Fill both slots to forge a new spell";
            forgeHintText.color = new Color(0.5f, 0.45f, 0.6f);
        }
    }

    void SetForgeSlot(Transform slotParent, SpellBase spell, SlotSource source, int index)
    {
        SpellSlot slot = slotParent.GetComponent<SpellSlot>();
        if (slot == null)
            slot = slotParent.gameObject.AddComponent<SpellSlot>();

        Image img = slotParent.GetComponent<Image>();
        slot.SetupForge(spell, source, index, this, img);
    }

    SpellBase GetComboResult()
    {
        if (slotASpell == null || slotBSpell == null)
            return null;

        foreach (SpellCombo combo in combos)
        {
            bool match =
                (combo.ingredientA == slotASpell && combo.ingredientB == slotBSpell) ||
                (combo.ingredientA == slotBSpell && combo.ingredientB == slotASpell);

            if (match)
                return combo.result;
        }

        return null;
    }

    SpellBase GetSingleForgeReferenceSpell()
    {
        bool hasA = slotASpell != null;
        bool hasB = slotBSpell != null;

        if (hasA && !hasB)
            return slotASpell;

        if (!hasA && hasB)
            return slotBSpell;

        return null;
    }

    bool IsValidComboPair(SpellBase a, SpellBase b)
    {
        if (a == null || b == null)
            return false;

        foreach (SpellCombo combo in combos)
        {
            bool match =
                (combo.ingredientA == a && combo.ingredientB == b) ||
                (combo.ingredientA == b && combo.ingredientB == a);

            if (match)
                return true;
        }

        return false;
    }

    public void BeginDrag(SpellBase spell, SlotSource source, int index)
    {
        if (spell == null)
            return;

        if (source == SlotSource.ForgeResult && GetComboResult() == null)
            return;

        draggedSpell = spell;
        dragSource = source;
        dragSourceIndex = index;
        isDragging = true;

        if (dragIconPrefab != null)
        {
            dragIcon = Instantiate(dragIconPrefab, inventoryPanel.transform);

            Image dragImage = dragIcon.GetComponent<Image>();
            if (dragImage != null)
            {
                dragImage.sprite = spell.icon;
                dragImage.color = spell.tintColor;
            }

            dragIcon.transform.SetAsLastSibling();
            dragIcon.transform.position = Input.mousePosition;
        }
    }

    void FinishDrag()
    {
        isDragging = false;

        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }

        if (draggedSpell == null)
            return;

        Vector2 mousePos = Input.mousePosition;
        SlotRecord? hit = null;

        foreach (SlotRecord sr in allSlots)
        {
            if (sr.rect == null)
                continue;

            Vector3[] corners = new Vector3[4];
            sr.rect.GetWorldCorners(corners);

            float minX = corners[0].x;
            float maxX = corners[2].x;
            float minY = corners[0].y;
            float maxY = corners[2].y;

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
        if (draggedSpell == null)
            return;

        bool isResultDrop = (dragSource == SlotSource.ForgeResult);

        switch (targetSource)
        {
            case SlotSource.Inventory:
                if (isResultDrop)
                {
                    inventory[targetIndex] = draggedSpell;
                    slotASpell = null;
                    slotBSpell = null;
                }
                else
                {
                    SwapInventorySlot(targetIndex);
                }
                break;

            case SlotSource.Hotbar:
                if (isResultDrop)
                {
                    hotbar[targetIndex] = draggedSpell;
                    slotASpell = null;
                    slotBSpell = null;
                }
                else
                {
                    SwapHotbarSlot(targetIndex);
                }
                break;

            case SlotSource.ForgeA:
                if (!isResultDrop)
                {
                    slotASpell = draggedSpell;
                    ClearDragSource();
                }
                break;

            case SlotSource.ForgeB:
                if (!isResultDrop)
                {
                    slotBSpell = draggedSpell;
                    ClearDragSource();
                }
                break;
        }

        draggedSpell = null;
        Refresh();
    }

    void SwapInventorySlot(int targetIndex)
    {
        SpellBase displaced = inventory[targetIndex];
        inventory[targetIndex] = draggedSpell;

        switch (dragSource)
        {
            case SlotSource.Inventory:
                inventory[dragSourceIndex] = displaced;
                break;
            case SlotSource.Hotbar:
                hotbar[dragSourceIndex] = displaced;
                break;
            case SlotSource.ForgeA:
                slotASpell = displaced;
                break;
            case SlotSource.ForgeB:
                slotBSpell = displaced;
                break;
        }
    }

    void SwapHotbarSlot(int targetIndex)
    {
        SpellBase displaced = hotbar[targetIndex];
        hotbar[targetIndex] = draggedSpell;

        switch (dragSource)
        {
            case SlotSource.Inventory:
                inventory[dragSourceIndex] = displaced;
                break;
            case SlotSource.Hotbar:
                hotbar[dragSourceIndex] = displaced;
                break;
            case SlotSource.ForgeA:
                slotASpell = displaced;
                break;
            case SlotSource.ForgeB:
                slotBSpell = displaced;
                break;
        }
    }

    void ClearDragSource()
    {
        switch (dragSource)
        {
            case SlotSource.Inventory:
                inventory[dragSourceIndex] = null;
                break;
            case SlotSource.Hotbar:
                hotbar[dragSourceIndex] = null;
                break;
            case SlotSource.ForgeA:
                slotASpell = null;
                break;
            case SlotSource.ForgeB:
                slotBSpell = null;
                break;
        }
    }

    void RefreshSelectedSpell()
    {
        if (hotbar == null || hotbar.Length == 0)
        {
            if (spellCaster != null)
                spellCaster.selectedSpell = null;
            return;
        }

        selectedHotbarIndex = Mathf.Clamp(selectedHotbarIndex, 0, hotbar.Length - 1);

        if (spellCaster != null)
            spellCaster.selectedSpell = hotbar[selectedHotbarIndex];
    }

    void NotifyHotbarChanged()
    {
        OnHotbarChanged?.Invoke();
    }

    public void SelectHotbarSlot(int index)
    {
        if (hotbar == null || hotbar.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, hotbar.Length - 1);

        if (selectedHotbarIndex == index)
            return;

        selectedHotbarIndex = index;
        RefreshSelectedSpell();
        NotifyHotbarChanged();

        if (isOpen)
            Refresh();
    }

    public int GetSelectedHotbarIndex()
    {
        return selectedHotbarIndex;
    }

    public int GetHotbarSize()
    {
        return hotbar != null ? hotbar.Length : 0;
    }

    public SpellBase GetHotbarSpell(int index)
    {
        if (hotbar == null || index < 0 || index >= hotbar.Length)
            return null;

        return hotbar[index];
    }

    public bool IsInventoryOpen()
    {
        return isOpen;
    }

    public bool TryAddSpell(SpellBase spell)
    {
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i] == null)
            {
                inventory[i] = spell;

                if (isOpen)
                    Refresh();
                else
                {
                    RefreshSelectedSpell();
                    NotifyHotbarChanged();
                }

                return true;
            }
        }

        Debug.LogWarning("Inventory full!");
        return false;
    }

    public void ShowSpellTooltip(SpellBase spell, Vector2 screenPosition)
    {
        if (!isOpen || tooltipUI == null || spell == null)
            return;

        tooltipUI.Show(spell, screenPosition);
    }

    public void UpdateSpellTooltipPosition(Vector2 screenPosition)
    {
        if (!isOpen || tooltipUI == null)
            return;

        tooltipUI.UpdatePosition(screenPosition);
    }

    public void HideSpellTooltip()
    {
        if (tooltipUI != null)
            tooltipUI.Hide();
    }
}