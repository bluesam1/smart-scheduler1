"use client"

import { useState, useEffect, Suspense } from "react"
import { useRouter, useSearchParams } from "next/navigation"
import { LoginForm } from "@/components/auth/login-form"
import { ChangePasswordForm } from "@/components/auth/change-password-form"
import { AuthGuard } from "@/components/auth/auth-guard"
import { CognitoUser } from "amazon-cognito-identity-js"

function LoginContent() {
  const router = useRouter()
  const searchParams = useSearchParams()
  const [showPasswordChange, setShowPasswordChange] = useState(false)
  const [cognitoUser, setCognitoUser] = useState<CognitoUser | null>(null)
  const [userEmail, setUserEmail] = useState<string>("")
  const [redirectTo, setRedirectTo] = useState<string | null>(null)

  useEffect(() => {
    const redirect = searchParams.get("redirect")
    if (redirect) {
      setRedirectTo(redirect)
    }
  }, [searchParams])

  const handlePasswordChangeRequired = (user: CognitoUser, email: string) => {
    setCognitoUser(user)
    setUserEmail(email)
    setShowPasswordChange(true)
  }

  const handlePasswordChangeCancel = () => {
    setShowPasswordChange(false)
    setCognitoUser(null)
    setUserEmail("")
  }

  const handleLoginSuccess = () => {
    const destination = redirectTo || "/"
    router.push(destination)
    router.refresh()
  }

  return (
    <AuthGuard requireAuth={false}>
      {showPasswordChange && cognitoUser ? (
        <div className="flex min-h-screen items-center justify-center bg-background p-4">
          <ChangePasswordForm
            cognitoUser={cognitoUser}
            email={userEmail}
            onCancel={handlePasswordChangeCancel}
          />
        </div>
      ) : (
        <div className="flex min-h-screen items-center justify-center bg-background p-4">
          <LoginForm 
            onPasswordChangeRequired={handlePasswordChangeRequired}
            onLoginSuccess={handleLoginSuccess}
          />
        </div>
      )}
    </AuthGuard>
  )
}

export default function LoginPage() {
  return (
    <Suspense fallback={
      <div className="flex min-h-screen items-center justify-center bg-background">
        <div className="text-muted-foreground">Loading...</div>
      </div>
    }>
      <LoginContent />
    </Suspense>
  )
}

