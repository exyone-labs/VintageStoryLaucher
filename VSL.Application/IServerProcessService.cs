using VSL.Domain;

namespace VSL.Application;

public interface IServerProcessService
{
    event EventHandler<string>? OutputReceived;

    event EventHandler<ServerRuntimeStatus>? StatusChanged;

    ServerRuntimeStatus CurrentStatus { get; }

    Task<OperationResult> StartAsync(ServerProfile profile, CancellationToken cancellationToken = default);

    Task<OperationResult> StopAsync(TimeSpan gracefulTimeout, CancellationToken cancellationToken = default);

    Task<OperationResult> SendCommandAsync(string command, CancellationToken cancellationToken = default);
}
