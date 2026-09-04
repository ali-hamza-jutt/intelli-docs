import { AuthForm } from "@/components/auth/AuthForm";

export default function RegisterPage() {
  return (
    <AuthForm
      config={{
        title: "Create your knowledge workspace",
        subtitle: "Upload documents and start asking in minutes.",
        cta: "Create Account",
        showTerms: true,
        footNote: "Already have an account?",
        footLink: "Sign in",
        footHref: "/login",
        redirectTo: "/dashboard",
        successMessage: "Workspace created",
        fields: [
          { id: "name", label: "Full Name", type: "text", placeholder: "Hamza Ali", autoComplete: "name" },
          { id: "email", label: "Email", type: "email", placeholder: "you@company.com", autoComplete: "email" },
          { id: "password", label: "Password", type: "password", placeholder: "At least 8 characters", autoComplete: "new-password", strength: true },
          { id: "confirm", label: "Confirm Password", type: "password", placeholder: "Re-enter password", autoComplete: "new-password" },
        ],
      }}
    />
  );
}
