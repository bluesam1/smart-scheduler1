"use client"

import { useEffect } from "react"
import { useRouter, usePathname } from "next/navigation"
import { useAuth } from "@/lib/auth/auth-context"
import { Spinner } from "@/components/ui/spinner"

interface AuthGuardProps {
  children: React.ReactNode
  requireAuth?: boolean
}

/**
 * AuthGuard component that protects routes based on authentication status
 * 
 * @param children - The content to render if access is allowed
 * @param requireAuth - If true, redirects to login if not authenticated. If false, redirects to home if authenticated.
 */
export function AuthGuard({ children, requireAuth = true }: AuthGuardProps) {
  const router = useRouter()
  const pathname = usePathname()
  const { isAuthenticated, isLoading } = useAuth()

  useEffect(() => {
    if (isLoading) {
      return // Wait for auth state to load
    }

    if (requireAuth && !isAuthenticated) {
      // Redirect to login, preserving the intended destination
      const loginUrl = `/login?redirect=${encodeURIComponent(pathname)}`
      router.push(loginUrl)
    } else if (!requireAuth && isAuthenticated) {
      // If auth is not required but user is authenticated, redirect to home
      router.push("/")
    }
  }, [isAuthenticated, isLoading, requireAuth, router, pathname])

  // Show loading spinner while checking auth state
  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Spinner className="size-8" />
      </div>
    )
  }

  // If auth is required and user is not authenticated, don't render children
  // (redirect will happen in useEffect)
  if (requireAuth && !isAuthenticated) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Spinner className="size-8" />
      </div>
    )
  }

  // If auth is not required and user is authenticated, don't render children
  // (redirect will happen in useEffect)
  if (!requireAuth && isAuthenticated) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <Spinner className="size-8" />
      </div>
    )
  }

  return <>{children}</>
}

