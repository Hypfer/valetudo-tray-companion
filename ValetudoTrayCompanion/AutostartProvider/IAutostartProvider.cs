namespace ValetudoTrayCompanion.AutostartProvider;

public interface IAutostartProvider
{
    public bool IsSupported { get; }
    public bool IsReady { get; }
    public bool IsAutostartEnabled { get; }
    public void EnableAutostart();
    public void DisableAutostart();
}