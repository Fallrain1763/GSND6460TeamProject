using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpellCaster : MonoBehaviour
{
    [Header("References")]
    public Camera mainCam;
    public Transform castPoint;

    [Header("Spell Inputs")]
    public SpellBase selectedSpell;

    [Header("Aiming")]
    public float maxAimDistance = 50f;
    public LayerMask aimLayers = ~0;

    [Header("UI")]
    public Image gloveImage;
    public Sprite closedGlove;
    public Sprite openGlove;
    public float animationTime = 0.5f;

    Dictionary<SpellBase, float> lastCastTimes = new Dictionary<SpellBase, float>();

    bool isChanneling = false;
    public bool IsChanneling => isChanneling;

    void Start()
    {
        if (mainCam == null)
            mainCam = Camera.main;
    }

    void Update()
    {
        if (isChanneling)
            return;

        if (Input.GetMouseButtonDown(1))
        {
            TryCastSpell(selectedSpell);
        }
    }

    void TryCastSpell(SpellBase spell)
    {
        if (spell == null || mainCam == null)
            return;

        if (!CanCast(spell))
            return;
        Debug.Log("HERE");
        StartCoroutine(HandCastAnimation());
        SpellContext context = BuildSpellContext();
        spell.Cast(context, this);
        lastCastTimes[spell] = Time.time;
    }

    bool CanCast(SpellBase spell)
    {
        if (spell == null)
            return false;

        if (!lastCastTimes.TryGetValue(spell, out float lastCastTime))
            return true;

        return Time.time >= lastCastTime + spell.cooldown;
    }

    SpellContext BuildSpellContext()
    {
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 aimPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimLayers))
            aimPoint = hit.point;
        else
            aimPoint = ray.origin + ray.direction * 25f;

        Vector3 origin = castPoint != null
            ? castPoint.position
            : transform.position + Vector3.up * 1.2f;

        Vector3 aimDirection = (aimPoint - origin).normalized;

        SpellContext context = new SpellContext();
        context.origin = origin;
        context.aimPoint = aimPoint;
        context.aimDirection = aimDirection;

        return context;
    }

    public Vector3 GetCurrentAimDirection()
    {
        return BuildSpellContext().aimDirection;
    }

    public Vector3 GetCurrentAimPoint()
    {
        return BuildSpellContext().aimPoint;
    }

    public Coroutine RunSpellRoutine(IEnumerator routine)
    {
        return StartCoroutine(routine);
    }

    public void SetChanneling(bool value)
    {
        isChanneling = value;
    }

    public float GetCooldownRemaining(SpellBase spell)
    {
        if (spell == null)
            return 0f;

        if (spell.cooldown <= 0f)
            return 0f;

        if (!lastCastTimes.TryGetValue(spell, out float lastCastTime))
            return 0f;

        float endTime = lastCastTime + spell.cooldown;
        return Mathf.Max(0f, endTime - Time.time);
    }

    public float GetCooldownNormalized(SpellBase spell)
    {
        if (spell == null || spell.cooldown <= 0f)
            return 0f;

        float remaining = GetCooldownRemaining(spell);
        return remaining / spell.cooldown;
    }

    public bool IsOnCooldown(SpellBase spell)
    {
        return GetCooldownRemaining(spell) > 0f;
    }

    IEnumerator HandCastAnimation()
    {
        gloveImage.sprite = openGlove;
        yield return new WaitForSecondsRealtime(animationTime);
        gloveImage.sprite = closedGlove;
    }
}