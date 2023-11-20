namespace Buffering.FineLockBuffering.SkipBuffering;

public struct SkipBufferIncomingUpdater<T, TUpdaterState>
    where T : struct
{
    private FineLockingSkipBuffer<T, TUpdaterState> _skipBuffer;

    public SkipBufferIncomingUpdater()
    {
        throw new NotImplementedException(
            "Updater must be retrieved from a buffer property");
    }

    public SkipBufferIncomingUpdater(FineLockingSkipBuffer<T, TUpdaterState> skipBuffer)
    {
        _skipBuffer = skipBuffer;
    }

    public void UpdateNextUnlockedBuffer(TUpdaterState state)
    {
        _skipBuffer.UpdateNextUnlockedBuffer(state);
    }
    
    public bool TryUpdate(int index, TUpdaterState state)
    {
        return _skipBuffer.TryUpdate(index, state);
    }
}