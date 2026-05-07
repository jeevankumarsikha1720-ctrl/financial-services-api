using FinancialAPI.Application.DTOs.Settlement;
using FinancialAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinancialAPI.API.Controllers;

/// <summary>
/// Settlement batch processing — create batches, add payment entries,
/// run net settlement calculations, complete and reconcile.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class SettlementsController : ControllerBase
{
    private readonly ISettlementService _settlementService;
    private readonly ILogger<SettlementsController> _logger;

    public SettlementsController(
        ISettlementService settlementService,
        ILogger<SettlementsController> logger)
    {
        _settlementService = settlementService;
        _logger            = logger;
    }

    /// <summary>Get paginated list of settlement batches.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(SettlementListResponse), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetSettlementsQuery query, CancellationToken ct)
    {
        var result = await _settlementService.GetAllAsync(query, ct);
        return Ok(result);
    }

    /// <summary>Get a settlement batch by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SettlementResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _settlementService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Create a new settlement batch.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SettlementResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSettlementRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var result = await _settlementService.CreateAsync(request, userId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Add a payment entry to a Draft settlement batch.</summary>
    [HttpPost("{id:guid}/entries")]
    [ProducesResponseType(typeof(SettlementResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddEntry(
        Guid id, [FromBody] AddSettlementEntryRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _settlementService.AddEntryAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Start processing a Draft settlement batch.</summary>
    [HttpPost("{id:guid}/process")]
    [ProducesResponseType(typeof(SettlementResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> StartProcessing(Guid id, CancellationToken ct)
    {
        var result = await _settlementService.StartProcessingAsync(id, ct);
        return Ok(result);
    }

    /// <summary>Complete a settlement batch and set reconciliation reference.</summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(SettlementResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Complete(
        Guid id, [FromBody] CompleteSettlementRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _settlementService.CompleteAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Reconcile a completed settlement batch.</summary>
    [HttpPost("{id:guid}/reconcile")]
    [ProducesResponseType(typeof(SettlementResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Reconcile(
        Guid id, [FromBody] ReconcileSettlementRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _settlementService.ReconcileAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Mark a settlement batch as Failed.</summary>
    [HttpPost("{id:guid}/fail")]
    [ProducesResponseType(typeof(SettlementResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Fail(
        Guid id, [FromBody] string reason, CancellationToken ct)
    {
        var result = await _settlementService.FailAsync(id, reason, ct);
        return Ok(result);
    }
}
