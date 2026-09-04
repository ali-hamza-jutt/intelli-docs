import Image from "next/image";
import { Logo } from "@/components/layout/Logo";
import { TESTIMONIAL } from "@/lib/landing-data";

/** Split auth shell: the form on the left, social proof on the right. */
export default function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="grid min-h-screen [grid-template-columns:repeat(auto-fit,minmax(420px,1fr))]">
      <div className="flex items-center justify-center bg-canvas px-6 py-10">
        <div className="w-full max-w-[420px] animate-fade-up">
          <Logo className="mb-8" />
          {children}
        </div>
      </div>

      <div className="flex min-h-[280px] flex-col justify-center gap-7 bg-inverse px-10 py-14">
        <p className="m-0 max-w-[420px] text-[22px] font-semibold leading-[1.5] tracking-[-0.02em] text-white">
          {TESTIMONIAL.quote}
        </p>
        <p className="m-0 text-base text-subtle">{TESTIMONIAL.author}</p>
        <div className="max-w-[460px] overflow-hidden rounded-card border border-ink-soft">
          <Image
            src="https://images.unsplash.com/photo-1600880292203-757bb62b4baf?auto=format&fit=crop&w=1000&q=80"
            alt="Colleagues reviewing documents together"
            width={1000}
            height={220}
            className="h-[220px] w-full object-cover"
            unoptimized
          />
        </div>
      </div>
    </div>
  );
}
