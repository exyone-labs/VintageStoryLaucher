using VSL.Domain;

namespace VSL.Application;

public interface IServerConfigService
{
    Task<OperationResult<ServerCommonSettings>> LoadServerSettingsAsync(ServerProfile profile, CancellationToken cancellationToken = default);

    Task<OperationResult<WorldSettings>> LoadWorldSettingsAsync(ServerProfile profile, CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyList<WorldRuleValue>>> LoadWorldRulesAsync(ServerProfile profile, CancellationToken cancellationToken = default);

    Task<OperationResult> SaveCommonSettingsAsync(
        ServerProfile profile,
        ServerCommonSettings serverSettings,
        WorldSettings worldSettings,
        IReadOnlyList<WorldRuleValue> rules,
        CancellationToken cancellationToken = default);

    Task<OperationResult<string>> LoadRawJsonAsync(ServerProfile profile, CancellationToken cancellationToken = default);

    Task<OperationResult> SaveRawJsonAsync(ServerProfile profile, string json, CancellationToken cancellationToken = default);
}
