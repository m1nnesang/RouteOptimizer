using System.Security.Claims;
using RouteOptimizer.Application.Abstractions;

namespace RouteOptimizer.API.Services;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal User =>
        _httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("No active HTTP context for the current user.");

    public Guid UserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("Current user identifier claim is missing or invalid.");

    public Guid? WarehouseId =>
        Guid.TryParse(User.FindFirstValue("warehouse_id"), out var id) ? id : null;
}
