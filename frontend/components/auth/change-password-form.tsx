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

// Cognito default password requirements:
// - Minimum 8 characters
// - At least one uppercase letter
// - At least one lowercase letter
// - At least one number
// - At least one special character
const passwordSchema = z
  .string()
  .min(8, "Password must be at least 8 characters")
  .regex(/[A-Z]/, "Password must contain at least one uppercase letter")
  .regex(/[a-z]/, "Password must contain at least one lowercase letter")
  .regex(/[0-9]/, "Password must contain at least one number")
  .regex(/[^A-Za-z0-9]/, "Password must contain at least one special character")

const changePasswordSchema = z
  .object({
    newPassword: passwordSchema,
    confirmPassword: z.string().min(1, "Please confirm your password"),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  })

type ChangePasswordFormData = z.infer<typeof changePasswordSchema>

interface ChangePasswordFormProps {
  cognitoUser: CognitoUser
  email: string
  onCancel?: () => void
}

export function ChangePasswordForm({ cognitoUser, email, onCancel }: ChangePasswordFormProps) {
  const router = useRouter()
  const { handleNewPasswordRequired } = useAuth()
  const [isLoading, setIsLoading] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors },
    watch,
  } = useForm<ChangePasswordFormData>({
    resolver: zodResolver(changePasswordSchema),
  })

  const newPassword = watch("newPassword")

  const onSubmit = async (data: ChangePasswordFormData) => {
    setIsLoading(true)
    try {
      await handleNewPasswordRequired(cognitoUser, data.newPassword)
      toast.success("Password changed successfully. You are now signed in.")
      router.push("/")
      router.refresh()
    } catch (error: any) {
      setIsLoading(false)
      
      let errorMessage = "Failed to change password. Please try again."
      
      if (error.code === "InvalidPasswordException") {
        errorMessage = "Password does not meet requirements. Please check and try again."
      } else if (error.code === "LimitExceededException") {
        errorMessage = "Too many attempts. Please try again later."
      } else if (error.message) {
        errorMessage = error.message
      }

      toast.error(errorMessage)
    }
  }

  return (
    <Card className="w-full max-w-md">
      <CardHeader>
        <CardTitle>Change Password</CardTitle>
        <CardDescription>
          Your password must be changed before you can continue. Please enter a new password.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="email-display">Email</Label>
            <Input
              id="email-display"
              type="email"
              value={email}
              disabled
              className="bg-muted"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="newPassword">New Password</Label>
            <Input
              id="newPassword"
              type="password"
              placeholder="Enter new password"
              autoComplete="new-password"
              aria-invalid={errors.newPassword ? "true" : "false"}
              aria-describedby={errors.newPassword ? "newPassword-error" : "newPassword-help"}
              {...register("newPassword")}
            />
            {errors.newPassword ? (
              <p id="newPassword-error" className="text-destructive text-sm" role="alert">
                {errors.newPassword.message}
              </p>
            ) : (
              <p id="newPassword-help" className="text-muted-foreground text-xs">
                Must be at least 8 characters with uppercase, lowercase, number, and special character
              </p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="confirmPassword">Confirm New Password</Label>
            <Input
              id="confirmPassword"
              type="password"
              placeholder="Confirm new password"
              autoComplete="new-password"
              aria-invalid={errors.confirmPassword ? "true" : "false"}
              aria-describedby={errors.confirmPassword ? "confirmPassword-error" : undefined}
              {...register("confirmPassword")}
            />
            {errors.confirmPassword && (
              <p id="confirmPassword-error" className="text-destructive text-sm" role="alert">
                {errors.confirmPassword.message}
              </p>
            )}
          </div>

          <div className="flex gap-2">
            {onCancel && (
              <Button type="button" variant="outline" onClick={onCancel} className="flex-1" disabled={isLoading}>
                Cancel
              </Button>
            )}
            <Button type="submit" className="flex-1" disabled={isLoading}>
              {isLoading ? (
                <>
                  <Spinner className="mr-2" />
                  Changing password...
                </>
              ) : (
                "Change Password"
              )}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  )
}

