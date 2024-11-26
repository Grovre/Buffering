namespace Buffering.Locking.Locks;

public class NoLock : IResourceLock
{
    public ResourceLockHandle Lock(ResourceAccessFlags flags)
    {
        return new ResourceLockHandle(this, ResourceAccessFlags.Generic);
    }

    public bool TryLock(ResourceAccessFlags flags, out ResourceLockHandle hlock)
    {
        hlock = new ResourceLockHandle(this, ResourceAccessFlags.Generic);
        return true;
    }

    public void Unlock(ResourceLockHandle hlock)
    {
        // Do nothing
    }

    public IResourceLock Copy()
    {
        return new NoLock();
    }
}