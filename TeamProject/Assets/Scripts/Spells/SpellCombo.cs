using System;
using UnityEngine;

public enum SlotSource
{
    Inventory,
    Hotbar,
    ForgeA,
    ForgeB,
    ForgeResult
}

[Serializable]
public class SpellCombo
{
    public SpellData ingredientA;
    public SpellData ingredientB;
    public SpellData result;
}
