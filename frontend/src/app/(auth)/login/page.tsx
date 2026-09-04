import { AuthForm } from "@/components/auth/AuthForm";

export default function LoginPage() {
  return (
    <AuthForm
      config={{
        title: "Welcome back",
        subtitle: "Continue to your knowledge workspace.",
        cta: "Sign In",
        showRemember: true,
        showDivider: true,
        footNote: "Don't have an account?",
        footLink: "Create one",
        footHref: "/register",
        redirectTo: "/dashboard",
        successMessage: "Signed in",
        fields: [
          { id: "email", label: "Email", type: "email", placeholder: "you@company.com", autoComplete: "email" },
          { id: "password", label: "Password", type: "password", placeholder: "••••••••", autoComplete: "current-password" },
        ],
      }}
    />
  );
}
