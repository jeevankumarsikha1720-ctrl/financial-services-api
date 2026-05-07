using FinancialAPI.Application.DTOs.Payment;
using FinancialAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinancialAPI.API.Controllers;

/// <summary>
/// Payment Processing API — initiate, track, retry and cancel payments.
/// All endpoints require JWT Bearer authentication.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger         = logger;
    }

    // ── GET /api/v1/payments ──────────────────────────────────────────────
    /// <summary>Get paginated list of payments with optional filters.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaymentListResponse), 200)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetPaymentsQuery query,
        CancellationToken ct)
    {
        var result = await _paymentService.GetAllAsync(query, ct);
        return Ok(result);
    }

    // ── GET /api/v1/payments/{id} ─────────────────────────────────────────
    /// <summary>Get a single payment by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _paymentService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    // ── GET /api/v1/payments/reference/{ref} ──────────────────────────────
    /// <summary>Get a payment by reference number (e.g. PAY-20260415-ABC12345).</summary>
    [HttpGet("reference/{referenceNumber}")]
    [ProducesResponseType(typeof(PaymentResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByReference(
        string referenceNumber, CancellationToken ct)
    {
        var result = await _paymentService.GetByReferenceAsync(referenceNumber, ct);
        return Ok(result);
    }

    // ── POST /api/v1/payments ─────────────────────────────────────────────
    /// <summary>Initiate a new payment (domestic or international).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentResponse), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Initiate(
        [FromBody] InitiatePaymentRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var result = await _paymentService.InitiateAsync(request, userId, ct);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // ── POST /api/v1/payments/{id}/process ───────────────────────────────
    /// <summary>Move payment from Pending to Processing.</summary>
    [HttpPost("{id:guid}/process")]
    [ProducesResponseType(typeof(PaymentResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Process(Guid id, CancellationToken ct)
    {
        var result = await _paymentService.ProcessAsync(id, ct);
        return Ok(result);
    }

    // ── POST /api/v1/payments/{id}/settle ────────────────────────────────
    /// <summary>Mark a payment as Settled (successfully completed).</summary>
    [HttpPost("{id:guid}/settle")]
    [ProducesResponseType(typeof(PaymentResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Settle(Guid id, CancellationToken ct)
    {
        var result = await _paymentService.SettleAsync(id, ct);
        return Ok(result);
    }

    // ── POST /api/v1/payments/{id}/fail ──────────────────────────────────
    /// <summary>Mark a payment as Failed with a reason.</summary>
    [HttpPost("{id:guid}/fail")]
    [ProducesResponseType(typeof(PaymentResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Fail(
        Guid id,
        [FromBody] UpdatePaymentStatusRequest request,
        CancellationToken ct)
    {
        var result = await _paymentService.FailAsync(id, request.Reason ?? "No reason provided", ct);
        return Ok(result);
    }

    // ── POST /api/v1/payments/{id}/retry ─────────────────────────────────
    /// <summary>Retry a failed payment (max 3 retries).</summary>
    [HttpPost("{id:guid}/retry")]
    [ProducesResponseType(typeof(PaymentResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Retry(Guid id, CancellationToken ct)
    {
        var result = await _paymentService.RetryAsync(id, ct);
        return Ok(result);
    }

    // ── POST /api/v1/payments/{id}/cancel ────────────────────────────────
    /// <summary>Cancel a pending payment.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(PaymentResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] UpdatePaymentStatusRequest request,
        CancellationToken ct)
    {
        var result = await _paymentService.CancelAsync(id, request.Reason ?? "Cancelled by user", ct);
        return Ok(result);
    }
}
