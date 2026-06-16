using System.Security.Claims;
using RouteOptimizer.Application.Abstractions;

namespace RouteOptimizer.API.Services;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal User => _httpContextAccessor.HttpContext!.User;

    public Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public Guid? WarehouseId
    {
        get
        {
            var value = User.FindFirstValue("warehouse_id");
            return value is not null ? Guid.Parse(value) : null;
        }
    }
}
