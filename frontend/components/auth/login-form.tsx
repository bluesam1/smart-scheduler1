"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import * as z from "zod"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Spinner } from "@/components/ui/spinner"
import { useAuth } from "@/lib/auth/auth-context"
import { toast } from "sonner"
import { CognitoUser } from "amazon-cognito-identity-js"

const loginSchema = z.object({
  email: z.string().email("Please enter a valid email address"),
  password: z.string().min(1, "Password is required"),
})

type LoginFormData = z.infer<typeof loginSchema>

interface LoginFormProps {
  onPasswordChangeRequired?: (cognitoUser: CognitoUser, email: string) => void
  onLoginSuccess?: () => void
}

export function LoginForm({ onPasswordChangeRequired, onLoginSuccess }: LoginFormProps) {
  const router = useRouter()
  const { login } = useAuth()
  const [isLoading, setIsLoading] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors },
    trigger,
    getValues,
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    mode: "onChange",
  })

  const onSubmit = async (data: LoginFormData) => {
    console.log("Form submitted with data:", data)
    setIsLoading(true)
    try {
      console.log("Calling login function...")
      await login(data.email, data.password)
      console.log("Login successful")
      toast.success("Login successful")
      if (onLoginSuccess) {
        onLoginSuccess()
      } else {
        router.push("/")
        router.refresh()
      }
    } catch (error: any) {
      console.error("Login error:", error)
      setIsLoading(false)
      
      // Handle NEW_PASSWORD_REQUIRED challenge
      if (error.message === "NEW_PASSWORD_REQUIRED" && error.cognitoUser) {
        if (onPasswordChangeRequired) {
          onPasswordChangeRequired(error.cognitoUser, data.email)
        }
        return
      }

      // Handle other authentication errors
      let errorMessage = "Login failed. Please check your credentials."
      
      if (error.code === "NotAuthorizedException") {
        errorMessage = "Incorrect email or password."
      } else if (error.code === "UserNotConfirmedException") {
        errorMessage = "Your account is not confirmed. Please check your email."
      } else if (error.code === "UserNotFoundException") {
        errorMessage = "No account found with this email address."
      } else if (error.code === "TooManyRequestsException") {
        errorMessage = "Too many login attempts. Please try again later."
      } else if (error.message) {
        errorMessage = error.message
      }

      toast.error(errorMessage)
    }
  }

  const handleFormSubmit = handleSubmit(
    onSubmit,
    (errors) => {
      console.log("Form validation errors:", errors)
      toast.error("Please fix the form errors before submitting")
    }
  )

  const handleButtonClick = async (e: React.MouseEvent<HTMLButtonElement> | React.KeyboardEvent<HTMLInputElement>) => {
    console.log("Button/Enter key triggered")
    e.preventDefault()
    // Manually trigger validation and submit
    const isValid = await trigger()
    console.log("Form validation result:", isValid, "Errors:", errors)
    if (isValid) {
      const values = getValues()
      console.log("Submitting with values:", values)
      await onSubmit(values)
    } else {
      console.log("Form has validation errors, not submitting")
      toast.error("Please fix the form errors before submitting")
    }
  }

  return (
    <div className="flex flex-col items-center gap-8 w-full max-w-md">
      {/* SmartScheduler Logo */}
      <div className="flex items-center gap-3">
        <div className="w-14 h-14 bg-blue-600 rounded-xl"></div>
        <span className="text-2xl font-semibold text-foreground">SmartScheduler</span>
      </div>

      <Card className="w-full">
        <CardHeader>
          <CardTitle>Sign In</CardTitle>
          <CardDescription>Enter your email and password to access your account</CardDescription>
        </CardHeader>
      <CardContent>
        <form 
          onSubmit={handleFormSubmit} 
          className="space-y-4"
          noValidate
        >
          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              placeholder="you@example.com"
              autoComplete="email"
              aria-invalid={errors.email ? "true" : "false"}
              aria-describedby={errors.email ? "email-error" : undefined}
              {...register("email")}
            />
            {errors.email && (
              <p id="email-error" className="text-destructive text-sm" role="alert">
                {errors.email.message}
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="password">Password</Label>
            <Input
              id="password"
              type="password"
              placeholder="Enter your password"
              autoComplete="current-password"
              aria-invalid={errors.password ? "true" : "false"}
              aria-describedby={errors.password ? "password-error" : undefined}
              {...register("password")}
              onKeyDown={async (e) => {
                if (e.key === "Enter" && !isLoading) {
                  e.preventDefault()
                  await handleButtonClick(e as any)
                }
              }}
            />
            {errors.password && (
              <p id="password-error" className="text-destructive text-sm" role="alert">
                {errors.password.message}
              </p>
            )}
          </div>

          <Button 
            type="button"
            className="w-full" 
            disabled={isLoading}
            onClick={handleButtonClick}
          >
            {isLoading ? (
              <>
                <Spinner className="mr-2" />
                Signing in...
              </>
            ) : (
              "Sign In"
            )}
          </Button>
        </form>
      </CardContent>
    </Card>
    </div>
  )
}

