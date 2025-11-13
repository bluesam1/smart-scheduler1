using System.Text.Json;

namespace SmartScheduler.Domain.Contracts.Entities;

/// <summary>
/// Audit trail record for recommendation requests.
/// Persists request payload, candidate set, scores, and selection for audit purposes.
/// </summary>
public class AuditRecommendation
{
    public Guid Id { get; private set; }
    public Guid JobId { get; private set; }
    public string RequestPayloadJson { get; private set; } = string.Empty; // JSON serialized RecommendationRequest
    public string CandidatesJson { get; private set; } = string.Empty; // JSON serialized CandidateScore[]
    public Guid? SelectedContractorId { get; private set; }
    public string SelectionActor { get; private set; } = string.Empty; // User ID or "system"
    public int ConfigVersion { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Private constructor for EF Core
    private AuditRecommendation() { }

    public AuditRecommendation(
        Guid id,
        Guid jobId,
        string requestPayloadJson,
        string candidatesJson,
        int configVersion,
        string selectionActor = "system",
        Guid? selectedContractorId = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("ID cannot be empty.", nameof(id));

        if (jobId == Guid.Empty)
            throw new ArgumentException("Job ID cannot be empty.", nameof(jobId));

        if (string.IsNullOrWhiteSpace(requestPayloadJson))
            throw new ArgumentException("Request payload cannot be empty.", nameof(requestPayloadJson));

        if (string.IsNullOrWhiteSpace(candidatesJson))
            throw new ArgumentException("Candidates JSON cannot be empty.", nameof(candidatesJson));

        if (configVersion < 1)
            throw new ArgumentOutOfRangeException(nameof(configVersion), "Config version must be at least 1.");

        if (string.IsNullOrWhiteSpace(selectionActor))
            throw new ArgumentException("Selection actor cannot be empty.", nameof(selectionActor));

        Id = id;
        JobId = jobId;
        RequestPayloadJson = requestPayloadJson;
        CandidatesJson = candidatesJson;
        ConfigVersion = configVersion;
        SelectionActor = selectionActor;
        SelectedContractorId = selectedContractorId;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the selected contractor when an assignment is created.
    /// </summary>
    public void UpdateSelection(Guid contractorId, string actor)
    {
        if (contractorId == Guid.Empty)
            throw new ArgumentException("Contractor ID cannot be empty.", nameof(contractorId));

        if (string.IsNullOrWhiteSpace(actor))
            throw new ArgumentException("Actor cannot be empty.", nameof(actor));

        SelectedContractorId = contractorId;
        SelectionActor = actor;
    }
}

/// <summary>
/// Represents a candidate score in the audit trail.
/// </summary>
public class CandidateScore
{
    public Guid ContractorId { get; init; }
    public double FinalScore { get; init; }
    public ScoreBreakdownData PerFactorScores { get; init; } = null!;
    public string Rationale { get; init; } = string.Empty;
    public bool WasSelected { get; init; }
}

/// <summary>
/// Score breakdown data for audit trail.
/// </summary>
public class ScoreBreakdownData
{
    public double Availability { get; init; }
    public double Rating { get; init; }
    public double Distance { get; init; }
    public double? Rotation { get; init; }
}


