namespace MutaBorn.Gameplay
{
    // A capability, not a unit type. Non-unit objects can implement this later.
    // A resource is available only when TryGetResource returns true.
    public interface IResourceTarget
    {
        bool TryGetResource(ResourceIdentifier resource, out int current, out int maximum);

        // Low-level write. Call ResourceOperations.TryApply for checked changes.
        bool TryChangeResource(ResourceIdentifier resource, int amount);

    }

}
