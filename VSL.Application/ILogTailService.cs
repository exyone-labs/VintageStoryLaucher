using VSL.Domain;

namespace VSL.Application;

public interface ILogTailService : IDisposable
{
    event EventHandler<string>? LogLineReceived;

    Task StartAsync(ServerProfile profile, CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}
