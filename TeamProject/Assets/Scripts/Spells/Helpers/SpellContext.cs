using UnityEngine;

[System.Serializable]
public class SpellContext
{
    //where the spell starts
    public Vector3 origin;
    //angle from origin to aimPoint
    public Vector3 aimDirection;
    //where the spell is aiming
    public Vector3 aimPoint;
}