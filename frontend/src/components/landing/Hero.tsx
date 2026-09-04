import { Icon } from "@/components/ui/Icon";
import { ButtonLink } from "@/components/ui/Button";
import { HERO_CHIPS, HERO_STATS, HERO_TURN } from "@/lib/landing-data";

/** Static mock of the chat exchange, shown beside the hero copy. */
function AnswerPreview() {
  return (
    <div className="panel overflow-hidden shadow-[0_14px_34px_-22px_rgb(17_24_39/0.45)]">
      <div className="flex items-center gap-2.5 border-b border-line px-5 py-3.5">
        <Icon name="sparkles" className="text-md text-brand" />
        <span className="text-small font-semibold">DocuMind AI</span>
      </div>

      <div className="flex flex-col gap-4 p-5">
        <div className="flex justify-end">
          <p className="m-0 max-w-[85%] rounded-[14px] rounded-br-[4px] bg-inverse px-4 py-2.5 text-body text-white">
            {HERO_TURN.question}
          </p>
        </div>

        <div className="card px-4 py-3.5">
          <p className="m-0 text-body leading-[1.7]">{HERO_TURN.answer}</p>
          <div className="mt-3.5 border-t border-line-soft pt-3">
            <p className="mb-2 eyebrow">Sources</p>
            <div className="flex flex-wrap gap-2">
              {HERO_TURN.sources.map((source) => (
                <span
                  key={source.doc}
                  className="inline-flex items-center gap-2 rounded-control border border-line bg-canvas px-2.5 py-1.5 text-tiny"
                >
                  <Icon name="quote" className="text-brand" />
                  <span className="font-semibold">{source.doc}</span>
                  <span className="text-subtle">{source.page}</span>
                </span>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export function Hero() {
  return (
    <section className="mx-auto grid max-w-[1200px] items-center gap-14 px-6 pt-20 pb-16 [grid-template-columns:repeat(auto-fit,minmax(360px,1fr))]">
      <div className="animate-fade-up">
        <div className="mb-6 inline-flex items-center gap-2 rounded-pill bg-brand-soft px-3 py-1.5 text-small font-semibold text-brand">
          <Icon name="sparkles" className="text-base" />
          Grounded answers, with sources
        </div>

        <h1 className="m-0 text-[clamp(38px,5vw,60px)] font-extrabold leading-[1.04] tracking-[-0.035em]">
          Your documents.
          <br />
          One intelligent workspace.
        </h1>

        <p className="mt-6 mb-0 max-w-[460px] text-lg leading-relaxed text-muted">
          Ask in plain language. Get answers grounded in your own files, with the source attached.
        </p>

        <div className="mt-8 flex flex-wrap gap-3">
          <ButtonLink href="/register" size="lg">
            Start for Free
          </ButtonLink>
          <ButtonLink href="#how" variant="secondary" size="lg" trailingIcon="arrowRight">
            See How It Works
          </ButtonLink>
        </div>

        <p className="mt-4 mb-0 text-small text-subtle">No credit card required</p>

        <div className="mt-9 flex flex-wrap gap-7 border-t border-line pt-7">
          {HERO_STATS.map((stat) => (
            <div key={stat.label}>
              <p className="m-0 text-2xl font-bold tracking-[-0.02em]">{stat.value}</p>
              <p className="mt-0.5 mb-0 text-caption text-subtle">{stat.label}</p>
            </div>
          ))}
        </div>
      </div>

      <div className="animate-fade-up">
        <AnswerPreview />
        <div className="mt-5 flex flex-wrap gap-2">
          {HERO_CHIPS.map((chip) => (
            <span
              key={chip.label}
              className="inline-flex items-center gap-2 rounded-pill border border-line bg-surface px-3 py-1.5 text-caption text-muted"
            >
              <Icon name={chip.icon} className="text-small text-brand" />
              {chip.label}
            </span>
          ))}
        </div>
      </div>
    </section>
  );
}
