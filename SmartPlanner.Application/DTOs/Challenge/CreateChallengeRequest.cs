using System;
using System.ComponentModel.DataAnnotations;
using SmartPlanner.Domain.Entities;

namespace SmartPlanner.Domain.DTOs.Challenge
{
    public class CreateChallengeRequest
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public ChallengeType Type { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public bool IsGroupChallenge { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int TargetValue { get; set; }

        [Required]
        public Guid CreatedBy { get; set; }
    }

    public class ChallengeResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ChallengeType Type { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsGroupChallenge { get; set; }
        public int TargetValue { get; set; }
        public int CurrentValue { get; set; }
        public double GroupProgressPercentage { get; set; }
        public bool IsActive { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}