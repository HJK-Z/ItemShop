using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Affectable : MonoBehaviour
{
    public List<Effect> effects = new List<Effect>();

    public PlayerCharacterController playerbody;

    public Health health;

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
                health.Heal(e.value / Time.deltaTime);
            }
            e.timer -= Time.deltaTime;
        }

        effects.RemoveAll(e => e.timer <= 0f);

        health.maxHealth = nHealth;
        playerinv.damageMultiplier = nDamage;
    }
}
