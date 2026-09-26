import { ArrowLeft, Home } from "lucide-react";
import { Link } from "react-router-dom";
import { useNavigate } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";

export function NotFoundPage() {
  const navigate = useNavigate();

  return (
    <main className="grid min-h-screen place-items-center bg-bg-body bg-[radial-gradient(circle_at_1px_1px,rgba(18,110,68,0.12)_1px,transparent_0)] p-6 [background-size:22px_22px]">
      <section className="w-full max-w-2xl text-center">
        <EmptyPlateIllustration />

        <h1 className="mt-2 text-3xl font-black tracking-tight text-text-primary sm:text-4xl">
          Not Found
        </h1>
        <p className="mx-auto mt-3 max-w-md leading-relaxed text-text-secondary">
          The address may be incorrect, or the page may have been moved. Let&apos;s get you back to
          something useful.
        </p>

        <div className="mt-8 flex flex-col justify-center gap-3 sm:flex-row">
          <Link
            to={APP_PATHS.HOME}
            className="inline-flex min-h-11 items-center justify-center gap-2 rounded-md bg-primary-600 px-5 py-3 text-sm font-bold text-white shadow-sm transition hover:bg-primary-700 focus-visible:outline-none focus-visible:shadow-focus"
          >
            <Home className="size-4" aria-hidden="true" />
            Back to home
          </Link>
          <button
            type="button"
            onClick={() => navigate(-1)}
            className="inline-flex min-h-11 items-center justify-center gap-2 rounded-md border border-primary-700 bg-bg-body px-5 py-3 text-sm font-bold text-primary-800 transition hover:bg-primary-800 hover:text-white focus-visible:outline-none focus-visible:shadow-focus"
          >
            <ArrowLeft className="size-4" aria-hidden="true" />
            Go back
          </button>
        </div>
      </section>
    </main>
  );
}

function EmptyPlateIllustration() {
  return (
    <svg viewBox="0 0 320 300" className="mx-auto w-full max-w-72" fill="none" aria-hidden="true">
      <ellipse cx="160" cy="252" rx="118" ry="14" className="fill-neutral-900/8" />

      <circle
        cx="160"
        cy="150"
        r="118"
        className="fill-bg-surface stroke-primary-700/15"
        strokeWidth="2"
      />
      <circle
        cx="160"
        cy="150"
        r="92"
        className="fill-primary-50/60 stroke-primary-700/15"
        strokeWidth="1.5"
      />

      <g>
        <circle cx="118" cy="120" r="3.2" className="fill-accent-500" />
        <circle cx="205" cy="108" r="2.4" className="fill-primary-500" />
        <circle cx="196" cy="176" r="2.8" className="fill-primary-700/70" />
        <circle cx="130" cy="188" r="2" className="fill-accent-500" />
        <circle cx="172" cy="202" r="2.4" className="fill-primary-400" />
        <circle cx="104" cy="157" r="1.8" className="fill-primary-600" />
        <circle cx="215" cy="143" r="1.8" className="fill-accent-500" />
        <circle cx="154" cy="108" r="1.5" className="fill-primary-300" />
        <path d="M150 132q6-4 11 1-3 6-9 5-4-2-2-6Z" className="fill-primary-700/60" />
        <path d="m181 157 6-3 4 5-7 4Z" className="fill-accent-500/80" />
      </g>

      <g transform="translate(90 150) rotate(-28)" className="fill-primary-800 stroke-primary-800">
        <rect x="-3.5" y="-6" width="7" height="70" rx="3.5" stroke="none" />
        <path d="M-10-6v-22M-3.5-6v-26M3.5-6v-26M10-6v-22" strokeWidth="4" strokeLinecap="round" />
        <path d="M-10-28q0 12 10 12t10-12" fill="none" strokeWidth="4" strokeLinecap="round" />
      </g>

      <g transform="translate(232 150) rotate(28)" className="fill-primary-800">
        <rect x="-3.5" y="10" width="7" height="55" rx="3.5" />
        <path d="M-6 8c0-30 16-42 16-48 0 10-6 48-6 48Z" />
      </g>

      <g transform="translate(160 86) rotate(-4)">
        <rect x="-46" y="-24" width="92" height="46" rx="6" className="fill-primary-600" />
        <circle cx="0" cy="-24" r="4" className="fill-bg-surface" />
        <text x="0" y="8" textAnchor="middle" className="fill-white text-[26px] font-black">
          404
        </text>
      </g>
    </svg>
  );
}
