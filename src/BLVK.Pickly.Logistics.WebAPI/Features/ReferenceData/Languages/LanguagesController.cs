using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.ReferenceData.Features.GetLanguages;
using Microsoft.AspNetCore.Mvc;

namespace BLVK.Pickly.Logistics.WebAPI.Features.ReferenceData.Languages;

[ApiController]
[ApiVersion("1.0")]
[Route(EndpointUrl.Languages.Route)]
public sealed class LanguagesController(GetLanguagesHandler handler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GetLanguagesResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<GetLanguagesResponse>> Get() => Ok(handler.Handle());
}
