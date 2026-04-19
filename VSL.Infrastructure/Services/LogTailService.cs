using System.Text;
using VSL.Application;
using VSL.Domain;

namespace VSL.Infrastructure.Services;

public sealed class LogTailService : ILogTailService
{
    private CancellationTokenSource? _cts;
    private Task? _tailTask;
    private string? _currentLogPath;
    private long _position;

    public event EventHandler<string>? LogLineReceived;

    public async Task StartAsync(ServerProfile profile, CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken);

        var logsPath = WorkspaceLayout.GetLogsPath(profile.DataPath);
        Directory.CreateDirectory(logsPath);

        _currentLogPath = WorkspaceLayout.GetServerMainLogPath(profile.DataPath);
        _position = 0;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _tailTask = TailLoopAsync(_cts.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_cts is null)
        {
            return;
        }

        try
        {
            await _cts.CancelAsync();
            if (_tailTask is not null)
            {
                await _tailTask.WaitAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _tailTask = null;
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private async Task TailLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentLogPath) || !File.Exists(_currentLogPath))
                {
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                await using var stream = new FileStream(_currentLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (stream.Length < _position)
                {
                    _position = 0;
                }

                stream.Seek(_position, SeekOrigin.Begin);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        LogLineReceived?.Invoke(this, line);
                    }
                }

                _position = stream.Position;
                await Task.Delay(1000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(1200, cancellationToken);
            }
        }
    }
}
