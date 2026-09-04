import { AuthForm } from "@/components/auth/AuthForm";

export default function ResetPasswordPage() {
  return (
    <AuthForm
      config={{
        title: "Choose a new password",
        subtitle: "Set a password for hamza@northstar.co.",
        cta: "Update Password",
        footNote: "Changed your mind?",
        footLink: "Back to sign in",
        footHref: "/login",
        redirectTo: "/login",
        successMessage: "Password updated",
        fields: [
          { id: "password", label: "New Password", type: "password", placeholder: "At least 8 characters", autoComplete: "new-password", strength: true },
          { id: "confirm", label: "Confirm Password", type: "password", placeholder: "Re-enter password", autoComplete: "new-password" },
        ],
      }}
    />
  );
}
