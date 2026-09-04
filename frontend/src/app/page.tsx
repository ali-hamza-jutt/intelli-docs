import Link from "next/link";
import { LandingNav } from "@/components/landing/LandingNav";
import { Hero } from "@/components/landing/Hero";
import { Faq } from "@/components/landing/Faq";
import { FeatureCard, Section, SectionHeading } from "@/components/landing/Sections";
import { Card } from "@/components/ui/Card";
import { ButtonLink } from "@/components/ui/Button";
import { Icon } from "@/components/ui/Icon";
import { Logo } from "@/components/layout/Logo";
import {
  BRANDS,
  FEATURES,
  FOOTER_LINKS,
  PIPELINE,
  PROBLEMS,
  SOCIALS,
  STEPS,
} from "@/lib/landing-data";

export default function LandingPage() {
  return (
    <div className="bg-surface">
      <LandingNav />
      <Hero />

      {/* Social proof */}
      <div className="border-y border-line bg-canvas py-6">
        <div className="mx-auto flex max-w-[1200px] flex-wrap items-center justify-center gap-x-12 gap-y-3 px-6">
          {BRANDS.map((brand) => (
            <span key={brand} className="text-md font-semibold tracking-[-0.01em] text-faint">
              {brand}
            </span>
          ))}
        </div>
      </div>

      {/* Problem */}
      <Section id="product" tone="surface">
        <SectionHeading
          eyebrow="The problem"
          title="Built for teams that work with information."
          body="A workspace built around your sources, not around search boxes."
        />
        <div className="grid-fit-lg">
          {PROBLEMS.map((item) => (
            <FeatureCard key={item.title} {...item} />
          ))}
        </div>
      </Section>

      {/* How it works */}
      <Section id="how" tone="canvas">
        <SectionHeading eyebrow="How it works" title="From document to answer in three steps." />

        <div className="grid-fit-lg">
          {STEPS.map((step) => (
            <Card key={step.title} hoverable>
              <p className="m-0 eyebrow text-brand">{step.num}</p>
              <span className="tile tile-brand mt-4 size-11 text-xl">
                <Icon name={step.icon} />
              </span>
              <h3 className="mt-4 mb-1.5 text-xl font-bold tracking-[-0.02em]">{step.title}</h3>
              <p className="m-0 text-body leading-relaxed text-muted">{step.body}</p>
            </Card>
          ))}
        </div>

        {/* Pipeline */}
        <div className="mt-12 flex flex-wrap items-center justify-center gap-3">
          {PIPELINE.map((node, i) => (
            <div key={node.label} className="flex items-center gap-3">
              <div className="flex items-center gap-2.5 rounded-card border border-line bg-surface px-4 py-3">
                <Icon name={node.icon} className="text-md text-brand" />
                <span className="text-body font-medium whitespace-nowrap">{node.label}</span>
              </div>
              {i < PIPELINE.length - 1 && <Icon name="arrowRight" className="text-md text-faint" />}
            </div>
          ))}
        </div>
      </Section>

      {/* Features */}
      <Section id="features" tone="surface">
        <SectionHeading
          eyebrow="Features"
          title="One question. One grounded answer."
          body="Only your documents answer your questions."
        />
        <div className="grid gap-3.5 [grid-template-columns:repeat(auto-fit,minmax(260px,1fr))]">
          {FEATURES.map((feature) => (
            <FeatureCard key={feature.title} {...feature} />
          ))}
        </div>
      </Section>

      {/* FAQ */}
      <Section id="faq" tone="canvas">
        <SectionHeading eyebrow="FAQ" title="Frequently asked questions" />
        <Faq />
      </Section>

      {/* CTA */}
      <Section tone="inverse">
        <div className="mx-auto max-w-[620px] text-center">
          <h2 className="m-0 text-[clamp(28px,4vw,40px)] font-bold leading-tight tracking-[-0.03em]">
            Turn your documents into answers.
          </h2>
          <p className="mx-auto mt-4 mb-8 max-w-[460px] text-lg leading-relaxed text-subtle">
            Your knowledge stays yours. Workspace-isolated retrieval, every answer cited.
          </p>
          <ButtonLink href="/register" size="lg">
            Get Started Free
          </ButtonLink>
        </div>
      </Section>

      {/* Footer */}
      <footer className="border-t border-line bg-surface">
        <div className="mx-auto grid max-w-[1200px] gap-10 px-6 py-14 [grid-template-columns:repeat(auto-fit,minmax(200px,1fr))]">
          <div>
            <Logo />
            <p className="mt-4 mb-5 max-w-[260px] text-body leading-relaxed text-muted">
              Ask your knowledge base anything and get answers grounded in your own documents.
            </p>
            <div className="flex gap-2">
              {SOCIALS.map((social) => (
                <span
                  key={social.label}
                  aria-label={social.label}
                  title={social.label}
                  className="icon-btn icon-btn-md icon-btn-bordered"
                >
                  <Icon name={social.icon} />
                </span>
              ))}
            </div>
          </div>

          {FOOTER_LINKS.map((group) => (
            <div key={group.title}>
              <p className="mb-3.5 eyebrow">{group.title}</p>
              <ul className="m-0 flex list-none flex-col gap-2.5 p-0">
                {group.links.map(([label, href]) => (
                  <li key={label}>
                    <Link href={href} className="text-body text-muted hover:text-ink">
                      {label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        <div className="border-t border-line py-5 text-center text-small text-subtle">
          © 2026 DocuMind AI. All rights reserved.
        </div>
      </footer>
    </div>
  );
}
