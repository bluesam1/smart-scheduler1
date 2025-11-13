/**
 * Unit tests for recommendations transformation logic
 * Tests the transformation from API response to component format
 */

describe('Recommendations Transformation', () => {
  // Mock API recommendation
  const createMockApiRecommendation = (suggestedSlots: any[] = []) => ({
    contractorId: '123e4567-e89b-12d3-a456-426614174000',
    contractorName: 'John Doe',
    contractorBaseLocation: '456 Oak Ave, Brooklyn, NY',
    score: 85.5,
    scoreBreakdown: {
      availability: 90,
      rating: 75,
      distance: 80,
      rotation: 95,
    },
    rationale: 'Strong availability and good rating',
    suggestedSlots,
    distance: 5000, // meters
    eta: 15, // minutes
  })

  // Transform function (extracted from recommendations-sheet.tsx)
  const transformRecommendation = (apiRec: any, index: number) => {
    console.log(`[Test] Transforming recommendation ${index + 1}:`, {
      contractorId: apiRec.contractorId,
      contractorName: apiRec.contractorName,
      suggestedSlotsCount: apiRec.suggestedSlots?.length || 0,
      suggestedSlots: apiRec.suggestedSlots
    })

    // Format time slots - handle empty/missing slots gracefully
    const slots = (apiRec.suggestedSlots || []).map((slot: any) => {
      const startDate = new Date(slot.startUtc)
      const endDate = new Date(slot.endUtc)
      const timeStr = startDate.toLocaleTimeString("en-US", {
        hour: "numeric",
        minute: "2-digit",
        hour12: true,
      })
      
      let label = slot.type
      if (slot.type === "earliest") label = "Earliest"
      else if (slot.type === "lowest-travel") label = "Lowest Travel"
      else if (slot.type === "highest-confidence") label = "Best Fit"
      
      let confidence = "Medium"
      if (slot.confidence >= 80) confidence = "High"
      else if (slot.confidence < 60) confidence = "Low"
      
      return {
        time: timeStr,
        startUtc: slot.startUtc,
        endUtc: slot.endUtc,
        label,
        confidence,
      }
    })
    
    if (slots.length === 0) {
      console.warn(`[Test] No suggested slots for contractor ${apiRec.contractorName} (${apiRec.contractorId})`)
    }

    // Format distance
    const distanceMiles = (apiRec.distance / 1609.34).toFixed(1) // meters to miles
    const travelTime = `${apiRec.eta} min`

    return {
      contractorId: apiRec.contractorId,
      contractorName: apiRec.contractorName,
      baseLocation: apiRec.contractorBaseLocation || "Location not available",
      rating: Math.round(apiRec.scoreBreakdown.rating),
      totalScore: Math.round(apiRec.score),
      scores: {
        availability: Math.round(apiRec.scoreBreakdown.availability),
        rating: Math.round(apiRec.scoreBreakdown.rating),
        distance: Math.round(apiRec.scoreBreakdown.distance),
        rotation: apiRec.scoreBreakdown.rotation ? Math.round(apiRec.scoreBreakdown.rotation) : 0,
      },
      travelTime,
      travelDistance: `${distanceMiles} miles`,
      rationale: apiRec.rationale,
      suggestedSlots: slots,
      currentUtilization: 0,
      jobsToday: 0,
      skills: [],
    }
  }

  test('transforms recommendation with suggested slots correctly', () => {
    // Arrange
    const apiRec = createMockApiRecommendation([
      {
        startUtc: '2025-01-20T14:30:00Z',
        endUtc: '2025-01-20T16:30:00Z',
        type: 'earliest',
        confidence: 85,
      },
      {
        startUtc: '2025-01-20T15:00:00Z',
        endUtc: '2025-01-20T17:00:00Z',
        type: 'lowest-travel',
        confidence: 75,
      },
    ])

    // Act
    const result = transformRecommendation(apiRec, 0)

    // Assert
    expect(result.contractorId).toBe('123e4567-e89b-12d3-a456-426614174000')
    expect(result.contractorName).toBe('John Doe')
    expect(result.totalScore).toBe(86) // Math.round(85.5)
    expect(result.suggestedSlots).toHaveLength(2)
    
    // Check first slot
    expect(result.suggestedSlots[0].label).toBe('Earliest')
    expect(result.suggestedSlots[0].confidence).toBe('High') // 85 >= 80
    expect(result.suggestedSlots[0].startUtc).toBe('2025-01-20T14:30:00Z')
    
    // Check second slot
    expect(result.suggestedSlots[1].label).toBe('Lowest Travel')
    expect(result.suggestedSlots[1].confidence).toBe('Medium') // 60 <= 75 < 80
  })

  test('handles empty suggested slots array', () => {
    // Arrange
    const apiRec = createMockApiRecommendation([]) // Empty array

    // Act
    const result = transformRecommendation(apiRec, 0)

    // Assert
    expect(result.suggestedSlots).toBeDefined()
    expect(result.suggestedSlots).toHaveLength(0)
    expect(Array.isArray(result.suggestedSlots)).toBe(true)
  })

  test('handles missing suggested slots property', () => {
    // Arrange
    const apiRec = {
      ...createMockApiRecommendation(),
      suggestedSlots: undefined, // Missing property
    }

    // Act
    const result = transformRecommendation(apiRec, 0)

    // Assert
    expect(result.suggestedSlots).toBeDefined()
    expect(result.suggestedSlots).toHaveLength(0)
    expect(Array.isArray(result.suggestedSlots)).toBe(true)
  })

  test('handles null suggested slots', () => {
    // Arrange
    const apiRec = {
      ...createMockApiRecommendation(),
      suggestedSlots: null, // Null value
    }

    // Act
    const result = transformRecommendation(apiRec, 0)

    // Assert
    expect(result.suggestedSlots).toBeDefined()
    expect(result.suggestedSlots).toHaveLength(0)
  })

  test('correctly converts slot types', () => {
    // Arrange
    const apiRec = createMockApiRecommendation([
      { startUtc: '2025-01-20T14:00:00Z', endUtc: '2025-01-20T16:00:00Z', type: 'earliest', confidence: 90 },
      { startUtc: '2025-01-20T15:00:00Z', endUtc: '2025-01-20T17:00:00Z', type: 'lowest-travel', confidence: 85 },
      { startUtc: '2025-01-20T16:00:00Z', endUtc: '2025-01-20T18:00:00Z', type: 'highest-confidence', confidence: 95 },
      { startUtc: '2025-01-20T17:00:00Z', endUtc: '2025-01-20T19:00:00Z', type: 'unknown-type', confidence: 70 },
    ])

    // Act
    const result = transformRecommendation(apiRec, 0)

    // Assert
    expect(result.suggestedSlots[0].label).toBe('Earliest')
    expect(result.suggestedSlots[1].label).toBe('Lowest Travel')
    expect(result.suggestedSlots[2].label).toBe('Best Fit')
    expect(result.suggestedSlots[3].label).toBe('unknown-type') // Unknown types pass through
  })

  test('correctly converts confidence levels', () => {
    // Arrange
    const apiRec = createMockApiRecommendation([
      { startUtc: '2025-01-20T14:00:00Z', endUtc: '2025-01-20T16:00:00Z', type: 'earliest', confidence: 95 },
      { startUtc: '2025-01-20T15:00:00Z', endUtc: '2025-01-20T17:00:00Z', type: 'earliest', confidence: 80 },
      { startUtc: '2025-01-20T16:00:00Z', endUtc: '2025-01-20T18:00:00Z', type: 'earliest', confidence: 75 },
      { startUtc: '2025-01-20T17:00:00Z', endUtc: '2025-01-20T19:00:00Z', type: 'earliest', confidence: 60 },
      { startUtc: '2025-01-20T18:00:00Z', endUtc: '2025-01-20T20:00:00Z', type: 'earliest', confidence: 50 },
    ])

    // Act
    const result = transformRecommendation(apiRec, 0)

    // Assert
    expect(result.suggestedSlots[0].confidence).toBe('High')  // >= 80
    expect(result.suggestedSlots[1].confidence).toBe('High')  // >= 80
    expect(result.suggestedSlots[2].confidence).toBe('Medium') // 60 <= x < 80
    expect(result.suggestedSlots[3].confidence).toBe('Medium') // 60 <= x < 80
    expect(result.suggestedSlots[4].confidence).toBe('Low')    // < 60
  })

  test('converts distance from meters to miles correctly', () => {
    // Arrange
    const apiRec = createMockApiRecommendation([])
    apiRec.distance = 8046.72 // 5 miles in meters

    // Act
    const result = transformRecommendation(apiRec, 0)

    // Assert
    expect(result.travelDistance).toBe('5.0 miles')
  })

  test('formats travel time correctly', () => {
    // Arrange
    const apiRec = createMockApiRecommendation([])
    apiRec.eta = 25

    // Act
    const result = transformRecommendation(apiRec, 0)

    // Assert
    expect(result.travelTime).toBe('25 min')
  })

  test('handles missing contractor base location', () => {
    // Arrange
    const apiRec = createMockApiRecommendation([])
    apiRec.contractorBaseLocation = undefined

    // Act
    const result = transformRecommendation(apiRec, 0)

    // Assert
    expect(result.baseLocation).toBe('Location not available')
  })

  test('rounds scores correctly', () => {
    // Arrange
    const apiRec = createMockApiRecommendation([])
    apiRec.score = 85.7
    apiRec.scoreBreakdown = {
      availability: 89.4,
      rating: 75.8,
      distance: 80.2,
      rotation: 94.6,
    }

    // Act
    const result = transformRecommendation(apiRec, 0)

    // Assert
    expect(result.totalScore).toBe(86)
    expect(result.scores.availability).toBe(89)
    expect(result.scores.rating).toBe(76)
    expect(result.scores.distance).toBe(80)
    expect(result.scores.rotation).toBe(95)
  })

  test('handles missing rotation score', () => {
    // Arrange
    const apiRec = createMockApiRecommendation([])
    apiRec.scoreBreakdown.rotation = undefined

    // Act
    const result = transformRecommendation(apiRec, 0)

    // Assert
    expect(result.scores.rotation).toBe(0)
  })
})

