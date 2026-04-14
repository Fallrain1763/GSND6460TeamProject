using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Plasma Beam")]
public class PlasmaBeam : SpellBase
{
    [Header("Beam")]
    public float duration = 2.5f;
    public float tickInterval = 0.15f;
    public float length = 14f;
    public float radius = 0.9f;
    public LayerMask targetLayers = ~0;

    [Header("Damage")]
    public float damagePerTick = 10f;

    [Header("Channel Penalties")]
    [Range(0f, 1f)]
    public float movementMultiplier = 0.35f;

    [Range(0f, 1f)]
    public float lookSensitivityMultiplier = 0.4f;

    public override void Cast(SpellContext context, SpellCaster caster)
    {
        caster.RunSpellRoutine(ChannelBeam(caster));
    }

    public override string GetTooltipDetails()
    {
        return
            "<i>Channel a destructive beam of plasma</i>\n\n" +
            $"<b>Damage:</b> <color=#FF6B6B>{damagePerTick} x {duration/tickInterval}</color>\n" +
            $"<b>Length:</b> <color=#6BCBFF>{length}</color>";
    }

    IEnumerator ChannelBeam(SpellCaster caster)
    {
        if (caster == null)
            yield break;

        PlayerMovement movement = caster.GetComponent<PlayerMovement>();
        ThirdPersonCamera cameraController = null;

        if (caster.mainCam != null)
            cameraController = caster.mainCam.GetComponent<ThirdPersonCamera>();

        caster.SetChanneling(true);

        if (movement != null)
            movement.SetMoveSpeedMultiplier(movementMultiplier);

        if (cameraController != null)
            cameraController.SetLookSensitivityMultiplier(lookSensitivityMultiplier);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            SpellContext tickContext = new SpellContext
            {
                origin = caster.castPoint != null
                    ? caster.castPoint.position
                    : caster.transform.position + Vector3.up * 1.2f,
                aimPoint = caster.GetCurrentAimPoint(),
                aimDirection = caster.GetCurrentAimDirection()
            };

            Collider[] hits = SpellShape.Line(
                tickContext.origin,
                tickContext.aimDirection,
                length,
                radius,
                targetLayers
            );

            foreach (Collider hit in hits)
            {
                if (!hit.CompareTag("Enemy"))
                    continue;

                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy == null)
                    continue;

                enemy.TakeDamage(damagePerTick, false);
            }

            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }

        if (movement != null)
            movement.SetMoveSpeedMultiplier(1f);

        if (cameraController != null)
            cameraController.SetLookSensitivityMultiplier(1f);

        caster.SetChanneling(false);
    }
}