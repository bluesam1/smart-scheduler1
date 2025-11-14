/**
 * SignalR Client Service
 * 
 * Manages SignalR connection, group subscriptions, and event handling.
 * This service provides a clean interface for connecting to the SignalR hub
 * and subscribing to real-time events.
 */

import * as signalR from "@microsoft/signalr";
import type { 
  RecommendationReady, 
  JobAssigned, 
  JobRescheduled,
  JobCancelled,
  RecommendationReadyHandler, 
  JobAssignedHandler,
  JobRescheduledHandler,
  JobCancelledHandler
} from "./signalr-types";

export interface SignalRClientOptions {
  /** SignalR hub URL (defaults to NEXT_PUBLIC_SIGNALR_URL or NEXT_PUBLIC_API_URL + /hub/recommendations) */
  hubUrl?: string;
  /** Function to get JWT access token */
  getAccessToken: () => string | null;
  /** Function to refresh access token */
  refreshToken?: () => Promise<string | null>;
  /** Enable automatic reconnection (default: true) */
  automaticReconnect?: boolean;
}

export class SignalRClient {
  private connection: signalR.HubConnection | null = null;
  private options: SignalRClientOptions;
  private isConnecting = false;
  private isDisposed = false;
  
  // Event handlers
  private recommendationReadyHandlers: RecommendationReadyHandler[] = [];
  private jobAssignedHandlers: JobAssignedHandler[] = [];
  private jobRescheduledHandlers: JobRescheduledHandler[] = [];
  private jobCancelledHandlers: JobCancelledHandler[] = [];
  
  // Group membership tracking
  private dispatchGroups: Set<string> = new Set();
  private contractorGroups: Set<string> = new Set();

  constructor(options: SignalRClientOptions) {
    this.options = {
      automaticReconnect: true,
      ...options,
    };
  }

  /**
   * Gets the SignalR hub URL from environment variables or options
   */
  private getHubUrl(): string {
    if (this.options.hubUrl) {
      return this.options.hubUrl;
    }

    // Try NEXT_PUBLIC_SIGNALR_URL first
    const signalrUrl = process.env.NEXT_PUBLIC_SIGNALR_URL;
    if (signalrUrl) {
      // If it already includes /hub/recommendations or /hubs/recommendations, use as-is
      if (signalrUrl.includes("/hub/recommendations") || signalrUrl.includes("/hubs/recommendations")) {
        return signalrUrl;
      }
      // If it ends with /hubs, append /recommendations
      if (signalrUrl.endsWith("/hubs")) {
        return `${signalrUrl}/recommendations`;
      }
      // If it's just the base URL, append /hub/recommendations
      return `${signalrUrl}/hub/recommendations`;
    }

    // Fall back to API URL + /hub/recommendations
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5004";
    return `${apiUrl}/hub/recommendations`;
  }

  /**
   * Checks if a JWT token is expired or will expire soon (within 60 seconds)
   */
  private isTokenExpiredOrExpiringSoon(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const exp = payload.exp;
      if (!exp) {
        return true; // No expiration claim, treat as expired
      }
      // Check if token expires within 60 seconds
      const expirationTime = exp * 1000;
      const now = Date.now();
      return expirationTime <= (now + 60000);
    } catch (error) {
      console.error("Error parsing token:", error);
      return true; // If we can't parse it, treat as expired
    }
  }

  /**
   * Gets a valid access token, refreshing if necessary
   */
  private async getValidAccessToken(): Promise<string> {
    let token = this.options.getAccessToken();
    
    // If no token, try to refresh
    if (!token && this.options.refreshToken) {
      token = await this.options.refreshToken();
    }
    
    if (!token) {
      throw new Error("No access token available for SignalR connection");
    }
    
    // Check if token is expired or expiring soon, and refresh if needed
    if (this.isTokenExpiredOrExpiringSoon(token) && this.options.refreshToken) {
      const refreshedToken = await this.options.refreshToken();
      if (refreshedToken) {
        token = refreshedToken;
      }
    }
    
    return token;
  }

  /**
   * Creates and configures the SignalR connection
   */
  private createConnection(): signalR.HubConnection {
    const hubUrl = this.getHubUrl();
    
    const builder = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: async () => {
          return await this.getValidAccessToken();
        },
        withCredentials: true,
      });

    if (this.options.automaticReconnect) {
      builder.withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0s, 2s, 10s, 30s, then 30s intervals
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount === 1) return 2000;
          if (retryContext.previousRetryCount === 2) return 10000;
          return 30000;
        },
      });
    }

    const connection = builder.build();

    // Set up event handlers
    this.setupEventHandlers(connection);

    return connection;
  }

  /**
   * Sets up SignalR event handlers
   */
  private setupEventHandlers(connection: signalR.HubConnection): void {
    // RecommendationReady event
    connection.on("RecommendationReady", (payload: RecommendationReady) => {
      this.recommendationReadyHandlers.forEach((handler) => {
        try {
          handler(payload);
        } catch (error) {
          console.error("Error in RecommendationReady handler:", error);
        }
      });
    });

    // JobAssigned event
    connection.on("JobAssigned", (payload: JobAssigned) => {
      this.jobAssignedHandlers.forEach((handler) => {
        try {
          handler(payload);
        } catch (error) {
          console.error("Error in JobAssigned handler:", error);
        }
      });
    });

    // JobRescheduled event
    connection.on("JobRescheduled", (payload: JobRescheduled) => {
      this.jobRescheduledHandlers.forEach((handler) => {
        try {
          handler(payload);
        } catch (error) {
          console.error("Error in JobRescheduled handler:", error);
        }
      });
    });

    // JobCancelled event
    connection.on("JobCancelled", (payload: JobCancelled) => {
      this.jobCancelledHandlers.forEach((handler) => {
        try {
          handler(payload);
        } catch (error) {
          console.error("Error in JobCancelled handler:", error);
        }
      });
    });

    // Connection lifecycle events
    connection.onclose((error) => {
      if (error) {
        console.error("SignalR connection closed with error:", error);
      } else {
        console.log("SignalR connection closed");
      }
    });

    connection.onreconnecting((error) => {
      console.log("SignalR reconnecting...", error);
    });

    connection.onreconnected(async (connectionId) => {
      console.log("SignalR reconnected:", connectionId);
      // Rejoin groups after reconnection
      await this.rejoinGroups();
    });
  }

  /**
   * Rejoins all groups after reconnection
   */
  private async rejoinGroups(): Promise<void> {
    if (!this.connection) return;

    try {
      // Rejoin dispatch groups
      for (const region of this.dispatchGroups) {
        await this.connection.invoke("JoinDispatchGroup", region);
      }

      // Rejoin contractor groups
      for (const contractorId of this.contractorGroups) {
        await this.connection.invoke("JoinContractorGroup", contractorId);
      }
    } catch (error) {
      console.error("Error rejoining groups after reconnection:", error);
    }
  }

  /**
   * Connects to the SignalR hub
   */
  async connect(): Promise<void> {
    if (this.isDisposed) {
      throw new Error("SignalR client has been disposed");
    }

    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return; // Already connected
    }

    if (this.isConnecting) {
      return; // Connection in progress
    }

    this.isConnecting = true;

    try {
      // Validate token before attempting connection
      try {
        await this.getValidAccessToken();
      } catch (tokenError) {
        console.error("Failed to get valid access token for SignalR:", tokenError);
        throw new Error("Cannot connect to SignalR: No valid access token available. Please log in again.");
      }

      // Create connection if it doesn't exist
      if (!this.connection) {
        this.connection = this.createConnection();
      }

      // Start connection
      await this.connection.start();
      console.log("SignalR connected successfully");

      // Rejoin groups if we have any
      await this.rejoinGroups();
    } catch (error) {
      console.error("Failed to connect to SignalR:", error);
      throw error;
    } finally {
      this.isConnecting = false;
    }
  }

  /**
   * Disconnects from the SignalR hub
   */
  async disconnect(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
        console.log("SignalR disconnected");
      } catch (error) {
        console.error("Error disconnecting from SignalR:", error);
      }
    }
  }

  /**
   * Joins a dispatcher group for a region
   */
  async joinDispatchGroup(region: string): Promise<void> {
    if (!this.connection) {
      throw new Error("SignalR connection not established. Call connect() first.");
    }

    if (this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error("SignalR connection not connected. Current state: " + this.connection.state);
    }

    try {
      await this.connection.invoke("JoinDispatchGroup", region);
      this.dispatchGroups.add(region);
      console.log(`Joined dispatch group: ${region}`);
    } catch (error) {
      console.error(`Error joining dispatch group ${region}:`, error);
      throw error;
    }
  }

  /**
   * Leaves a dispatcher group for a region
   */
  async leaveDispatchGroup(region: string): Promise<void> {
    if (!this.connection) {
      return;
    }

    if (this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await this.connection.invoke("LeaveDispatchGroup", region);
      this.dispatchGroups.delete(region);
      console.log(`Left dispatch group: ${region}`);
    } catch (error) {
      console.error(`Error leaving dispatch group ${region}:`, error);
    }
  }

  /**
   * Joins a contractor group
   */
  async joinContractorGroup(contractorId: string): Promise<void> {
    if (!this.connection) {
      throw new Error("SignalR connection not established. Call connect() first.");
    }

    if (this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error("SignalR connection not connected. Current state: " + this.connection.state);
    }

    try {
      await this.connection.invoke("JoinContractorGroup", contractorId);
      this.contractorGroups.add(contractorId);
      console.log(`Joined contractor group: ${contractorId}`);
    } catch (error) {
      console.error(`Error joining contractor group ${contractorId}:`, error);
      throw error;
    }
  }

  /**
   * Leaves a contractor group
   */
  async leaveContractorGroup(contractorId: string): Promise<void> {
    if (!this.connection) {
      return;
    }

    if (this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      await this.connection.invoke("LeaveContractorGroup", contractorId);
      this.contractorGroups.delete(contractorId);
      console.log(`Left contractor group: ${contractorId}`);
    } catch (error) {
      console.error(`Error leaving contractor group ${contractorId}:`, error);
    }
  }

  /**
   * Subscribes to RecommendationReady events
   */
  onRecommendationReady(handler: RecommendationReadyHandler): () => void {
    this.recommendationReadyHandlers.push(handler);
    
    // Return unsubscribe function
    return () => {
      const index = this.recommendationReadyHandlers.indexOf(handler);
      if (index > -1) {
        this.recommendationReadyHandlers.splice(index, 1);
      }
    };
  }

  /**
   * Subscribes to JobAssigned events
   */
  onJobAssigned(handler: JobAssignedHandler): () => void {
    this.jobAssignedHandlers.push(handler);
    
    // Return unsubscribe function
    return () => {
      const index = this.jobAssignedHandlers.indexOf(handler);
      if (index > -1) {
        this.jobAssignedHandlers.splice(index, 1);
      }
    };
  }

  /**
   * Subscribes to JobRescheduled events
   */
  onJobRescheduled(handler: JobRescheduledHandler): () => void {
    this.jobRescheduledHandlers.push(handler);
    
    // Return unsubscribe function
    return () => {
      const index = this.jobRescheduledHandlers.indexOf(handler);
      if (index > -1) {
        this.jobRescheduledHandlers.splice(index, 1);
      }
    };
  }

  /**
   * Subscribes to JobCancelled events
   */
  onJobCancelled(handler: JobCancelledHandler): () => void {
    this.jobCancelledHandlers.push(handler);
    
    // Return unsubscribe function
    return () => {
      const index = this.jobCancelledHandlers.indexOf(handler);
      if (index > -1) {
        this.jobCancelledHandlers.splice(index, 1);
      }
    };
  }

  /**
   * Gets the current connection state
   */
  getConnectionState(): signalR.HubConnectionState {
    return this.connection?.state ?? signalR.HubConnectionState.Disconnected;
  }

  /**
   * Checks if the connection is connected
   */
  isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }

  /**
   * Disposes the SignalR client and cleans up resources
   */
  async dispose(): Promise<void> {
    this.isDisposed = true;
    await this.disconnect();
    this.connection = null;
    this.recommendationReadyHandlers = [];
    this.jobAssignedHandlers = [];
    this.jobRescheduledHandlers = [];
    this.jobCancelledHandlers = [];
    this.dispatchGroups.clear();
    this.contractorGroups.clear();
  }
}

