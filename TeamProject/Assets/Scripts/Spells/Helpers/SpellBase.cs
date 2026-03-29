using UnityEngine;

public abstract class SpellBase : ScriptableObject
{
    [Header("Basic Spell Info")]
    public string spellName = "New Spell";

    [Header("Casting")]
    public float cooldown = 0f;

    public abstract void Cast(SpellContext context, SpellCaster caster);
}