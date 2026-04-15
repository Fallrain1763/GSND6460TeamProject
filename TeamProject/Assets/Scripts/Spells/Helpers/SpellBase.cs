using UnityEngine;

public abstract class SpellBase : ScriptableObject
{
    [Header("Basic Spell Info")]
    public string spellName = "New Spell";
    public Sprite icon;
    public Color tintColor = Color.white;
    public GameObject spellPrefab; // not needed for lob delivery spells

    [Header("Casting")]
    public float cooldown = 0f;

    public abstract void Cast(SpellContext context, SpellCaster caster);

    public virtual string GetTooltipDetails()
    {
        return "PLACEHOLDER TOOLTIP";
    }
}