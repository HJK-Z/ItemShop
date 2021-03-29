using UnityEngine;

public class NotificationHUDManager : MonoBehaviour
{
    [Tooltip("UI panel containing the layoutGroup for displaying notifications")]
    public RectTransform notificationPanel;
    [Tooltip("Prefab for the notifications")]
    public GameObject notificationPrefab;


    void Awake()
    {
        // PlayerWeaponsManager playerWeaponsManager = FindObjectOfType<PlayerWeaponsManager>();
        // DebugUtility.HandleErrorIfNullFindObject<PlayerWeaponsManager, NotificationHUDManager>(playerWeaponsManager, this);
        // playerWeaponsManager.onAddedWeapon += OnPickupWeapon;
    }

    public void CreateNotification(string text)
    {
        GameObject notificationInstance = Instantiate(notificationPrefab, notificationPanel);
        notificationInstance.transform.SetSiblingIndex(0);

        NotificationToast toast = notificationInstance.GetComponent<NotificationToast>();
        if (toast)
        {
            toast.Initialize(text);
        }
    }
}
