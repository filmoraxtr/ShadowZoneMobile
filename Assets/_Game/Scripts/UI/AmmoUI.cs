using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shadow Zone Mobile — Mermi ve Silah UI
/// WeaponManager ve WeaponBase eventlerini dinler.
/// Canvas altında AmmoUI objesine eklenir.
/// </summary>
public class AmmoUI : MonoBehaviour
{
    [Header("Referanslar")]
    [SerializeField] private WeaponManager weaponManager;

    [Header("Mermi Metni")]
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private TMP_Text weaponNameText;

    [Header("Reload Göstergesi")]
    [SerializeField] private GameObject reloadingPanel;
    [SerializeField] private Slider reloadProgressBar;

    [Header("Mermi Renk Uyarısı")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lowAmmoColor = new Color(1f, 0.4f, 0.1f, 1f);
    [SerializeField] private Color emptyColor = Color.red;
    [SerializeField] private int lowAmmoCount = 4;

    [Header("Reload UI Süresi")]
    [SerializeField] private float reloadUIDuration = 1.6f;

    [Header("Animasyon")]
    [SerializeField] private float punchScale = 1.25f;
    [SerializeField] private float punchDuration = 0.08f;

    private WeaponBase boundWeapon;
    private Coroutine punchCoroutine;
    private Coroutine reloadBarCoroutine;

    private void OnEnable()
    {
        if (weaponManager == null) return;

        weaponManager.OnWeaponChanged += HandleWeaponChanged;

        if (weaponManager.CurrentWeapon != null)
        {
            HandleWeaponChanged(weaponManager.CurrentWeapon);
        }
    }

    private void OnDisable()
    {
        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged -= HandleWeaponChanged;
        }

        UnbindWeapon();
    }

    private void HandleWeaponChanged(WeaponBase weapon)
    {
        UnbindWeapon();

        boundWeapon = weapon;

        if (boundWeapon == null) return;

        boundWeapon.OnAmmoChanged += HandleAmmoChanged;
        boundWeapon.OnReloadStarted += HandleReloadStarted;
        boundWeapon.OnReloadFinished += HandleReloadFinished;

        if (weaponNameText != null)
        {
            weaponNameText.text = boundWeapon.WeaponName.ToUpper();
        }

        HandleAmmoChanged(boundWeapon.CurrentAmmo, boundWeapon.ReserveAmmo);

        SetReloadVisible(false);
    }

    private void UnbindWeapon()
    {
        if (boundWeapon == null) return;

        boundWeapon.OnAmmoChanged -= HandleAmmoChanged;
        boundWeapon.OnReloadStarted -= HandleReloadStarted;
        boundWeapon.OnReloadFinished -= HandleReloadFinished;

        boundWeapon = null;
    }

    private void HandleAmmoChanged(int mag, int reserve)
    {
        if (ammoText != null)
        {
            ammoText.text = mag + " / " + reserve;

            if (mag == 0)
            {
                ammoText.color = emptyColor;
            }
            else if (mag <= lowAmmoCount)
            {
                ammoText.color = lowAmmoColor;
            }
            else
            {
                ammoText.color = normalColor;
            }

            if (punchCoroutine != null)
            {
                StopCoroutine(punchCoroutine);
            }

            punchCoroutine = StartCoroutine(PunchScale(ammoText.transform));
        }
    }

    private IEnumerator PunchScale(Transform target)
    {
        if (target == null) yield break;

        target.localScale = Vector3.one * punchScale;

        float elapsed = 0f;

        while (elapsed < punchDuration)
        {
            target.localScale = Vector3.Lerp(
                Vector3.one * punchScale,
                Vector3.one,
                elapsed / punchDuration
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    private void HandleReloadStarted()
    {
        SetReloadVisible(true);

        if (reloadProgressBar != null)
        {
            if (reloadBarCoroutine != null)
            {
                StopCoroutine(reloadBarCoroutine);
            }

            reloadBarCoroutine = StartCoroutine(FillReloadBar(reloadUIDuration));
        }
    }

    private void HandleReloadFinished()
    {
        SetReloadVisible(false);

        if (reloadBarCoroutine != null)
        {
            StopCoroutine(reloadBarCoroutine);
            reloadBarCoroutine = null;
        }

        if (reloadProgressBar != null)
        {
            reloadProgressBar.value = 0f;
        }
    }

    private IEnumerator FillReloadBar(float duration)
    {
        if (reloadProgressBar == null) yield break;

        reloadProgressBar.value = 0f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            reloadProgressBar.value = elapsed / duration;
            elapsed += Time.deltaTime;
            yield return null;
        }

        reloadProgressBar.value = 1f;
    }

    private void SetReloadVisible(bool visible)
    {
        if (reloadingPanel != null)
        {
            reloadingPanel.SetActive(visible);
        }
    }
}
