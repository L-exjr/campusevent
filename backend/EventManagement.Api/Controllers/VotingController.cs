using EventManagement.Api.DTOs.Voting;
using EventManagement.Api.Infrastructure;
using EventManagement.Api.Models;
using EventManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EventManagement.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/voting")]
public sealed class EventVotingController(IVotingService votingService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<VotingCampaignResponse>> Get(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        Guid? actorId = User.Identity?.IsAuthenticated == true ? User.GetRequiredUserId() : null;
        UserRole? actorRole = User.Identity?.IsAuthenticated == true ? User.GetRequiredRole() : null;
        return Ok(await votingService.GetCampaignAsync(
            eventId, actorId, actorRole, cancellationToken));
    }

    [Authorize(Roles = "Student,Organizer,Admin")]
    [HttpPut]
    public async Task<ActionResult<VotingCampaignResponse>> Upsert(
        Guid eventId,
        VotingCampaignUpsertRequest request,
        CancellationToken cancellationToken) =>
        Ok(await votingService.UpsertCampaignAsync(
            eventId, User.GetRequiredUserId(), User.GetRequiredRole(), request, cancellationToken));
}

[ApiController]
[Authorize(Roles = "Student,Organizer")]
[EnableRateLimiting("Voting")]
[Route("api/voting")]
public sealed class VotingController(IVotingService votingService) : ControllerBase
{
    [HttpPost("categories/{categoryId:guid}/votes")]
    public async Task<ActionResult<VoteAcceptedResponse>> CastFreeVote(
        Guid categoryId,
        CastFreeVoteRequest request,
        CancellationToken cancellationToken) =>
        Ok(await votingService.CastFreeVoteAsync(
            categoryId, request.NomineeId, User.GetRequiredUserId(), cancellationToken));

    [HttpPost("categories/{categoryId:guid}/payments/initialize")]
    public async Task<ActionResult<VotingPaymentInitializationResponse>> InitializePayment(
        Guid categoryId,
        InitializePaidVoteRequest request,
        CancellationToken cancellationToken) =>
        Ok(await votingService.InitializePaidVoteAsync(
            categoryId, request.NomineeId, request.Quantity,
            User.GetRequiredUserId(), cancellationToken));

    [HttpGet("payments/{reference}")]
    public async Task<ActionResult<VotingPaymentStatusResponse>> GetPaymentStatus(
        string reference,
        CancellationToken cancellationToken) =>
        Ok(await votingService.GetPaymentStatusAsync(
            reference, User.GetRequiredUserId(), cancellationToken));
}
