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
    public SpellBase ingredientA;
    public SpellBase ingredientB;
    public SpellBase result;
}