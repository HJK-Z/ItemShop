using System.Collections;
using UnityEngine;

[System.Serializable]
public class Effect
{
    public string name;

    public float value;

    public float timer;

    public bool isPermanent;


    public Effect(string name, int value, float timer, bool isPermanent)
    {
        this.name = name;
        this.value = value;
        this.timer = timer;
        this.isPermanent = isPermanent;
    }

}
