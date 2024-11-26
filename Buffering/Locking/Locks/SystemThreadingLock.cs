using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

#pragma warning disable S3261 // Namespaces should not be empty
namespace Buffering.Locking.Locks;
#pragma warning restore S3261 // Namespaces should not be empty

#if NET9_0_OR_GREATER

/// <summary>
/// Uses a monitor to lock the front buffer for reading/writing.
/// Best use case is for two threads where one is reading the buffer and the other is updating the buffer.
/// ResourceAccessFlags is ignored as only one thread can enter the monitor at a time.
/// </summary>
public class SystemThreadingLock : IResourceLock
{
    private readonly Lock _lock = new();

    /// <inheritdoc />
    public ResourceLockHandle Lock(ResourceAccessFlags flags)
    {
        _lock.Enter();
        return new ResourceLockHandle(this, ResourceAccessFlags.Generic);
    }

    /// <inheritdoc />
    public bool TryLock(ResourceAccessFlags flags, out ResourceLockHandle hlock)
    {
        hlock = new ResourceLockHandle(this, flags);
        var attempt = _lock.TryEnter();
        return attempt;
    }

    /// <inheritdoc />
    public void Unlock(ResourceLockHandle hlock)
    {
        if (hlock.Owner != this)
            throw new AuthenticationException(IResourceLock.BadOwnerExceptionMessage);

        _lock.Exit();
    }

    /// <inheritdoc />
    public IResourceLock Copy()
    {
        return new SystemThreadingLock();
    }
}

#endif