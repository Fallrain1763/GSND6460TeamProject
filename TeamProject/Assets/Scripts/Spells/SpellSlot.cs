using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Image))]
public class SpellSlot : MonoBehaviour, IPointerDownHandler
{
    SpellBase spell;
    SlotSource source;
    int index;
    SpellInventoryUI manager;

    [SerializeField] public Image iconImage;
    [SerializeField] public Image bgImage;
    [SerializeField] public TMP_Text label;

    static readonly Color EmptyBg = new Color(0.16f, 0.12f, 0.24f, 0.85f);
    static readonly Color FilledBg = new Color(0.22f, 0.16f, 0.32f, 0.85f);

    public void Setup(SpellBase data, SlotSource src, int idx, SpellInventoryUI mgr)
    {
        spell = data;
        source = src;
        index = idx;
        manager = mgr;

        bool hasSome = spell != null;

        if (bgImage != null)
            bgImage.color = hasSome ? FilledBg : EmptyBg;

        if (iconImage != null)
        {
            iconImage.enabled = hasSome;

            if (hasSome)
            {
                iconImage.sprite = spell.icon;
                iconImage.color = spell.tintColor;
            }
        }

        if (label != null)
        {
            if (source == SlotSource.Hotbar)
                label.text = (index + 1).ToString();
            else
                label.text = hasSome ? spell.spellName : "";
        }
    }

    public void SetupForge(SpellBase data, SlotSource src, int idx, SpellInventoryUI mgr, Image slotImage)
    {
        spell = data;
        source = src;
        index = idx;
        manager = mgr;
        bgImage = slotImage;

        if (slotImage != null)
        {
            if (data != null)
            {
                slotImage.sprite = data.icon;
                slotImage.color = Color.white;
            }
            else
            {
                slotImage.sprite = null;
                slotImage.color = EmptyBg;
            }
        }
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (spell == null)
            return;

        if (iconImage != null)
            iconImage.enabled = false;

        if (label != null)
            label.text = "";

        manager.BeginDrag(spell, source, index);
    }

    public SlotSource GetSource() => source;
    public int GetIndex() => index;
}