import { AuthForm } from "@/components/auth/AuthForm";

export default function ForgotPasswordPage() {
  return (
    <AuthForm
      config={{
        title: "Reset your password",
        subtitle: "Enter your email and we'll send a reset link.",
        cta: "Send Reset Link",
        footNote: "Remembered it?",
        footLink: "Back to sign in",
        footHref: "/login",
        redirectTo: "/login",
        successMessage: "Reset link sent",
        fields: [
          { id: "email", label: "Email", type: "email", placeholder: "you@company.com", autoComplete: "email" },
        ],
      }}
    />
  );
}
