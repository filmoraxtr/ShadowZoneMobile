using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Vurulabilen her şey bu interface'i uygular.
/// EnemyHealth.cs bunu implement edecek.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal);
}

/// <summary>
/// Shadow Zone Mobile — Silah Sistemi
/// Ateş, fire rate, mermi, reload, recoil, raycast hasar, muzzle flash, ses.
/// Pistol ve Rifle aynı scripti kullanır, Inspector değerleri farklı olur.
/// </summary>
public class WeaponBase : MonoBehaviour
{
    [Header("Kimlik")]
    [SerializeField] private string weaponName = "Pistol";

    [Header("Atış")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float fireRate = 5f;
    [SerializeField] private float range = 100f;
    [SerializeField] private bool isAutomatic = false;
    [SerializeField, Range(0f, 5f)] private float spreadAngle = 0.4f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Mermi")]
    [SerializeField] private int magSize = 12;
    [SerializeField] private int maxReserveAmmo = 96;
    [SerializeField] private float reloadTime = 1.6f;

    [Header("Recoil")]
    [SerializeField] private float recoilPitch = 1.2f;
    [SerializeField] private float recoilYaw = 0.4f;
    [SerializeField] private float recoilSnappiness = 14f;
    [SerializeField] private float recoilRecovery = 7f;

    [Header("Efektler")]
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private GameObject impactPrefab;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioClip emptySound;

    public string WeaponName => weaponName;
    public int CurrentAmmo { get; private set; }
    public int ReserveAmmo { get; private set; }
    public bool IsReloading { get; private set; }

    public event Action<int, int> OnAmmoChanged;
    public event Action OnReloadStarted;
    public event Action OnReloadFinished;

    private Camera cam;
    private TouchCameraController camController;
    private float nextFireTime;
    private bool isFiring;
    private Coroutine reloadRoutine;

    private Vector2 recoilTarget;
    private Vector2 recoilCurrent;

    private void Awake()
    {
        cam = Camera.main;
        camController = cam != null ? cam.GetComponent<TouchCameraController>() : null;

        CurrentAmmo = magSize;
        ReserveAmmo = maxReserveAmmo;
    }

    private void OnEnable()
    {
        OnAmmoChanged?.Invoke(CurrentAmmo, ReserveAmmo);
    }

    private void OnDisable()
    {
        isFiring = false;

        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            reloadRoutine = null;
            IsReloading = false;
        }

        recoilTarget = Vector2.zero;
        recoilCurrent = Vector2.zero;

        if (camController != null)
        {
            camController.RecoilPitch = 0f;
            camController.RecoilYaw = 0f;
        }
    }

    private void Update()
    {
        if (isFiring && isAutomatic)
        {
            TryFireOnce();
        }

        UpdateRecoil();
    }

    public void StartFire()
    {
        isFiring = true;
        TryFireOnce();
    }

    public void StopFire()
    {
        isFiring = false;
    }

    public void Reload()
    {
        if (IsReloading) return;
        if (ReserveAmmo <= 0) return;
        if (CurrentAmmo >= magSize) return;

        reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    private void TryFireOnce()
    {
        if (IsReloading || Time.time < nextFireTime) return;

        if (CurrentAmmo <= 0)
        {
            PlaySound(emptySound);
            nextFireTime = Time.time + 0.3f;
            isFiring = false;
            return;
        }

        nextFireTime = Time.time + 1f / fireRate;

        CurrentAmmo--;
        OnAmmoChanged?.Invoke(CurrentAmmo, ReserveAmmo);

        FireRaycast();
        AddRecoil();

        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        PlaySound(fireSound);
    }

    private void FireRaycast()
    {
        if (cam == null) return;

        Vector3 dir = cam.transform.forward;

        if (spreadAngle > 0f)
        {
            float yaw = UnityEngine.Random.Range(-spreadAngle, spreadAngle);
            float pitch = UnityEngine.Random.Range(-spreadAngle, spreadAngle);

            dir = Quaternion.AngleAxis(yaw, cam.transform.up) *
                  Quaternion.AngleAxis(pitch, cam.transform.right) *
                  dir;
        }

        if (Physics.Raycast(
                cam.transform.position,
                dir,
                out RaycastHit hit,
                range,
                hitMask,
                QueryTriggerInteraction.Ignore))
        {
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();

            if (target != null)
            {
                float finalDamage = damage;

                if (hit.collider.CompareTag("Head"))
                {
                    finalDamage *= 2f;
                }

                target.TakeDamage(finalDamage, hit.point, hit.normal);
            }

            if (impactPrefab != null)
            {
                GameObject fx = Instantiate(
                    impactPrefab,
                    hit.point,
                    Quaternion.LookRotation(hit.normal)
                );

                Destroy(fx, 1.5f);
            }
        }
    }

    private void AddRecoil()
    {
        recoilTarget += new Vector2(
            -recoilPitch,
            UnityEngine.Random.Range(-recoilYaw, recoilYaw)
        );
    }

    private void UpdateRecoil()
    {
        if (camController == null) return;

        recoilTarget = Vector2.Lerp(
            recoilTarget,
            Vector2.zero,
            recoilRecovery * Time.deltaTime
        );

        recoilCurrent = Vector2.Lerp(
            recoilCurrent,
            recoilTarget,
            recoilSnappiness * Time.deltaTime
        );

        camController.RecoilPitch = recoilCurrent.x;
        camController.RecoilYaw = recoilCurrent.y;
    }

    private IEnumerator ReloadRoutine()
    {
        IsReloading = true;
        isFiring = false;

        OnReloadStarted?.Invoke();
        PlaySound(reloadSound);

        yield return new WaitForSeconds(reloadTime);

        int needed = magSize - CurrentAmmo;
        int toLoad = Mathf.Min(needed, ReserveAmmo);

        CurrentAmmo += toLoad;
        ReserveAmmo -= toLoad;

        IsReloading = false;
        reloadRoutine = null;

        OnReloadFinished?.Invoke();
        OnAmmoChanged?.Invoke(CurrentAmmo, ReserveAmmo);
    }

    public void AddReserveAmmo(int amount)
    {
        ReserveAmmo = Mathf.Min(ReserveAmmo + amount, maxReserveAmmo);
        OnAmmoChanged?.Invoke(CurrentAmmo, ReserveAmmo);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
