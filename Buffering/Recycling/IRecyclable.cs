namespace Buffering.Recycling;

public interface IRecyclable
{
    public void Reset();
}

public interface IRecyclable<in TResetState> : IRecyclable
{
    public void Reset(TResetState state);
}