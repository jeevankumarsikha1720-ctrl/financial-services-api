using FinancialAPI.Application.DTOs.Beneficiary;
using FinancialAPI.Application.Interfaces;
using FinancialAPI.Application.Interfaces.Kafka;
using FinancialAPI.Domain.Entities;
using FinancialAPI.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancialAPI.Application.Services;

public class BeneficiaryService : IBeneficiaryService
{
    private readonly IUnitOfWork _uow;
    private readonly IKafkaProducer<BeneficiaryCreatedMessage> _createdProducer;
    private readonly IKafkaProducer<BeneficiaryVerifiedMessage> _verifiedProducer;
    private readonly KafkaSettings _kafka;
    private readonly ILogger<BeneficiaryService> _logger;

    public BeneficiaryService(
        IUnitOfWork uow,
        IKafkaProducer<BeneficiaryCreatedMessage> createdProducer,
        IKafkaProducer<BeneficiaryVerifiedMessage> verifiedProducer,
        IOptions<KafkaSettings> kafkaSettings,
        ILogger<BeneficiaryService> logger)
    {
        _uow              = uow;
        _createdProducer  = createdProducer;
        _verifiedProducer = verifiedProducer;
        _kafka            = kafkaSettings.Value;
        _logger           = logger;
    }

    public async Task<BeneficiaryResponse> CreateAsync(
        CreateBeneficiaryRequest request, string userId, CancellationToken ct = default)
    {
        // Validate IBAN if provided
        if (!string.IsNullOrWhiteSpace(request.IBAN) && !Beneficiary.IsValidIBAN(request.IBAN))
            throw new ArgumentException("Invalid IBAN format.");

        if (!Beneficiary.IsValidSwiftBic(request.SwiftBic))
            throw new ArgumentException("Invalid SWIFT/BIC code.");

        // Check duplicate — same owner + same account number
        var exists = await _uow.Beneficiaries.ExistsAsync(
            b => b.OwnerId == userId && b.AccountNumber == request.AccountNumber, ct);

        if (exists)
            throw new InvalidOperationException(
                "A beneficiary with this account number already exists.");

        var beneficiary = Beneficiary.Create(
            ownerId:           userId,
            firstName:         request.FirstName,
            lastName:          request.LastName,
            email:             request.Email,
            phoneNumber:       request.PhoneNumber,
            accountNumber:     request.AccountNumber,
            iban:              request.IBAN,
            swiftBic:          request.SwiftBic,
            bankName:          request.BankName,
            bankCountryCode:   request.BankCountryCode,
            preferredCurrency: request.PreferredCurrency,
            nickname:          request.Nickname,
            createdBy:         userId);

        await _uow.Beneficiaries.AddAsync(beneficiary, ct);
        await _uow.SaveChangesAsync(ct);

        await _createdProducer.ProduceAsync(
            _kafka.Topics.BeneficiaryEvents,
            beneficiary.Id.ToString(),
            new BeneficiaryCreatedMessage
            {
                BeneficiaryId  = beneficiary.Id,
                OwnerId        = beneficiary.OwnerId,
                FullName       = beneficiary.FullName,
                AccountNumber  = beneficiary.AccountNumber,
                SwiftBic       = beneficiary.SwiftBic,
                BankCountryCode = beneficiary.BankCountryCode,
                CreatedAt      = beneficiary.CreatedAt
            }, ct);

        _logger.LogInformation("Beneficiary created. Id: {Id}, Owner: {Owner}",
            beneficiary.Id, userId);

        return MapToResponse(beneficiary);
    }

    public async Task<BeneficiaryResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var b = await _uow.Beneficiaries.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Beneficiary {id} not found.");
        return MapToResponse(b);
    }

    public async Task<BeneficiaryListResponse> GetAllAsync(
        GetBeneficiariesQuery query, CancellationToken ct = default)
    {
        var (items, total) = await _uow.Beneficiaries.GetPagedAsync(
            pageNumber: query.PageNumber,
            pageSize:   query.PageSize,
            predicate:  b =>
                (query.OwnerId        == null || b.OwnerId        == query.OwnerId)        &&
                (query.Status         == null || b.Status         == query.Status)         &&
                (query.BankCountryCode == null || b.BankCountryCode == query.BankCountryCode),
            orderBy:    b => b.CreatedAt,
            descending: true,
            ct:         ct);

        return new BeneficiaryListResponse
        {
            Items      = items.Select(MapToResponse).ToList(),
            TotalCount = total,
            PageNumber = query.PageNumber,
            PageSize   = query.PageSize
        };
    }

    public async Task<BeneficiaryResponse> UpdateAsync(
        Guid id, UpdateBeneficiaryRequest request, string userId, CancellationToken ct = default)
    {
        var b = await _uow.Beneficiaries.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Beneficiary {id} not found.");

        b.Update(request.FirstName, request.LastName, request.Email,
                 request.PhoneNumber, request.Nickname, userId);

        _uow.Beneficiaries.Update(b);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Beneficiary updated. Id: {Id}", id);
        return MapToResponse(b);
    }

    public async Task DeleteAsync(Guid id, string userId, CancellationToken ct = default)
    {
        var b = await _uow.Beneficiaries.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Beneficiary {id} not found.");

        _uow.Beneficiaries.Delete(b);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Beneficiary soft-deleted. Id: {Id}", id);
    }

    public async Task<BeneficiaryResponse> SubmitForReviewAsync(
        Guid id, CancellationToken ct = default)
    {
        var b = await _uow.Beneficiaries.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Beneficiary {id} not found.");
        b.SubmitForReview();
        _uow.Beneficiaries.Update(b);
        await _uow.SaveChangesAsync(ct);
        return MapToResponse(b);
    }

    public async Task<BeneficiaryResponse> VerifyAsync(
        Guid id, string verifiedBy, CancellationToken ct = default)
    {
        var b = await _uow.Beneficiaries.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Beneficiary {id} not found.");

        b.Verify(verifiedBy);
        _uow.Beneficiaries.Update(b);
        await _uow.SaveChangesAsync(ct);

        await _verifiedProducer.ProduceAsync(
            _kafka.Topics.BeneficiaryEvents,
            b.Id.ToString(),
            new BeneficiaryVerifiedMessage
            {
                BeneficiaryId = b.Id,
                OwnerId       = b.OwnerId,
                FullName      = b.FullName,
                VerifiedBy    = verifiedBy,
                VerifiedAt    = b.VerifiedAt!.Value
            }, ct);

        return MapToResponse(b);
    }

    public async Task<BeneficiaryResponse> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var b = await _uow.Beneficiaries.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Beneficiary {id} not found.");
        b.Activate();
        _uow.Beneficiaries.Update(b);
        await _uow.SaveChangesAsync(ct);
        return MapToResponse(b);
    }

    public async Task<BeneficiaryResponse> RejectAsync(
        Guid id, string reason, CancellationToken ct = default)
    {
        var b = await _uow.Beneficiaries.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Beneficiary {id} not found.");
        b.Reject(reason);
        _uow.Beneficiaries.Update(b);
        await _uow.SaveChangesAsync(ct);
        return MapToResponse(b);
    }

    public async Task<BeneficiaryResponse> SuspendAsync(Guid id, CancellationToken ct = default)
    {
        var b = await _uow.Beneficiaries.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Beneficiary {id} not found.");
        b.Suspend();
        _uow.Beneficiaries.Update(b);
        await _uow.SaveChangesAsync(ct);
        return MapToResponse(b);
    }

    private static BeneficiaryResponse MapToResponse(Beneficiary b) => new()
    {
        Id                = b.Id,
        OwnerId           = b.OwnerId,
        Nickname          = b.Nickname,
        FirstName         = b.FirstName,
        LastName          = b.LastName,
        FullName          = b.FullName,
        Email             = b.Email,
        PhoneNumber       = b.PhoneNumber,
        AccountNumber     = b.AccountNumber,
        IBAN              = b.IBAN,
        SwiftBic          = b.SwiftBic,
        BankName          = b.BankName,
        BankCountryCode   = b.BankCountryCode,
        PreferredCurrency = b.PreferredCurrency.ToString(),
        Status            = b.Status.ToString(),
        IsVerified        = b.IsVerified,
        VerifiedAt        = b.VerifiedAt,
        VerifiedBy        = b.VerifiedBy,
        RejectionReason   = b.RejectionReason,
        CreatedAt         = b.CreatedAt,
        UpdatedAt         = b.UpdatedAt
    };
}
