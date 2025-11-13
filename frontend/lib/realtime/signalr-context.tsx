"use client"

/**
 * SignalR Context Provider
 * 
 * Provides SignalR client instance to React components via context.
 * Manages connection lifecycle and group subscriptions based on user role.
 */

import React, { createContext, useContext, useEffect, useRef, useState, useCallback } from "react";
import { useAuth } from "../auth/auth-context";
import { SignalRClient, type SignalRClientOptions } from "./signalr-client";
import type { SignalRConnectionState } from "./signalr-types";
import * as signalR from "@microsoft/signalr";

interface SignalRContextType {
  client: SignalRClient | null;
  connectionState: SignalRConnectionState;
  isConnected: boolean;
  connect: () => Promise<void>;
  disconnect: () => Promise<void>;
  joinDispatchGroup: (region: string) => Promise<void>;
  leaveDispatchGroup: (region: string) => Promise<void>;
  joinContractorGroup: (contractorId: string) => Promise<void>;
  leaveContractorGroup: (contractorId: string) => Promise<void>;
}

const SignalRContext = createContext<SignalRContextType | undefined>(undefined);

interface SignalRProviderProps {
  children: React.ReactNode;
  /** Optional region for dispatcher (if user is a dispatcher) */
  dispatcherRegion?: string;
  /** Optional contractor ID (if user is a contractor) */
  contractorId?: string;
  /** Auto-connect when authenticated (default: true) */
  autoConnect?: boolean;
}

export function SignalRProvider({
  children,
  dispatcherRegion,
  contractorId,
  autoConnect = true,
}: SignalRProviderProps) {
  const { isAuthenticated, accessToken, getTokenProvider } = useAuth();
  const clientRef = useRef<SignalRClient | null>(null);
  const [connectionState, setConnectionState] = useState<SignalRConnectionState>("Disconnected");
  const [isConnected, setIsConnected] = useState(false);

  // Update connection state periodically
  useEffect(() => {
    if (!clientRef.current) return;

    const interval = setInterval(() => {
      const state = clientRef.current?.getConnectionState() ?? signalR.HubConnectionState.Disconnected;
      const connected = clientRef.current?.isConnected() ?? false;
      
      setConnectionState(
        state === signalR.HubConnectionState.Connected
          ? "Connected"
          : state === signalR.HubConnectionState.Connecting
          ? "Connecting"
          : state === signalR.HubConnectionState.Reconnecting
          ? "Reconnecting"
          : state === signalR.HubConnectionState.Disconnecting
          ? "Disconnecting"
          : "Disconnected"
      );
      setIsConnected(connected);
    }, 1000);

    return () => clearInterval(interval);
  }, []);

  // Helper function to check if API is available
  const checkApiAvailability = useCallback(async (): Promise<boolean> => {
    try {
      const apiUrl = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5004";
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 2000); // 2 second timeout
      
      try {
        const response = await fetch(`${apiUrl}/health`, {
          method: "GET",
          signal: controller.signal,
        });
        clearTimeout(timeoutId);
        return response.ok;
      } catch (error) {
        clearTimeout(timeoutId);
        if (error instanceof Error && error.name === "AbortError") {
          // Timeout - API not responding
          return false;
        }
        throw error;
      }
    } catch (error) {
      return false;
    }
  }, []);

  // Create SignalR client when authenticated
  useEffect(() => {
    if (!isAuthenticated || !accessToken) {
      // Clean up client if not authenticated
      if (clientRef.current) {
        clientRef.current.dispose();
        clientRef.current = null;
        setConnectionState("Disconnected");
        setIsConnected(false);
      }
      return;
    }

    // Create client if it doesn't exist
    if (!clientRef.current) {
      const tokenProvider = getTokenProvider();
      if (!tokenProvider) {
        return;
      }

      const options: SignalRClientOptions = {
        getAccessToken: () => tokenProvider.getToken(),
        refreshToken: tokenProvider.refreshToken,
        automaticReconnect: true,
      };

      clientRef.current = new SignalRClient(options);
    }

    // Auto-connect if enabled (only if client exists and not already connected)
    if (autoConnect && clientRef.current && !isConnected) {
      // Use setTimeout to avoid calling during render
      const timeoutId = setTimeout(async () => {
        // Check if API is available before attempting connection
        const apiAvailable = await checkApiAvailability();
        if (!apiAvailable) {
          // Silently skip connection if API is not available
          if (process.env.NODE_ENV === "development") {
            console.debug("SignalR: API not available, skipping connection");
          }
          return;
        }

        connect().catch((err) => {
          // Already handled in connect, just prevent unhandled rejection
          console.warn("SignalR auto-connect failed:", err);
        });
      }, 100);
      
      return () => clearTimeout(timeoutId);
    }

    // Cleanup on unmount
    return () => {
      if (clientRef.current) {
        clientRef.current.dispose();
        clientRef.current = null;
      }
    };
  }, [isAuthenticated, accessToken, autoConnect, isConnected, getTokenProvider, checkApiAvailability]);

  // Auto-join groups when connected
  useEffect(() => {
    if (!isConnected || !clientRef.current) return;

    const joinGroups = async () => {
      try {
        if (dispatcherRegion) {
          await clientRef.current!.joinDispatchGroup(dispatcherRegion);
        }
        if (contractorId) {
          await clientRef.current!.joinContractorGroup(contractorId);
        }
      } catch (error) {
        console.error("Error joining SignalR groups:", error);
      }
    };

    joinGroups();
  }, [isConnected, dispatcherRegion, contractorId]);

  const connect = useCallback(async () => {
    if (!clientRef.current) {
      console.warn("SignalR client not initialized");
      return;
    }

    // Check if API is available before attempting connection
    const apiAvailable = await checkApiAvailability();
    if (!apiAvailable) {
      if (process.env.NODE_ENV === "development") {
        console.debug("SignalR: API not available, skipping connection");
      }
      return;
    }

    try {
      await clientRef.current.connect();
      setConnectionState("Connected");
      setIsConnected(true);
    } catch (error) {
      // Log error but don't throw - allow app to continue without SignalR
      console.warn("Failed to connect to SignalR (app will continue without real-time updates):", error);
      setConnectionState("Disconnected");
      setIsConnected(false);
      // Don't throw - SignalR is optional for app functionality
    }
  }, [checkApiAvailability]);

  const disconnect = useCallback(async () => {
    if (clientRef.current) {
      await clientRef.current.disconnect();
      setConnectionState("Disconnected");
      setIsConnected(false);
    }
  }, []);

  const joinDispatchGroup = useCallback(async (region: string) => {
    if (!clientRef.current) {
      throw new Error("SignalR client not initialized");
    }
    await clientRef.current.joinDispatchGroup(region);
  }, []);

  const leaveDispatchGroup = useCallback(async (region: string) => {
    if (clientRef.current) {
      await clientRef.current.leaveDispatchGroup(region);
    }
  }, []);

  const joinContractorGroup = useCallback(async (contractorId: string) => {
    if (!clientRef.current) {
      throw new Error("SignalR client not initialized");
    }
    await clientRef.current.joinContractorGroup(contractorId);
  }, []);

  const leaveContractorGroup = useCallback(async (contractorId: string) => {
    if (clientRef.current) {
      await clientRef.current.leaveContractorGroup(contractorId);
    }
  }, []);

  const value: SignalRContextType = {
    client: clientRef.current,
    connectionState,
    isConnected,
    connect,
    disconnect,
    joinDispatchGroup,
    leaveDispatchGroup,
    joinContractorGroup,
    leaveContractorGroup,
  };

  return <SignalRContext.Provider value={value}>{children}</SignalRContext.Provider>;
}

export function useSignalR() {
  const context = useContext(SignalRContext);
  if (!context) {
    throw new Error("useSignalR must be used within a SignalRProvider");
  }
  return context;
}

