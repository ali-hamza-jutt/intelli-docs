"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  API_BASE_URL,
  setAccessToken,
  setAuthExpiredHandler,
} from "@/lib/api/client";
import type { AuthResponse, UserResponse } from "@/lib/api/model";

type AuthState = {
  user: UserResponse | null;
  /** True until the initial refresh attempt settles — used to avoid flashing the login screen. */
  isLoading: boolean;
  isAuthenticated: boolean;
  /** Stores the session returned by login or register. */
  signIn: (auth: AuthResponse) => void;
  signOut: () => Promise<void>;
  setUser: (user: UserResponse) => void;
};

const AuthContext = createContext<AuthState | null>(null);

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used inside <AuthProvider>.");
  }

  return context;
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUserState] = useState<UserResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const queryClient = useQueryClient();

  const clearSession = useCallback(() => {
    setAccessToken(null);
    setUserState(null);
    // Drops every cached query so the next user never sees the previous one's data.
    queryClient.clear();
  }, [queryClient]);

  const signIn = useCallback((auth: AuthResponse) => {
    setAccessToken(auth.accessToken);
    setUserState(auth.user);
  }, []);

  const signOut = useCallback(async () => {
    try {
      // Revokes the refresh token server-side and clears the cookie.
      await fetch(`${API_BASE_URL}/api/auth/logout`, {
        method: "POST",
        credentials: "include",
      });
    } catch {
      // A failed logout call still signs the user out locally.
    }

    clearSession();
  }, [clearSession]);

  /*
   * On first load the access token is gone — it only ever lived in memory. The refresh cookie
   * survives, so try to exchange it for a new session before deciding the user is signed out.
   */
  useEffect(() => {
    let cancelled = false;

    const restore = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
          method: "POST",
          credentials: "include",
        });

        if (!response.ok) throw new Error("no session");

        const auth = (await response.json()) as AuthResponse;

        if (!cancelled) {
          setAccessToken(auth.accessToken);
          setUserState(auth.user);
        }
      } catch {
        if (!cancelled) {
          setAccessToken(null);
          setUserState(null);
        }
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    };

    restore();

    return () => {
      cancelled = true;
    };
  }, []);

  // Lets the fetch wrapper drop the session when a refresh fails mid-flight.
  useEffect(() => {
    setAuthExpiredHandler(clearSession);
    return () => setAuthExpiredHandler(null);
  }, [clearSession]);

  const value = useMemo<AuthState>(
    () => ({
      user,
      isLoading,
      isAuthenticated: user !== null,
      signIn,
      signOut,
      setUser: setUserState,
    }),
    [user, isLoading, signIn, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
