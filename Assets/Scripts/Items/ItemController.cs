using UnityEngine;
using UnityEngine.Events;

public enum WeaponShootType
{
    Manual,
    Automatic,
    Charge
}

[System.Serializable]
public struct CrosshairData
{
    [Tooltip("The image that will be used for this weapon's crosshair")]
    public Sprite crosshairSprite;

    [Tooltip("The size of the crosshair image")]
    public int crosshairSize;

    [Tooltip("The color of the crosshair image")]
    public Color crosshairColor;
}

[RequireComponent(typeof (AudioSource))]
public class ItemController : MonoBehaviour
{
    //weapons
    [Header("Information")]
    public Item item;

    public CrosshairData crosshairDataDefault;

    public CrosshairData crosshairDataTargetInSight;

    public GameObject weaponRoot;

    public Transform weaponMuzzle;

    public SpriteRenderer image;

    [Header("Shoot Parameters")]
    public WeaponShootType shootType;

    public ProjectileBase projectilePrefab;

    public float delayBetweenShots = 0.5f;

    public float bulletSpreadAngle = 0f;

    public int bulletsPerShot = 1;

    [Range(0f, 2f)]
    public float recoilForce = 1;

    [Header("Ammo Parameters")]
    public float ammoReloadRate = 1f;

    public float ammoReloadDelay = 2f;

    public float maxAmmo = 8;

    [Header("Charging parameters (charging weapons only)")]
    public bool automaticReleaseOnCharged;

    public float maxChargeDuration = 2f;

    public float ammoUsedOnStartCharge = 1f;

    public float ammoUsageRateWhileCharging = 1f;

    [Header("Audio & Visual")]
    public Animator weaponAnimator;

    public GameObject muzzleFlashPrefab;

    public bool unparentMuzzleFlash;

    public AudioClip shootSFX;

    public AudioClip changeWeaponSFX;

    public bool useContinuousShootSound = false;

    public AudioClip continuousShootStartSFX;

    public AudioClip continuousShootLoopSFX;

    public AudioClip continuousShootEndSFX;

    private AudioSource m_continuousShootAudioSource = null;

    private bool m_wantsToShoot = false;

    public UnityAction onShoot;

    float m_CurrentAmmo;

    float m_LastTimeShot = Mathf.NegativeInfinity;

    public float LastChargeTriggerTimestamp { get; private set; }

    Vector3 m_LastMuzzlePosition;

    public GameObject owner { get; set; }

    public GameObject sourcePrefab { get; set; }

    public bool isCharging { get; private set; }

    public float currentAmmoRatio { get; private set; }

    public bool isWeaponActive { get; private set; }

    public bool isCooling { get; private set; }

    public float currentCharge { get; private set; }

    public Vector3 muzzleWorldVelocity { get; private set; }

    public float GetAmmoNeededToShoot() =>
        (
        shootType != WeaponShootType.Charge
            ? 1f
            : Mathf.Max(1f, ammoUsedOnStartCharge)
        ) /
        (maxAmmo * bulletsPerShot);

    AudioSource m_ShootAudioSource;

    const string k_AnimAttackParameter = "Attack";

    void Awake()
    {
        m_CurrentAmmo = maxAmmo;
        m_LastMuzzlePosition = weaponMuzzle.position;

        m_ShootAudioSource = GetComponent<AudioSource>();
        DebugUtility
            .HandleErrorIfNullGetComponent
            <AudioSource, ItemController>(m_ShootAudioSource, this, gameObject);

        if (useContinuousShootSound)
        {
            m_continuousShootAudioSource =
                gameObject.AddComponent<AudioSource>();
            m_continuousShootAudioSource.playOnAwake = false;
            m_continuousShootAudioSource.clip = continuousShootLoopSFX;
            m_continuousShootAudioSource.outputAudioMixerGroup =
                AudioUtility
                    .GetAudioGroup(AudioUtility.AudioGroups.WeaponShoot);
            m_continuousShootAudioSource.loop = true;
        }
    }

    void Update()
    {
        UpdateAmmo();
        UpdateCharge();
        UpdateContinuousShootSound();

        if (Time.deltaTime > 0)
        {
            muzzleWorldVelocity =
                (weaponMuzzle.position - m_LastMuzzlePosition) / Time.deltaTime;
            m_LastMuzzlePosition = weaponMuzzle.position;
        }
    }

    public void UpdateItem(Item newItem)
    {
        item = newItem;

        image.sprite = item.itemIcon;
    }

    void UpdateAmmo()
    {
        if (
            m_LastTimeShot + ammoReloadDelay < Time.time &&
            m_CurrentAmmo < maxAmmo &&
            !isCharging
        )
        {
            // reloads weapon over time
            m_CurrentAmmo += ammoReloadRate * Time.deltaTime;

            // limits ammo to max value
            m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo, 0, maxAmmo);

            isCooling = true;
        }
        else
        {
            isCooling = false;
        }

        if (maxAmmo == Mathf.Infinity)
        {
            currentAmmoRatio = 1f;
        }
        else
        {
            currentAmmoRatio = m_CurrentAmmo / maxAmmo;
        }
    }

    void UpdateCharge()
    {
        if (isCharging)
        {
            if (currentCharge < 1f)
            {
                float chargeLeft = 1f - currentCharge;

                // Calculate how much charge ratio to add this frame
                float chargeAdded = 0f;
                if (maxChargeDuration <= 0f)
                {
                    chargeAdded = chargeLeft;
                }
                else
                {
                    chargeAdded = (1f / maxChargeDuration) * Time.deltaTime;
                }

                chargeAdded = Mathf.Clamp(chargeAdded, 0f, chargeLeft);

                // See if we can actually add this charge
                float ammoThisChargeWouldRequire =
                    chargeAdded * ammoUsageRateWhileCharging;
                if (ammoThisChargeWouldRequire <= m_CurrentAmmo)
                {
                    // Use ammo based on charge added
                    UseAmmo (ammoThisChargeWouldRequire);

                    // set current charge ratio
                    currentCharge = Mathf.Clamp01(currentCharge + chargeAdded);
                }
            }
        }
    }

    private void UpdateContinuousShootSound()
    {
        if (useContinuousShootSound)
        {
            if (m_wantsToShoot && m_CurrentAmmo >= 1f)
            {
                if (!m_continuousShootAudioSource.isPlaying)
                {
                    m_ShootAudioSource.PlayOneShot (shootSFX);
                    m_ShootAudioSource.PlayOneShot (continuousShootStartSFX);
                    m_continuousShootAudioSource.Play();
                }
            }
            else if (m_continuousShootAudioSource.isPlaying)
            {
                m_ShootAudioSource.PlayOneShot (continuousShootEndSFX);
                m_continuousShootAudioSource.Stop();
            }
        }
    }

    public void Show(bool show)
    {
        weaponRoot.SetActive (show);

        if (show && changeWeaponSFX)
        {
            m_ShootAudioSource.PlayOneShot (changeWeaponSFX);
        }

        isWeaponActive = show;
    }

    public void UseAmmo(float amount)
    {
        m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo - amount, 0f, maxAmmo);
        m_LastTimeShot = Time.time;
    }

    public bool HandleUseInputs(bool inputDown, bool inputHeld, bool inputUp)
    {
        m_wantsToShoot = inputDown || inputHeld;
        switch (shootType)
        {
            case WeaponShootType.Manual:
                if (inputDown)
                {
                    return TryShoot();
                }
                return false;
            case WeaponShootType.Automatic:
                if (inputHeld)
                {
                    return TryShoot();
                }
                return false;
            case WeaponShootType.Charge:
                if (inputHeld)
                {
                    TryBeginCharge();
                }

                // Check if we released charge or if the weapon shoot autmatically when it's fully charged
                if (
                    inputUp ||
                    (automaticReleaseOnCharged && currentCharge >= 1f)
                )
                {
                    return TryReleaseCharge();
                }
                return false;
            default:
                return false;
        }
    }

    bool TryShoot()
    {
        if (
            m_CurrentAmmo >= 1f &&
            m_LastTimeShot + delayBetweenShots < Time.time
        )
        {
            HandleShoot();
            m_CurrentAmmo -= 1f;

            return true;
        }

        return false;
    }

    bool TryBeginCharge()
    {
        if (
            !isCharging &&
            m_CurrentAmmo >= ammoUsedOnStartCharge &&
            Mathf
                .FloorToInt((m_CurrentAmmo - ammoUsedOnStartCharge) *
                bulletsPerShot) >
            0 &&
            m_LastTimeShot + delayBetweenShots < Time.time
        )
        {
            UseAmmo (ammoUsedOnStartCharge);

            LastChargeTriggerTimestamp = Time.time;
            isCharging = true;

            return true;
        }

        return false;
    }

    bool TryReleaseCharge()
    {
        if (isCharging)
        {
            HandleShoot();

            currentCharge = 0f;
            isCharging = false;

            return true;
        }
        return false;
    }

    void HandleShoot()
    {
        int bulletsPerShotFinal =
            shootType == WeaponShootType.Charge
                ? Mathf.CeilToInt(currentCharge * bulletsPerShot)
                : bulletsPerShot;

        // spawn all bullets with random direction
        for (int i = 0; i < bulletsPerShotFinal; i++)
        {
            Vector3 shotDirection = GetShotDirectionWithinSpread(weaponMuzzle);
            ProjectileBase newProjectile =
                Instantiate(projectilePrefab,
                weaponMuzzle.position,
                Quaternion.LookRotation(shotDirection));
            newProjectile.Shoot(this);
        }

        // muzzle flash
        if (muzzleFlashPrefab != null)
        {
            GameObject muzzleFlashInstance =
                Instantiate(muzzleFlashPrefab,
                weaponMuzzle.position,
                weaponMuzzle.rotation,
                weaponMuzzle.transform);

            // Unparent the muzzleFlashInstance
            if (unparentMuzzleFlash)
            {
                muzzleFlashInstance.transform.SetParent(null);
            }

            Destroy(muzzleFlashInstance, 2f);
        }

        m_LastTimeShot = Time.time;

        // play shoot SFX
        if (shootSFX && !useContinuousShootSound)
        {
            m_ShootAudioSource.PlayOneShot (shootSFX);
        }

        // Trigger attack animation if there is any
        if (weaponAnimator)
        {
            weaponAnimator.SetTrigger (k_AnimAttackParameter);
        }

        // Callback on shoot
        if (onShoot != null)
        {
            onShoot();
        }
    }

    public Vector3 GetShotDirectionWithinSpread(Transform shootTransform)
    {
        float spreadAngleRatio = bulletSpreadAngle / 180f;
        Vector3 spreadWorldDirection =
            Vector3
                .Slerp(shootTransform.forward,
                UnityEngine.Random.insideUnitSphere,
                spreadAngleRatio);

        return spreadWorldDirection;
    }
}
