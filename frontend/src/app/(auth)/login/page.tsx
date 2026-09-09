"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { Suspense } from "react";
import { AuthForm } from "@/components/auth/AuthForm";
import { usePostApiAuthLogin } from "@/lib/api/generated/auth/auth";
import { useAuth } from "@/lib/auth/AuthProvider";
import { ApiError } from "@/lib/api/client";
import { useToast } from "@/components/ui/Toast";

function LoginScreen() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { signIn } = useAuth();
  const toast = useToast();

  const login = usePostApiAuthLogin({
    mutation: {
      onSuccess: (auth) => {
        signIn(auth);
        toast("Signed in");
        // Return the user to whatever they were trying to reach.
        router.replace(searchParams.get("next") ?? "/dashboard");
      },
    },
  });

  return (
    <AuthForm
      isPending={login.isPending}
      errorMessage={login.error instanceof ApiError ? login.error.message : null}
      onSubmit={(values) =>
        login.mutate({ data: { email: values.email, password: values.password } })
      }
      config={{
        title: "Welcome back",
        subtitle: "Continue to your knowledge workspace.",
        cta: "Sign In",
        showRemember: true,
        showDivider: true,
        footNote: "Don't have an account?",
        footLink: "Create one",
        footHref: "/register",
        fields: [
          { id: "email", label: "Email", type: "email", placeholder: "you@company.com", autoComplete: "email" },
          { id: "password", label: "Password", type: "password", placeholder: "••••••••", autoComplete: "current-password" },
        ],
      }}
    />
  );
}

export default function LoginPage() {
  return (
    <Suspense fallback={null}>
      <LoginScreen />
    </Suspense>
  );
}
