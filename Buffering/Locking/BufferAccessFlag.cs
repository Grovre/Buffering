namespace Buffering.Locking;

[Flags]
public enum BufferAccessFlag
{
    Generic = 1 << 0,
    Read = 1 << 1,
    Write = 1 << 2
}