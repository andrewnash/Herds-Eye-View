public class DetectableBlock : MBaske.Sensors.Grid.DetectableGameObject
{
    public float size; // Set normalized value.
    float SizeObservable() => size;

    public override void AddObservables()
    {
        Observables.Add("Block Size", SizeObservable);
    }
}