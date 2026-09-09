"use client";

import { useRouter } from "next/navigation";
import { AuthForm } from "@/components/auth/AuthForm";
import { usePostApiAuthRegister } from "@/lib/api/generated/auth/auth";
import { useAuth } from "@/lib/auth/AuthProvider";
import { ApiError } from "@/lib/api/client";
import { useToast } from "@/components/ui/Toast";

export default function RegisterPage() {
  const router = useRouter();
  const { signIn } = useAuth();
  const toast = useToast();

  const register = usePostApiAuthRegister({
    mutation: {
      onSuccess: (auth) => {
        signIn(auth);
        toast("Workspace created");
        router.replace("/dashboard");
      },
    },
  });

  return (
    <AuthForm
      isPending={register.isPending}
      errorMessage={register.error instanceof ApiError ? register.error.message : null}
      onSubmit={(values) =>
        register.mutate({
          data: { name: values.name, email: values.email, password: values.password },
        })
      }
      config={{
        title: "Create your knowledge workspace",
        subtitle: "Upload documents and start asking in minutes.",
        cta: "Create Account",
        showTerms: true,
        footNote: "Already have an account?",
        footLink: "Sign in",
        footHref: "/login",
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
