import { ShieldCheck } from "lucide-react";

interface SessionCheckingScreenProps {
  message?: string;
  subMessage?: string;
  minHeight?: string;
}

export function SessionCheckingScreen({
  message = "Checking your session",
  subMessage = "Securing your connection and preparing your SaveBite experience",
  minHeight = "min-h-screen",
}: SessionCheckingScreenProps) {
  return (
    <div
      className={`relative grid ${minHeight} w-full place-items-center overflow-hidden bg-[#fafbfc] bg-[radial-gradient(circle_at_1px_1px,rgba(22,138,84,0.12)_1px,transparent_0)] p-6 [background-size:22px_22px]`}
    >
      {/* Ambient background glow orbs */}
      <div
        className="pointer-events-none absolute -top-24 left-1/2 -translate-x-1/2 h-96 w-96 rounded-full bg-emerald-500/10 blur-3xl"
        aria-hidden="true"
      />
      <div
        className="pointer-events-none absolute -bottom-24 left-1/2 -translate-x-1/2 h-96 w-96 rounded-full bg-amber-500/8 blur-3xl"
        aria-hidden="true"
      />

      {/* Main Glass Card */}
      <div className="relative z-10 flex flex-col items-center text-center">
        {/* Animated Brand Emblem */}
        <div className="relative mb-6 flex h-24 w-24 items-center justify-center">
          {/* Outer rotating pulse ring */}
          <div
            className="absolute inset-0 rounded-3xl border-2 border-dashed border-emerald-500/30 animate-[spin_8s_linear_infinite]"
            aria-hidden="true"
          />

          {/* Secondary counter-rotating accent ring */}
          <div
            className="absolute inset-1.5 rounded-2xl border border-emerald-600/20 animate-[spin_5s_linear_infinite_reverse]"
            aria-hidden="true"
          />

          {/* Glowing central squircle with SaveBite Logo */}
          <div className="relative flex h-14 w-14 items-center justify-center rounded-2xl bg-gradient-to-tr from-emerald-600 via-emerald-500 to-teal-400 text-white shadow-xl shadow-emerald-600/30 transition-transform">
            <svg
              width="26"
              height="26"
              viewBox="0 0 24 24"
              fill="none"
              stroke="#ffffff"
              strokeWidth="2.2"
              strokeLinecap="round"
              strokeLinejoin="round"
              className="animate-[pulse_2s_ease-in-out_infinite]"
              aria-hidden="true"
            >
              <path d="M11 20A7 7 0 0 1 4 13H2a10 10 0 0 0 10 10 9.7 9.7 0 0 0 6.6-2.5" />
              <path d="M15.7 4.2a10 10 0 0 1 3.8 8.8" />
              <path d="M11 20A7 7 0 0 0 18 13" />
              <path d="M4 13a7 7 0 0 1 7-7V2c-6 0-11 5-11 11z" />
            </svg>
          </div>

          {/* Security Shield Badge */}
          <div className="absolute -bottom-1 -right-1 flex h-7 w-7 items-center justify-center rounded-full border-2 border-white bg-emerald-50 text-emerald-700 shadow-md">
            <ShieldCheck size={14} className="stroke-[2.5]" />
          </div>
        </div>

        {/* Title with animated typing dots */}
        <h2 className="flex items-center text-lg sm:text-xl font-bold tracking-tight text-neutral-800">
          <span>{message}</span>
          <span className="inline-flex w-7 text-left text-emerald-600 font-extrabold tracking-widest pl-0.5">
            <span className="animate-[pulse_1s_infinite_100ms]">.</span>
            <span className="animate-[pulse_1s_infinite_300ms]">.</span>
            <span className="animate-[pulse_1s_infinite_500ms]">.</span>
          </span>
        </h2>

        {/* Subtitle */}
        <p className="mt-2 max-w-[280px] sm:max-w-xs text-xs sm:text-sm font-medium leading-relaxed text-neutral-500">
          {subMessage}
        </p>

        {/* High-tech Indeterminate Shimmer Progress Bar */}
        <div className="mt-6 h-1.5 w-52 overflow-hidden rounded-full bg-neutral-200/70 shadow-inner">
          <div className="h-full w-full rounded-full bg-gradient-to-r from-transparent via-emerald-500 to-transparent animate-[indeterminate_1.4s_infinite_easeInOut]" />
        </div>

        {/* Security / System Status Tag */}
        <div className="mt-8 inline-flex items-center gap-2 rounded-full border border-neutral-200/90 bg-white/90 px-3.5 py-1.5 text-xs font-medium text-neutral-600 shadow-xs backdrop-blur-md">
          <span className="relative flex h-2 w-2">
            <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400 opacity-75" />
            <span className="relative inline-flex h-2 w-2 rounded-full bg-emerald-600" />
          </span>
          <span className="text-[11px] font-semibold text-neutral-600 tracking-wide">
            SaveBite Secure Session
          </span>
        </div>
      </div>

      <style>{`
        @keyframes indeterminate {
          0% {
            transform: translateX(-100%);
          }
          50% {
            transform: translateX(0%);
          }
          100% {
            transform: translateX(100%);
          }
        }
      `}</style>
    </div>
  );
}
