using System;
using UnityEngine;

public class DetectablePuck : MBaske.Sensors.Grid.DetectableGameObject
{
    public bool IsGoal;
    float IsGoalObservable()
    {
        return IsGoal ? 1 : 0;
    }

    public bool IsPushable;
    float IsPushableObservable() => IsPushable ? 1 : 0;

    public override void AddObservables()
    {
        Observables.Add("Is Goal", IsGoalObservable);
        Observables.Add("Is Pushable", IsPushableObservable);
    }
}