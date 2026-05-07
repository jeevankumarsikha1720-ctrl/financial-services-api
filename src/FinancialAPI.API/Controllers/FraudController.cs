using FinancialAPI.Application.DTOs.Fraud;
using FinancialAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinancialAPI.API.Controllers;

/// <summary>
/// Fraud Detection — raise alerts, review, and resolve fraud cases.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class FraudController : ControllerBase
{
    private readonly IFraudService _fraudService;
    private readonly ILogger<FraudController> _logger;

    public FraudController(IFraudService fraudService, ILogger<FraudController> logger)
    {
        _fraudService = fraudService;
        _logger       = logger;
    }

    /// <summary>Get paginated fraud alerts with optional filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(FraudAlertListResponse), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetFraudAlertsQuery query, CancellationToken ct)
        => Ok(await _fraudService.GetAllAsync(query, ct));

    /// <summary>Get a fraud alert by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FraudAlertResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _fraudService.GetByIdAsync(id, ct));

    /// <summary>Raise a new fraud alert for a payment (risk score must be > 0.4).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(FraudAlertResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RaiseAlert(
        [FromBody] RaiseFraudAlertRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _fraudService.RaiseAlertAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Mark a fraud alert as a false positive — unblocks the payment.</summary>
    [HttpPost("{id:guid}/resolve-genuine")]
    [ProducesResponseType(typeof(FraudAlertResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ResolveGenuine(
        Guid id, [FromBody] ReviewFraudAlertRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        return Ok(await _fraudService.ResolveAsGenuineAsync(id, request, userId, ct));
    }

    /// <summary>Confirm a fraud alert as real fraud — payment stays blocked.</summary>
    [HttpPost("{id:guid}/confirm-fraud")]
    [ProducesResponseType(typeof(FraudAlertResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ConfirmFraud(
        Guid id, [FromBody] ReviewFraudAlertRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        return Ok(await _fraudService.ConfirmFraudAsync(id, request, userId, ct));
    }
}
