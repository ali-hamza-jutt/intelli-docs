"use client";

import { useRouter } from "next/navigation";
import { AuthForm } from "@/components/auth/AuthForm";
import { useToast } from "@/components/ui/Toast";

/**
 * Not yet backed by an endpoint — password reset is not part of the auth module's API surface.
 * The screen stays wired to the same form so it can be connected without a redesign.
 */
export default function ForgotPasswordPage() {
  const router = useRouter();
  const toast = useToast();

  return (
    <AuthForm
      onSubmit={() => {
        toast("Password reset is not available yet", "warn");
        router.push("/login");
      }}
      config={{
        title: "Reset your password",
        subtitle: "Enter your email and we'll send a reset link.",
        cta: "Send Reset Link",
        footNote: "Remembered it?",
        footLink: "Back to sign in",
        footHref: "/login",
        fields: [
          { id: "email", label: "Email", type: "email", placeholder: "you@company.com", autoComplete: "email" },
        ],
      }}
    />
  );
}
