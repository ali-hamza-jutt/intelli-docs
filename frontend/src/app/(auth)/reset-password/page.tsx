"use client";

import { useRouter } from "next/navigation";
import { AuthForm } from "@/components/auth/AuthForm";
import { useToast } from "@/components/ui/Toast";

/** Also awaiting an endpoint — see the note on the forgot-password screen. */
export default function ResetPasswordPage() {
  const router = useRouter();
  const toast = useToast();

  return (
    <AuthForm
      onSubmit={() => {
        toast("Password reset is not available yet", "warn");
        router.push("/login");
      }}
      config={{
        title: "Choose a new password",
        subtitle: "Set a new password for your account.",
        cta: "Update Password",
        footNote: "Changed your mind?",
        footLink: "Back to sign in",
        footHref: "/login",
        fields: [
          { id: "password", label: "New Password", type: "password", placeholder: "At least 8 characters", autoComplete: "new-password", strength: true },
          { id: "confirm", label: "Confirm Password", type: "password", placeholder: "Re-enter password", autoComplete: "new-password" },
        ],
      }}
    />
  );
}
