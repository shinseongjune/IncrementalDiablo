using System;
using UnityEngine;

[Serializable]
public struct ResourceAmount
{
    [SerializeField] private ResourceId resource;
    [SerializeField] private int amount;

    public ResourceId Resource => resource;
    public int Amount => Mathf.Max(0, amount);

    public ResourceAmount(ResourceId resource, int amount)
    {
        this.resource = resource;
        this.amount = Mathf.Max(0, amount);
    }

    public override string ToString()
    {
        return $"{Resource}: {Amount}";
    }
}
