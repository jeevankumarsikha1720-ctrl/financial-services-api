using FinancialAPI.Application.DTOs.Ledger;
using FinancialAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinancialAPI.API.Controllers;

/// <summary>
/// Transaction Ledger — post entries, query balance history, and reverse entries.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class LedgerController : ControllerBase
{
    private readonly ILedgerService _ledgerService;
    private readonly ILogger<LedgerController> _logger;

    public LedgerController(ILedgerService ledgerService, ILogger<LedgerController> logger)
    {
        _ledgerService = ledgerService;
        _logger        = logger;
    }

    /// <summary>Get paginated ledger entries with optional filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(LedgerListResponse), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetLedgerEntriesQuery query, CancellationToken ct)
        => Ok(await _ledgerService.GetAllAsync(query, ct));

    /// <summary>Get a single ledger entry by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LedgerEntryResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _ledgerService.GetByIdAsync(id, ct));

    /// <summary>Get the current balance for an account.</summary>
    [HttpGet("accounts/{accountId}/balance")]
    [ProducesResponseType(typeof(AccountBalanceResponse), 200)]
    public async Task<IActionResult> GetBalance(string accountId, CancellationToken ct)
        => Ok(await _ledgerService.GetBalanceAsync(accountId, ct));

    /// <summary>Post a new debit or credit ledger entry.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LedgerEntryResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> PostEntry(
        [FromBody] PostLedgerEntryRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var result = await _ledgerService.PostEntryAsync(request, userId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Reverse an existing ledger entry (creates offsetting entry).</summary>
    [HttpPost("{id:guid}/reverse")]
    [ProducesResponseType(typeof(LedgerEntryResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Reverse(
        Guid id, [FromBody] ReverseLedgerEntryRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var result = await _ledgerService.ReverseAsync(id, userId, ct);
        return Ok(result);
    }
}
