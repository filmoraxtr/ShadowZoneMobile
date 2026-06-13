using System;
using UnityEngine;

/// <summary>
/// Shadow Zone Mobile — Silah Yöneticisi
/// Pistol/Rifle değişimi + UI butonlarının bağlandığı tek merkez.
/// Sadece aktif silah enabled olur; diğeri kapalı durur.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("Silahlar 0 = Pistol, 1 = Rifle")]
    [SerializeField] private WeaponBase[] weapons;

    [Header("Başlangıç")]
    [SerializeField] private int startWeaponIndex = 0;

    public WeaponBase CurrentWeapon { get; private set; }

    public event Action<WeaponBase> OnWeaponChanged;

    private int currentIndex = -1;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Start()
    {
        Equip(startWeaponIndex);
    }

    public void Equip(int index)
    {
        if (weapons == null || weapons.Length == 0) return;
        if (index < 0 || index >= weapons.Length) return;
        if (index == currentIndex) return;

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                weapons[i].gameObject.SetActive(i == index);
            }
        }

        currentIndex = index;
        CurrentWeapon = weapons[index];

        OnWeaponChanged?.Invoke(CurrentWeapon);
    }

    public void UI_SwitchWeapon()
    {
        if (weapons == null || weapons.Length == 0) return;

        Equip((currentIndex + 1) % weapons.Length);
    }

    public void UI_FireDown()
    {
        if (IsBlocked()) return;

        CurrentWeapon.StartFire();
    }

    public void UI_FireUp()
    {
        if (CurrentWeapon == null) return;

        CurrentWeapon.StopFire();
    }

    public void UI_Reload()
    {
        if (IsBlocked()) return;

        CurrentWeapon.Reload();
    }

    private bool IsBlocked()
    {
        if (CurrentWeapon == null) return true;
        if (playerHealth != null && playerHealth.IsDead) return true;

        return false;
    }
}
