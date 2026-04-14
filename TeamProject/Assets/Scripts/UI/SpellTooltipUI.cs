using TMPro;
using UnityEngine;

public class SpellTooltipUI : MonoBehaviour
{
    [SerializeField] RectTransform panel;
    [SerializeField] TMP_Text tooltipText;
    [SerializeField] Vector2 offset = new Vector2(18f, -18f);

    Canvas rootCanvas;

    void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();

        if (panel == null)
            panel = transform as RectTransform;

        Hide();
    }

    public void Show(SpellBase spell, Vector2 screenPosition)
    {
        if (spell == null || panel == null || tooltipText == null)
            return;

        tooltipText.text = BuildTooltipText(spell);
        panel.gameObject.SetActive(true);
        SetPosition(screenPosition);
    }

    public void UpdatePosition(Vector2 screenPosition)
    {
        if (panel == null || !panel.gameObject.activeSelf)
            return;

        SetPosition(screenPosition);
    }

    public void Hide()
    {
        if (panel != null)
            panel.gameObject.SetActive(false);
    }

    void SetPosition(Vector2 screenPosition)
    {
        Vector2 finalPos = screenPosition + offset;

        if (panel.parent is RectTransform parentRect)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                finalPos,
                rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null,
                out Vector2 localPoint
            );

            panel.anchoredPosition = localPoint;
        }
        else
        {
            panel.position = finalPos;
        }
    }

    string BuildTooltipText(SpellBase spell)
    {
        string extra = spell.GetTooltipDetails();

        if (string.IsNullOrWhiteSpace(extra))
            return $"<b>{spell.spellName}</b>\n<color=#AAAAAA>Cooldown: {spell.cooldown:0.##}s</color>";

        return $"<b>{spell.spellName}</b>\n<color=#AAAAAA>Cooldown: {spell.cooldown:0.##}s</color>\n{extra}";
    }
}