using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("Set Up UI")]
    [SerializeField] Slider healthbar;
    [SerializeField] Image screenFlash;
    [SerializeField] GameObject healthUI;
    bool isDisplayingHealth = true;

    void Update()
    {
        if (isDisplayingHealth && ThirdPersonCamera.InputLocked)
        {
            isDisplayingHealth = false;
            healthUI.SetActive(false);
        }
        else if (!isDisplayingHealth && !ThirdPersonCamera.InputLocked)
        {
            isDisplayingHealth = true;
            healthUI.SetActive(true);
        }
    }

    public void UpdateUI(float newHealth)
    {
        healthbar.value = newHealth;
        StartCoroutine(FlashRed());
    }

    IEnumerator FlashRed()
    {
        Color currColor = screenFlash.color;
        currColor.a = 0.2f;
        screenFlash.color = currColor;
        yield return new WaitForSecondsRealtime(0.1f);
        currColor.a = 0;
        screenFlash.color = currColor;
    }
}
