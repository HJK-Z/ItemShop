using UnityEngine;
using UnityEngine.Events;

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
    public ProjectileBase projectilePrefab;

    public float delayBetweenUses = 0.5f;

    public float bulletSpreadAngle = 0f;

    public int bulletsPerShot = 1;

    [Range(0f, 2f)]
    public float recoilForce = 1;

    [Header("Audio & Visual")]
    public Animator weaponAnimator;

    public GameObject muzzleFlashPrefab;

    public bool unparentMuzzleFlash;

    public AudioClip shootSFX;

    public bool useContinuousShootSound = false;

    public AudioClip changeWeaponSFX;

    public AudioClip continuousShootStartSFX;

    public AudioClip continuousShootLoopSFX;

    public AudioClip continuousShootEndSFX;

    private AudioSource m_continuousShootAudioSource = null;

    private bool m_wantsToShoot = false;

    public UnityAction onUse;

    float m_LastTimeUse = Mathf.NegativeInfinity;

    Vector3 m_LastMuzzlePosition;

    public GameObject owner { get; set; }

    public GameObject sourcePrefab { get; set; }

    public bool isWeaponActive { get; private set; }

    public Vector3 muzzleWorldVelocity { get; private set; }

    AudioSource m_ShootAudioSource;

    const string k_AnimAttackParameter = "Attack";

    void Awake()
    {
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

    public void Show(bool show)
    {
        weaponRoot.SetActive (show);

        if (show && changeWeaponSFX)
        {
            m_ShootAudioSource.PlayOneShot (changeWeaponSFX);
        }

        isWeaponActive = show;
    }

    public bool HandleUseInputs(bool inputDown, bool inputHeld, bool inputUp)
    {
        m_wantsToShoot = inputDown || inputHeld;
        switch (item.itemType)
        {
            case ItemType.Weapon:
                if (inputDown)
                {
                    return TryShoot();
                }
                break;
            case ItemType.Consumable:
                if (inputDown)
                {
                    return TryConsume();
                }
                break;
            default:
                break;
        }

        return false;
    }

    bool TryShoot()
    {
        if (m_LastTimeUse + delayBetweenUses < Time.time)
        {
            HandleShoot();

            return true;
        }

        return false;
    }

    void HandleShoot()
    {
        // spawn all bullets with random direction
        for (int i = 0; i < bulletsPerShot; i++)
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

        m_LastTimeUse = Time.time;

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
        if (onUse != null)
        {
            onUse();
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

    bool TryConsume()
    {
        if (m_LastTimeUse + delayBetweenUses < Time.time)
        {
            HandleConsume();

            return true;
        }

        return false;
    }

    void HandleConsume()
    {
        m_LastTimeUse = Time.time;

        owner.GetComponent<PlayerInventory>().OnConsumeItem(item);

        // Callback on use
        if (onUse != null)
        {
            onUse();
        }
    }
}
