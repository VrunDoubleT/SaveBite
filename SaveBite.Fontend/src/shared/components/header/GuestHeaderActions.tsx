import { Link } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";

export function GuestHeaderActions() {
  return (
    <div className="flex items-center gap-2 sm:gap-3">
      <Link
        to={APP_PATHS.LOGIN}
        className="rounded-md px-3 py-2 text-sm font-semibold text-neutral-600 transition-colors hover:bg-neutral-50 hover:text-primary-600 focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-primary-400/30 sm:px-4"
      >
        Sign in
      </Link>
      <Link
        to={APP_PATHS.REGISTER}
        className="rounded-md bg-primary-500 px-3 py-2 text-sm font-semibold text-white transition-colors hover:bg-primary-600 active:bg-primary-700 focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-primary-400/30 sm:px-4"
      >
        Create account
      </Link>
    </div>
  );
}
