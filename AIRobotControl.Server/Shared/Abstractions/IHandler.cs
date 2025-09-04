namespace AIRobotControl.Server.Shared.Abstractions;

/// <summary>
/// Marker interface (legacy) — not required for new handlers.
/// </summary>
public interface IHandler { }

public interface IHandler<in TRequest>
{
	Task Handle(TRequest request, CancellationToken ct);
}

public interface IHandler<in TRequest, TResponse>
{
	Task<TResponse> Handle(TRequest request, CancellationToken ct);
}