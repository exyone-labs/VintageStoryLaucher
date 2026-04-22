using VSL.Domain;

namespace VSL.Application;

public interface IVs2QQProcessService
{
    event EventHandler<string>? OutputReceived;

    event EventHandler<Vs2QQRuntimeStatus>? StatusChanged;

    Vs2QQRuntimeStatus CurrentStatus { get; }

    Task<OperationResult> StartAsync(Vs2QQLaunchSettings settings, CancellationToken cancellationToken = default);

    Task<OperationResult> StopAsync(TimeSpan gracefulTimeout, CancellationToken cancellationToken = default);
}
