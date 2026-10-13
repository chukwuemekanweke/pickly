using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.ReferenceData.Features.GetCountries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BLVK.Pickly.Logistics.WebAPI.Features.ReferenceData.Countries;

[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route(EndpointUrl.Countries.Route)]
public sealed class CountriesController(GetCountriesHandler handler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GetCountriesResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GetCountriesResponse>>> Handle(CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(cancellationToken);
        return Ok(response);
    }
}
