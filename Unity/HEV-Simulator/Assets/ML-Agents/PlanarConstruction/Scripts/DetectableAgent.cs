public class DetectableAgent : MBaske.Sensors.Grid.DetectableGameObject
{
    public bool IsControlAgent;
    float IsControlAgentObservable() => IsControlAgent ? 1 : 0;

    public bool IsAllyAgent;
    float IsAllyAgentObservable() => IsAllyAgent ? 1 : 0;

    public override void AddObservables()
    {
        Observables.Add("Is Control Agent", IsControlAgentObservable);
        Observables.Add("Is Ally Agent", IsAllyAgentObservable);
    }
}