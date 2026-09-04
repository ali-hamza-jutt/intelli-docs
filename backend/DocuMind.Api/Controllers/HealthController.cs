using DocuMind.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DocuMind.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthService _healthService;

    public HealthController(IHealthService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var status = _healthService.GetStatus();

        return Ok(new
        {
            status
        });
    }
}