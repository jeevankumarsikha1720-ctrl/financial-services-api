using FinancialAPI.Application.DTOs.Beneficiary;
using FinancialAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinancialAPI.API.Controllers;

/// <summary>
/// Beneficiary Management — add, verify, update and manage payment recipients.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class BeneficiariesController : ControllerBase
{
    private readonly IBeneficiaryService _beneficiaryService;
    private readonly ILogger<BeneficiariesController> _logger;

    public BeneficiariesController(
        IBeneficiaryService beneficiaryService,
        ILogger<BeneficiariesController> logger)
    {
        _beneficiaryService = beneficiaryService;
        _logger             = logger;
    }

    /// <summary>Get paginated list of beneficiaries.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(BeneficiaryListResponse), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetBeneficiariesQuery query, CancellationToken ct)
        => Ok(await _beneficiaryService.GetAllAsync(query, ct));

    /// <summary>Get a beneficiary by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BeneficiaryResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _beneficiaryService.GetByIdAsync(id, ct));

    /// <summary>Add a new beneficiary (starts in Pending status).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BeneficiaryResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBeneficiaryRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var result = await _beneficiaryService.CreateAsync(request, userId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Update beneficiary personal details (resets verification).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BeneficiaryResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateBeneficiaryRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var result = await _beneficiaryService.UpdateAsync(id, request, userId, ct);
        return Ok(result);
    }

    /// <summary>Soft-delete a beneficiary.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        await _beneficiaryService.DeleteAsync(id, userId, ct);
        return NoContent();
    }

    /// <summary>Submit beneficiary for compliance review.</summary>
    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(typeof(BeneficiaryResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
        => Ok(await _beneficiaryService.SubmitForReviewAsync(id, ct));

    /// <summary>Verify a beneficiary (compliance officer action).</summary>
    [HttpPost("{id:guid}/verify")]
    [ProducesResponseType(typeof(BeneficiaryResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Verify(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        return Ok(await _beneficiaryService.VerifyAsync(id, userId, ct));
    }

    /// <summary>Activate a verified beneficiary.</summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(typeof(BeneficiaryResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
        => Ok(await _beneficiaryService.ActivateAsync(id, ct));

    /// <summary>Reject a beneficiary with a reason.</summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(BeneficiaryResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Reject(
        Guid id, [FromBody] RejectBeneficiaryRequest request, CancellationToken ct)
        => Ok(await _beneficiaryService.RejectAsync(id, request.Reason, ct));

    /// <summary>Suspend an active beneficiary.</summary>
    [HttpPost("{id:guid}/suspend")]
    [ProducesResponseType(typeof(BeneficiaryResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken ct)
        => Ok(await _beneficiaryService.SuspendAsync(id, ct));
}
