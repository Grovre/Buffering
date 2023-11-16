namespace Buffering.Locking;

[Flags]
public enum Accessor
{
    Generic = 0,
    Read = 1 << 0,
    Write = 1 << 1
}