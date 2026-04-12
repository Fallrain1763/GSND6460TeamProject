using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpellHotbarHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] SpellInventoryUI inventoryUI;
    [SerializeField] SpellCaster spellCaster;
    [SerializeField] Transform slotParent;
    [SerializeField] GameObject slotPrefab;

    [Header("Colors")]
    [SerializeField] Color selectedColor = new Color(0.90f, 0.75f, 0.28f, 1f);
    [SerializeField] Color normalFilledColor = new Color(0.22f, 0.16f, 0.32f, 0.85f);
    [SerializeField] Color emptyColor = new Color(0.16f, 0.12f, 0.24f, 0.85f);

    readonly List<SpellSlot> spawnedSlots = new List<SpellSlot>();

    void Start()
    {
        if (inventoryUI == null)
        {
            Debug.LogError("SpellHotbarHUD: no SpellInventoryUI assigned.");
            return;
        }

        BuildSlots();
        inventoryUI.OnHotbarChanged += RefreshHUD;
        RefreshHUD();
    }

    void Update()
    {
        RefreshCooldowns();
    }

    void OnDestroy()
    {
        if (inventoryUI != null)
            inventoryUI.OnHotbarChanged -= RefreshHUD;
    }

    void BuildSlots()
    {
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        spawnedSlots.Clear();

        int count = inventoryUI.GetHotbarSize();

        for (int i = 0; i < count; i++)
        {
            GameObject go = Instantiate(slotPrefab, slotParent);
            SpellSlot slot = go.GetComponent<SpellSlot>();

            if (slot == null)
            {
                Debug.LogError("SpellHotbarHUD: slotPrefab is missing SpellSlot.");
                continue;
            }

            DisableRaycastTargets(go);
            spawnedSlots.Add(slot);
        }
    }

    void RefreshHUD()
    {
        if (inventoryUI == null)
            return;

        int selectedIndex = inventoryUI.GetSelectedHotbarIndex();

        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            SpellBase spell = inventoryUI.GetHotbarSpell(i);
            SpellSlot slot = spawnedSlots[i];

            slot.Setup(spell, SlotSource.Hotbar, i, inventoryUI);
            slot.SetHighlight(i == selectedIndex, selectedColor, normalFilledColor, emptyColor);
        }

        RefreshCooldowns();
    }

    void RefreshCooldowns()
    {
        if (inventoryUI == null || spellCaster == null)
            return;

        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            SpellBase spell = inventoryUI.GetHotbarSpell(i);
            float normalized = spellCaster.GetCooldownNormalized(spell);
            spawnedSlots[i].SetCooldownOverlay(normalized);
        }
    }

    void DisableRaycastTargets(GameObject root)
    {
        Image[] images = root.GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
            img.raycastTarget = false;

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text txt in texts)
            txt.raycastTarget = false;
    }
}