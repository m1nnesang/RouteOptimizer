using MediatR;
using RouteOptimizer.Application.Abstractions;
using RouteOptimizer.Application.Common;

namespace RouteOptimizer.Application.Drivers.GetDrivers;

public class GetDriversQueryHandler : IRequestHandler<GetDriversQuery, PagedResult<DriverListItemDto>>
{
    private readonly IUserRepository _userRepository;

    public GetDriversQueryHandler(IUserRepository userRepository) => _userRepository = userRepository;

    public async Task<PagedResult<DriverListItemDto>> Handle(GetDriversQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var (drivers, totalCount) = await _userRepository.GetAllDriversAsync(skip, request.PageSize, ct);

        var items = drivers.Select(d => new DriverListItemDto(
            d.Id,
            d.FirstName,
            d.LastName,
            d.Email,
            d.PhoneNumber.Value,
            d.IsActive,
            d.DriverProfile?.HighestLicenseCategory.ToString()
        )).ToList();

        return new PagedResult<DriverListItemDto>(items, totalCount, request.Page, request.PageSize);
    }
}
