import type { Metadata } from "next";
import { Inter } from "next/font/google";
import { ToastProvider } from "@/components/ui/Toast";
import { QueryProvider } from "@/lib/query/QueryProvider";
import { AuthProvider } from "@/lib/auth/AuthProvider";
import "./globals.css";

const inter = Inter({
  subsets: ["latin"],
  weight: ["400", "500", "600", "700", "800"],
  variable: "--font-inter",
});

export const metadata: Metadata = {
  title: "DocuMind AI — Your documents, one intelligent workspace",
  description:
    "Ask in plain language. Get answers grounded in your own files, with the source attached.",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" className={inter.variable}>
      <body>
        {/* AuthProvider needs the query client to clear caches on sign-out, so it sits inside. */}
        <QueryProvider>
          <AuthProvider>
            <ToastProvider>{children}</ToastProvider>
          </AuthProvider>
        </QueryProvider>
      </body>
    </html>
  );
}
