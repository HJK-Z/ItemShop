using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Affectable : MonoBehaviour
{
    public List<Effect> effects = new List<Effect>();

    public PlayerCharacterController playerbody;

    public PlayerHoldingManager playerinv;

    public float nHealth;

    public float nDamage;

    void Awake()
    {
        nHealth = GameConstants.baseHealth;
        nDamage = 1f;
    }

    void Update()
    {
        foreach (Effect e in effects)
        {
            if (e.name == "Health")
            {
                nHealth += e.value;
            }
            else if (e.name == "Damage")
            {
                nDamage *= e.value;
            }
            else if (e.name == "Healing")
            {
                player.m_Health.Heal(e.value / Time.deltaTime);
            }
            e.timer -= Time.deltaTime;

            if (e.timer <= 0f)
            {
                Destroy (e);
            }
        }
        playerbody.m_Health.maxHealth = nHealth;
        playerinv.damageMultiplier = nDamage;
    }
}
