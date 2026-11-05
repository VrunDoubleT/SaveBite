import { Link } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";

export const APP_LOGO_URL =
  "https://res.cloudinary.com/qhd4t4qt/image/upload/v1793859170/IMG_6340.jpg";

export function AppLogo() {
  return (
    <Link
      to={APP_PATHS.HOME}
      className="flex items-center gap-2 text-xl font-extrabold tracking-[-0.01em] text-primary-600 no-underline"
      aria-label="SaveBite - Home"
    >
      <img
        src={APP_LOGO_URL}
        alt=""
        aria-hidden="true"
        className="h-8 w-8 shrink-0 rounded-md object-cover"
      />
      SaveBite
    </Link>
  );
}
