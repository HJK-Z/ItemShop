using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowManager : MonoBehaviour
{
    [Header("Parameters")]
    [Tooltip("Duration of the fade-to-black at the end of the game")]
    public float endSceneLoadDelay = 3f;

    [Tooltip("The canvas group of the fade-to-black screen")]
    public CanvasGroup endGameFadeCanvasGroup;

    [Header("Death")]
    [Tooltip("Duration of delay before the fade-to-black")]
    public float delayBeforeFadeToBlack = 4f;

    [Tooltip("Duration of delay before the death message")]
    public float delayBeforeDeathMessage = 2f;

    [Tooltip("Sound played on death")]
    public AudioClip deathSound;

    [Tooltip("Prefab for the death message")]
    public GameObject DeathMessagePrefab;

    public bool gameIsEnding { get; private set; }

    public PlayerCharacterController m_Player;

    NotificationHUDManager m_NotificationHUDManager;

    float m_TimeRespawn;

    void Start()
    {
        AudioUtility.SetMasterVolume(1);
    }

    void Update()
    {
        if (gameIsEnding)
        {
            float timeRatio =
                1 - (m_TimeRespawn - Time.time) / endSceneLoadDelay;
            endGameFadeCanvasGroup.alpha = timeRatio;

            AudioUtility.SetMasterVolume(1 - timeRatio);

            // See if it's time to respawn (after the delay)
            if (Time.time >= m_TimeRespawn)
            {
                Respawn();
            }
        }
        else
        {
            // Test if player died
            if (m_Player.isDead) EndGame();
        }
    }

    void EndGame()
    {
        // unlocks the cursor before leaving the scene, to be able to click buttons
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Remember that we need to load the appropriate end scene after a delay
        gameIsEnding = true;
        endGameFadeCanvasGroup.gameObject.SetActive(true);

        m_TimeRespawn = Time.time + endSceneLoadDelay + delayBeforeFadeToBlack;

        // play a sound on death
        var audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = deathSound;
        audioSource.playOnAwake = false;
        audioSource.outputAudioMixerGroup =
            AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.HUDVictory);
        audioSource
            .PlayScheduled(AudioSettings.dspTime + delayBeforeDeathMessage);

        Destroy(audioSource, m_TimeRespawn);

        // create a game message
        var message =
            Instantiate(DeathMessagePrefab).GetComponent<DisplayMessage>();
        if (message)
        {
            message.delayBeforeShowing = delayBeforeDeathMessage;
            message.GetComponent<Transform>().SetAsLastSibling();
            Destroy(message.gameObject, m_TimeRespawn);
        }
    }

    void Respawn()
    {
        m_Player.transform.position = new Vector3(0, 0, 0);
        m_Player.Reset();

        gameIsEnding = false;
        endGameFadeCanvasGroup.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        AudioUtility.SetMasterVolume(1);
    }
}
