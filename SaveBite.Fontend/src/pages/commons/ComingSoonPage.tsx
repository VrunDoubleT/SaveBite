import { Link } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";

interface ComingSoonPageProps {
  title: string;
  description: string;
  backTo?: string;
}

export function ComingSoonPage({
  title,
  description,
  backTo = APP_PATHS.HOME,
}: ComingSoonPageProps) {
  return (
    <div className="grid min-h-[65vh] place-items-center">
      <div className="max-w-lg text-center">
        <span className="mx-auto grid size-16 place-items-center rounded-2xl bg-primary-50 text-2xl text-primary-700">
          ◇
        </span>
        <h1 className="mt-5 text-3xl font-black">{title}</h1>
        <p className="mt-2 text-neutral-500">{description}</p>
        <Link
          to={backTo}
          className="mt-6 inline-flex rounded-xl bg-primary-600 px-5 py-3 text-sm font-bold text-white"
        >
          Back to overview
        </Link>
      </div>
    </div>
  );
}
