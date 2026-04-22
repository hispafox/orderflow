namespace Gateway.API.Services;

public interface IAuthFailureCounter
{
    Task<int> IncrementAsync(string key, CancellationToken ct = default);
}
