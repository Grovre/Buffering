namespace Buffering.Locking.Locks;

public class NoLock : IResourceLock
{
    public event EventHandler? Locking;
    public event EventHandler? AfterLocked;
    public event EventHandler? Unlocking;
    public event EventHandler? AfterUnlocked;
    
    public ResourceLockHandle Lock(ResourceAccessFlags flags)
    {
        Locking?.Invoke(this, EventArgs.Empty);
        AfterLocked?.Invoke(this, EventArgs.Empty);
        return new ResourceLockHandle(this, ResourceAccessFlags.Generic);;
    }

    public bool TryLock(ResourceAccessFlags flags, out ResourceLockHandle hlock)
    {
        Locking?.Invoke(this, EventArgs.Empty);
        hlock = new ResourceLockHandle(this, ResourceAccessFlags.Generic);
        AfterLocked?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Unlock(ResourceLockHandle hlock)
    {
        Unlocking?.Invoke(this, EventArgs.Empty);
        AfterUnlocked?.Invoke(this, EventArgs.Empty);
    }

    public IResourceLock Copy()
    {
        return new NoLock();
    }
}