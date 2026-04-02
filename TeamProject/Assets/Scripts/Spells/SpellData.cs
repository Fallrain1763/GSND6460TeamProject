using UnityEngine;

[CreateAssetMenu(menuName = "Spells/SpellData")]
public class SpellData : ScriptableObject
{
    public string spellID;
    public string displayName;
    public Sprite icon;
    public Color  tintColor = Color.white;
}
