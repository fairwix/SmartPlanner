using System;
using System.ComponentModel.DataAnnotations;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Application.DTOs.Challenge;

    public record CreateChallengeRequest(
        [Required, StringLength(100)] string Title,
        [StringLength(500)] string Description,
        [Required] ChallengeType Type,
        [Required] DateTime StartDate,
        [Required] DateTime EndDate,
        [Required] bool IsGroupChallenge,
        [Required, Range(1, int.MaxValue)] int TargetValue,
        [Required] Guid CreatedBy);

    public record ChallengeResponse(
        Guid Id,
        string Title,
        string Description,
        ChallengeType Type,
        DateTime StartDate,
        DateTime EndDate,
        bool IsGroupChallenge,
        int TargetValue,
        int CurrentValue,
        double GroupProgressPercentage,
        bool IsActive,
        Guid CreatedBy,
        DateTime CreatedAt);

