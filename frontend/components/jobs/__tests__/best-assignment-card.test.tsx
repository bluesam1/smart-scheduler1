import { render, screen, waitFor, fireEvent } from '@testing-library/react'
import { BestAssignmentCard } from '../best-assignment-card'
import { useAuth } from '@/lib/auth/auth-context'
import { useSignalR } from '@/lib/realtime/signalr-context'
import { createApiClients } from '@/lib/api/api-client-config'
import { toast } from 'sonner'

// Mock dependencies
jest.mock('@/lib/auth/auth-context')
jest.mock('@/lib/realtime/signalr-context')
jest.mock('@/lib/api/api-client-config')
jest.mock('sonner')

const mockUseAuth = useAuth as jest.MockedFunction<typeof useAuth>
const mockUseSignalR = useSignalR as jest.MockedFunction<typeof useSignalR>
const mockCreateApiClients = createApiClients as jest.MockedFunction<typeof createApiClients>

describe('BestAssignmentCard', () => {
  const mockJobId = '123e4567-e89b-12d3-a456-426614174000'
  const mockTokenProvider = { getToken: jest.fn(() => 'mock-token') }
  
  beforeEach(() => {
    jest.clearAllMocks()
    
    mockUseAuth.mockReturnValue({
      getTokenProvider: () => mockTokenProvider,
      isAuthenticated: true,
      isLoading: false,
    } as any)
    
    mockUseSignalR.mockReturnValue({
      client: {
        on: jest.fn(),
        off: jest.fn(),
      },
      isConnected: true,
    } as any)
  })

  it('renders loading state initially', () => {
    const mockClient = {
      getLatestRecommendations: jest.fn().mockResolvedValue({
        recommendations: [],
      }),
    }
    
    mockCreateApiClients.mockReturnValue({ client: mockClient } as any)
    
    render(<BestAssignmentCard jobId={mockJobId} />)
    
    expect(screen.getByText(/loading best assignment/i)).toBeInTheDocument()
  })

  it('renders no recommendations message when none available', async () => {
    const mockClient = {
      getLatestRecommendations: jest.fn().mockRejectedValue({ status: 404 }),
    }
    
    mockCreateApiClients.mockReturnValue({ client: mockClient } as any)
    
    render(<BestAssignmentCard jobId={mockJobId} />)
    
    await waitFor(() => {
      expect(screen.getByText(/no recommendations calculated yet/i)).toBeInTheDocument()
    })
  })

  it('displays best recommendation when available', async () => {
    const mockRecommendation = {
      contractorId: 'contractor-123',
      contractorName: 'John Smith',
      score: 95,
      suggestedSlots: [
        {
          startUtc: '2025-01-15T14:00:00Z',
          endUtc: '2025-01-15T18:00:00Z',
          type: 'earliest',
          confidence: 85,
        },
      ],
      distance: 5000,
      eta: 15,
    }
    
    const mockClient = {
      getLatestRecommendations: jest.fn().mockResolvedValue({
        recommendations: [mockRecommendation],
      }),
    }
    
    mockCreateApiClients.mockReturnValue({ client: mockClient } as any)
    
    render(<BestAssignmentCard jobId={mockJobId} />)
    
    await waitFor(() => {
      expect(screen.getByText('John Smith')).toBeInTheDocument()
      expect(screen.getByText('95')).toBeInTheDocument()
      expect(screen.getByText(/15 min travel/i)).toBeInTheDocument()
    })
  })

  it('displays multi-day assignment correctly', async () => {
    const mockRecommendation = {
      contractorId: 'contractor-123',
      contractorName: 'Jane Doe',
      score: 88,
      suggestedSlots: [
        {
          startUtc: '2025-01-15T14:00:00Z',
          endUtc: '2025-01-17T18:00:00Z', // 3 days later
          type: 'earliest',
          confidence: 80,
        },
      ],
      distance: 3000,
      eta: 10,
    }
    
    const mockClient = {
      getLatestRecommendations: jest.fn().mockResolvedValue({
        recommendations: [mockRecommendation],
      }),
    }
    
    mockCreateApiClients.mockReturnValue({ client: mockClient } as any)
    
    render(<BestAssignmentCard jobId={mockJobId} />)
    
    await waitFor(() => {
      expect(screen.getByText('Jane Doe')).toBeInTheDocument()
      expect(screen.getByText(/multi-day assignment/i)).toBeInTheDocument()
    })
  })

  it('calls recalculate endpoint when refresh button clicked', async () => {
    const mockClient = {
      getLatestRecommendations: jest.fn().mockRejectedValue({ status: 404 }),
      recalculateRecommendations: jest.fn().mockResolvedValue({}),
    }
    
    mockCreateApiClients.mockReturnValue({ client: mockClient } as any)
    
    render(<BestAssignmentCard jobId={mockJobId} />)
    
    await waitFor(() => {
      expect(screen.getByText(/calculate now/i)).toBeInTheDocument()
    })
    
    const calculateButton = screen.getByText(/calculate now/i)
    fireEvent.click(calculateButton)
    
    await waitFor(() => {
      expect(mockClient.recalculateRecommendations).toHaveBeenCalledWith({
        jobId: mockJobId,
      })
    })
  })

  it('calls assignJob when schedule now button clicked', async () => {
    const mockRecommendation = {
      contractorId: 'contractor-123',
      contractorName: 'John Smith',
      score: 95,
      suggestedSlots: [
        {
          startUtc: '2025-01-15T14:00:00Z',
          endUtc: '2025-01-15T18:00:00Z',
          type: 'earliest',
          confidence: 85,
        },
      ],
      distance: 5000,
      eta: 15,
    }
    
    const mockClient = {
      getLatestRecommendations: jest.fn().mockResolvedValue({
        recommendations: [mockRecommendation],
      }),
      assignJob: jest.fn().mockResolvedValue({}),
    }
    
    mockCreateApiClients.mockReturnValue({ client: mockClient } as any)
    
    const onAssigned = jest.fn()
    render(<BestAssignmentCard jobId={mockJobId} onAssigned={onAssigned} />)
    
    await waitFor(() => {
      expect(screen.getByText(/schedule now/i)).toBeInTheDocument()
    })
    
    const scheduleButton = screen.getByText(/schedule now/i)
    fireEvent.click(scheduleButton)
    
    await waitFor(() => {
      expect(mockClient.assignJob).toHaveBeenCalledWith(mockJobId, {
        contractorId: 'contractor-123',
        startUtc: '2025-01-15T14:00:00Z',
        endUtc: '2025-01-15T18:00:00Z',
      })
      expect(onAssigned).toHaveBeenCalled()
    })
  })

  it('subscribes to SignalR RecommendationReady events', () => {
    const mockUnsubscribe = jest.fn()
    const mockOnRecommendationReady = jest.fn().mockReturnValue(mockUnsubscribe)
    
    mockUseSignalR.mockReturnValue({
      client: {
        onRecommendationReady: mockOnRecommendationReady,
      },
      isConnected: true,
    } as any)
    
    const mockClient = {
      getLatestRecommendations: jest.fn().mockResolvedValue({
        recommendations: [],
      }),
    }
    
    mockCreateApiClients.mockReturnValue({ client: mockClient } as any)
    
    const { unmount } = render(<BestAssignmentCard jobId={mockJobId} />)
    
    expect(mockOnRecommendationReady).toHaveBeenCalledWith(expect.any(Function))
    
    unmount()
    
    expect(mockUnsubscribe).toHaveBeenCalled()
  })

  it('refetches recommendations when SignalR event received for matching job', async () => {
    let signalRHandler: any
    
    const mockOnRecommendationReady = jest.fn((handler) => {
      signalRHandler = handler
      return jest.fn() // return unsubscribe function
    })
    
    mockUseSignalR.mockReturnValue({
      client: {
        onRecommendationReady: mockOnRecommendationReady,
      },
      isConnected: true,
    } as any)
    
    const mockClient = {
      getLatestRecommendations: jest.fn().mockResolvedValue({
        recommendations: [],
      }),
    }
    
    mockCreateApiClients.mockReturnValue({ client: mockClient } as any)
    
    render(<BestAssignmentCard jobId={mockJobId} />)
    
    // Wait for initial fetch
    await waitFor(() => {
      expect(mockClient.getLatestRecommendations).toHaveBeenCalledTimes(1)
    })
    
    // Trigger SignalR event
    signalRHandler({ jobId: mockJobId, requestId: 'request-123' })
    
    // Should trigger another fetch
    await waitFor(() => {
      expect(mockClient.getLatestRecommendations).toHaveBeenCalledTimes(2)
    })
  })

  it('does not refetch when SignalR event for different job', async () => {
    let signalRHandler: any
    
    const mockOnRecommendationReady = jest.fn((handler) => {
      signalRHandler = handler
      return jest.fn() // return unsubscribe function
    })
    
    mockUseSignalR.mockReturnValue({
      client: {
        onRecommendationReady: mockOnRecommendationReady,
      },
      isConnected: true,
    } as any)
    
    const mockClient = {
      getLatestRecommendations: jest.fn().mockResolvedValue({
        recommendations: [],
      }),
    }
    
    mockCreateApiClients.mockReturnValue({ client: mockClient } as any)
    
    render(<BestAssignmentCard jobId={mockJobId} />)
    
    // Wait for initial fetch
    await waitFor(() => {
      expect(mockClient.getLatestRecommendations).toHaveBeenCalledTimes(1)
    })
    
    // Trigger SignalR event for DIFFERENT job
    signalRHandler({ jobId: 'different-job-id', requestId: 'request-123' })
    
    // Should NOT trigger another fetch
    await waitFor(() => {
      expect(mockClient.getLatestRecommendations).toHaveBeenCalledTimes(1)
    })
  })
})

