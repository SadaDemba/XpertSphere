namespace XpertSphere.MonolithApi.DTOs.User;

/// <summary>
/// DTO for user statistics and metrics
/// </summary>
public record UserStatsDto
{
    public Guid UserId { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    
    // Profile statistics
    public int ProfileCompletionPercentage { get; set; }
    public DateTime? ProfileLastUpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Activity statistics
    public int TotalLogins { get; set; }
    public int LoginThisMonth { get; set; }
    public TimeSpan? AverageSessionDuration { get; set; }
    public DateTime? LastPasswordChange { get; set; }
    
    // For candidates - application statistics
    public CandidateStatsDto? CandidateStats { get; set; }
    
    // For recruiters - recruitment statistics  
    public RecruiterStatsDto? RecruiterStats { get; set; }
    
    // Security statistics
    public int FailedLoginAttempts { get; set; }
    public DateTime? LastFailedLogin { get; set; }
    public bool IsAccountLocked { get; set; }
    public DateTime? AccountLockedUntil { get; set; }
    
    // GDPR compliance
    public DateTime? ConsentGivenAt { get; set; }
    public DateTime? ConsentWithdrawnAt { get; set; }
    public DateTime? DataRetentionUntil { get; set; }
    public bool IsAnonymized { get; set; }
}

/// <summary>
/// Statistics specific to candidates
/// </summary>
public class CandidateStatsDto
{
    public int TotalApplications { get; set; }
    public int ApplicationsThisMonth { get; set; }
    public int ApplicationsInProgress { get; set; }
    public int ApplicationsAccepted { get; set; }
    public int ApplicationsRejected { get; set; }
    public int InterviewsScheduled { get; set; }
    public int InterviewsCompleted { get; set; }
    public DateTime? LastApplicationDate { get; set; }
    public TimeSpan? AverageResponseTime { get; set; }
    public decimal? SuccessRate { get; set; }
}

/// <summary>
/// Statistics specific to recruiters and HR staff
/// </summary>
public class RecruiterStatsDto
{
    public int JobPostingsCreated { get; set; }
    public int JobPostingsActive { get; set; }
    public int CandidatesReviewed { get; set; }
    public int CandidatesInterviewed { get; set; }
    public int CandidatesHired { get; set; }
    public int InterviewsScheduled { get; set; }
    public int InterviewsCompleted { get; set; }
    public DateTime? LastJobPosting { get; set; }
    public TimeSpan? AverageTimeToHire { get; set; }
    public decimal? HireRate { get; set; }
}

/// <summary>
/// Summary DTO for dashboard metrics
/// </summary>
public class UserDashboardStatsDto
{
    public Guid UserId { get; set; }
    public required string FullName { get; set; }
    public int ProfileCompletionPercentage { get; set; }
    public DateTime? LastLogin { get; set; }
    public int UnreadNotifications { get; set; }
    public int PendingTasks { get; set; }
    
    // Recent activity summary
    public List<RecentActivityDto> RecentActivities { get; set; } = [];
    
    // Quick stats based on user type
    public Dictionary<string, object> QuickStats { get; set; } = [];
}

/// <summary>
/// DTO for recent user activities
/// </summary>
public class RecentActivityDto
{
    public DateTime Timestamp { get; set; }
    public required string ActivityType { get; set; }
    public required string Description { get; set; }
    public string? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
}
