using System.ComponentModel.DataAnnotations;
using EventManagement.Api.Models;

namespace EventManagement.Api.DTOs.Voting;

public sealed record VotingNomineeInput(
    [param: Required, StringLength(150, MinimumLength = 2)] string Name,
    [param: StringLength(1000)] string? Description);

public sealed record VotingCategoryInput(
    [param: Required, StringLength(150, MinimumLength = 2)] string Name,
    [param: StringLength(1000)] string? Description,
    VotingMode Mode,
    [param: Range(0, long.MaxValue)] long PricePerVoteMinor,
    [param: Required, MinLength(2)] IReadOnlyList<VotingNomineeInput> Nominees);

public sealed record VotingCampaignUpsertRequest(
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    bool IsPublished,
    bool ShowLiveResults,
    [param: Required, MinLength(1)] IReadOnlyList<VotingCategoryInput> Categories);

public sealed record VotingNomineeResponse(
    Guid Id,
    string Name,
    string? Description,
    long? VoteCount);

public sealed record VotingCategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    VotingMode Mode,
    long PricePerVoteMinor,
    string Currency,
    bool HasVoted,
    IReadOnlyList<VotingNomineeResponse> Nominees);

public sealed record VotingCampaignResponse(
    Guid Id,
    Guid EventId,
    string EventTitle,
    DateTimeOffset OpensAt,
    DateTimeOffset ClosesAt,
    bool IsPublished,
    bool ShowLiveResults,
    string Status,
    bool CanManage,
    bool ResultsVisible,
    IReadOnlyList<VotingCategoryResponse> Categories);

public sealed record CastFreeVoteRequest(Guid NomineeId);

public sealed record InitializePaidVoteRequest(
    Guid NomineeId,
    [param: Range(1, 100)] int Quantity);

public sealed record VoteAcceptedResponse(
    Guid CategoryId,
    Guid NomineeId,
    int Quantity,
    DateTimeOffset CastAt);

public sealed record VotingPaymentInitializationResponse(
    string Reference,
    string AuthorizationUrl,
    Guid CategoryId,
    Guid NomineeId,
    int Quantity,
    long AmountMinor,
    string Currency,
    DateTimeOffset ExpiresAt);

public sealed record VotingPaymentStatusResponse(
    string Reference,
    PaymentOrderStatus Status,
    Guid CategoryId,
    Guid NomineeId,
    int Quantity,
    long AmountMinor,
    string Currency,
    bool VoteRecorded,
    DateTimeOffset ExpiresAt);
