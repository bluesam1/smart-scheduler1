using Microsoft.Extensions.Logging;

namespace SmartScheduler.Application.Contracts.Services;

/// <summary>
/// Service for processing multiple distance/ETA requests in batches efficiently.
/// Uses parallel processing and optimal batching strategies.
/// </summary>
public class BatchDistanceProcessor : IBatchDistanceProcessor
{
    private readonly SmartScheduler.Application.Contracts.Services.IDistanceService _distanceService;
    private readonly IETAMatrixService _etaMatrixService;
    private readonly ILogger<BatchDistanceProcessor> _logger;
    private const int OptimalBatchSize = 25; // ORS matrix API limit
    private const int MaxConcurrentBatches = 4; // Limit concurrent API calls

    public BatchDistanceProcessor(
        IDistanceService distanceService,
        IETAMatrixService etaMatrixService,
        ILogger<BatchDistanceProcessor> logger)
    {
        _distanceService = distanceService;
        _etaMatrixService = etaMatrixService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Dictionary<int, (double? DistanceMeters, int? EtaMinutes)>> ProcessBatchAsync(
        IReadOnlyList<DistanceRequest> requests,
        CancellationToken cancellationToken = default)
    {
        if (requests.Count == 0)
        {
            return new Dictionary<int, (double?, int?)>();
        }

        var results = new Dictionary<int, (double?, int?)>();

        // Group requests into batches for matrix API
        var batches = CreateBatches(requests, OptimalBatchSize);

        // Process batches with concurrency limit
        var semaphore = new SemaphoreSlim(MaxConcurrentBatches);
        var batchTasks = batches.Select(async (batch, batchIndex) =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await ProcessBatch(batch, results, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(batchTasks);

        return results;
    }

    /// <summary>
    /// Creates batches from requests for optimal processing.
    /// </summary>
    private List<List<(int Index, DistanceRequest Request)>> CreateBatches(
        IReadOnlyList<DistanceRequest> requests,
        int batchSize)
    {
        var batches = new List<List<(int, DistanceRequest)>>();
        var currentBatch = new List<(int, DistanceRequest)>();

        for (int i = 0; i < requests.Count; i++)
        {
            currentBatch.Add((i, requests[i]));

            if (currentBatch.Count >= batchSize)
            {
                batches.Add(currentBatch);
                currentBatch = new List<(int, DistanceRequest)>();
            }
        }

        if (currentBatch.Count > 0)
        {
            batches.Add(currentBatch);
        }

        return batches;
    }

    /// <summary>
    /// Processes a single batch of requests.
    /// </summary>
    private async Task ProcessBatch(
        List<(int Index, DistanceRequest Request)> batch,
        Dictionary<int, (double?, int?)> results,
        CancellationToken cancellationToken)
    {
        try
        {
            // Extract unique origins and destinations for matrix API
            var origins = batch.Select(b => (b.Request.OriginLat, b.Request.OriginLng)).Distinct().ToList();
            var destinations = batch.Select(b => (b.Request.DestinationLat, b.Request.DestinationLng)).Distinct().ToList();

            // Try matrix API first (most efficient)
            var matrixResult = await _etaMatrixService.CalculateETAsAsync(origins, destinations, cancellationToken);

            if (matrixResult != null)
            {
                // Map matrix results back to requests
                foreach (var (index, request) in batch)
                {
                    var originIndex = origins.IndexOf((request.OriginLat, request.OriginLng));
                    var destIndex = destinations.IndexOf((request.DestinationLat, request.DestinationLng));

                    if (originIndex >= 0 && destIndex >= 0 &&
                        matrixResult.TryGetValue((originIndex, destIndex), out var eta))
                    {
                        // Get distance separately (matrix API may not provide distance)
                        var distance = await _distanceService.GetDistanceAsync(
                            request.OriginLat, request.OriginLng,
                            request.DestinationLat, request.DestinationLng,
                            cancellationToken);

                        lock (results)
                        {
                            results[index] = (distance, eta);
                        }
                    }
                    else
                    {
                        // Fallback to individual requests
                        await ProcessIndividualRequest(index, request, results, cancellationToken);
                    }
                }
            }
            else
            {
                // Fallback to individual requests for entire batch
                _logger.LogWarning("Matrix API failed for batch, falling back to individual requests");
                var individualTasks = batch.Select(b => 
                    ProcessIndividualRequest(b.Index, b.Request, results, cancellationToken));
                await Task.WhenAll(individualTasks);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch, falling back to individual requests");
            var individualTasks = batch.Select(b => 
                ProcessIndividualRequest(b.Index, b.Request, results, cancellationToken));
            await Task.WhenAll(individualTasks);
        }
    }

    /// <summary>
    /// Processes a single request individually.
    /// </summary>
    private async Task ProcessIndividualRequest(
        int index,
        DistanceRequest request,
        Dictionary<int, (double?, int?)> results,
        CancellationToken cancellationToken)
    {
        try
        {
            var distanceTask = _distanceService.GetDistanceAsync(
                request.OriginLat, request.OriginLng,
                request.DestinationLat, request.DestinationLng,
                cancellationToken);

            var etaTask = _distanceService.GetEtaAsync(
                request.OriginLat, request.OriginLng,
                request.DestinationLat, request.DestinationLng,
                cancellationToken);

            await Task.WhenAll(distanceTask, etaTask);

            var distance = await distanceTask;
            var eta = await etaTask;

            lock (results)
            {
                results[index] = (distance, eta);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing individual request {Index}", index);
            lock (results)
            {
                results[index] = (null, null);
            }
        }
    }
}

