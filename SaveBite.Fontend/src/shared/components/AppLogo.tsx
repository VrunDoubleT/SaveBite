import { Link } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";

export function AppLogo() {
  return (
    <Link
      to={APP_PATHS.HOME}
      className="flex items-center gap-2 text-xl font-extrabold tracking-[-0.01em] text-primary-600 no-underline"
      aria-label="SaveBite - Home"
    >
      <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md bg-primary-500">
        <svg
          width="18"
          height="18"
          viewBox="0 0 24 24"
          fill="none"
          stroke="#ffffff"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
          aria-hidden="true"
        >
          <path d="M11 20A7 7 0 0 1 4 13H2a10 10 0 0 0 10 10 9.7 9.7 0 0 0 6.6-2.5" />
          <path d="M15.7 4.2a10 10 0 0 1 3.8 8.8" />
          <path d="M11 20A7 7 0 0 0 18 13" />
          <path d="M4 13a7 7 0 0 1 7-7V2c-6 0-11 5-11 11z" />
        </svg>
      </span>
      SaveBite
    </Link>
  );
}
