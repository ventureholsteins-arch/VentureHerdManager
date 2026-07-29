namespace VentureHerdManager.Api.Services;

public sealed class DatabaseInitializationState
{
    private volatile bool _isReady;

    public bool IsReady => _isReady;

    public void MarkReady()
    {
        _isReady = true;
    }
}
