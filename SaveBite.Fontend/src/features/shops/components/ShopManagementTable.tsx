import { Link } from "react-router-dom";
import { Eye, MapPin, User } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { ShopStatusBadge } from "./ShopProfileHeader";
import type { ShopListManagement } from "../types/shopManagement.types";

const manageLinkClass =
  "inline-flex items-center gap-1.5 rounded-md border border-border-default bg-white px-3 py-1.5 text-xs font-semibold text-text-secondary transition-colors hover:border-primary-300 hover:bg-primary-50 hover:text-primary-700 focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500";

const manageHref = (id: string) => APP_PATHS.ADMIN_SHOP_CONTENT.replace(":shopId", id);

function ShopMeta({ shop }: { shop: ShopListManagement }) {
  return (
    <div className="min-w-0 space-y-0.5">
      <p className="truncate font-semibold text-text-primary" title={shop.name}>
        {shop.name}
      </p>
      {shop.description && (
        <p className="truncate text-xs text-text-muted" title={shop.description}>
          {shop.description}
        </p>
      )}
    </div>
  );
}

export function ShopManagementTable({ items }: { items: ShopListManagement[] }) {
  return (
    <>
      {/* Desktop table */}
      <div className="hidden overflow-hidden rounded-3xl border border-border-default bg-white shadow-[0_1px_2px_rgba(16,24,40,0.04)] md:block">
        <table className="min-w-full text-left text-sm">
          <thead className="bg-neutral-50 text-xs text-text-secondary">
            <tr>
              <th scope="col" className="px-6 py-3.5 font-semibold">
                Shop
              </th>
              <th scope="col" className="px-4 py-3.5 font-semibold">
                Owner
              </th>
              <th scope="col" className="px-4 py-3.5 font-semibold">
                City
              </th>
              <th scope="col" className="px-4 py-3.5 font-semibold">
                Status
              </th>
              <th scope="col" className="px-6 py-3.5 text-right font-semibold">
                <span className="sr-only">Actions</span>
              </th>
            </tr>
          </thead>

          <tbody className="divide-y divide-border-default text-text-primary">
            {items.map((shop) => (
              <tr key={shop.id} className="transition-colors hover:bg-neutral-50/70">
                <td className="max-w-sm px-6 py-4">
                  <ShopMeta shop={shop} />
                </td>

                <td className="px-4 py-4 font-medium">{shop.ownerName || "—"}</td>

                <td className="whitespace-nowrap px-4 py-4 text-text-secondary">
                  {shop.city || "—"}
                </td>

                <td className="px-4 py-4">
                  <ShopStatusBadge status={shop.status} />
                </td>

                <td className="px-6 py-4 text-right">
                  <Link to={manageHref(shop.id)} className={manageLinkClass}>
                    <Eye className="size-3.5" aria-hidden="true" />
                    Manage content
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Mobile cards */}
      <ul className="space-y-3 md:hidden">
        {items.map((shop) => (
          <li
            key={shop.id}
            className="space-y-4 rounded-3xl border border-border-default bg-white p-4 shadow-[0_1px_2px_rgba(16,24,40,0.04)]"
          >
            <ShopMeta shop={shop} />

            <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-xs text-text-secondary">
              <span className="flex items-center gap-1.5">
                <User className="size-3.5 text-text-muted" aria-hidden="true" />
                {shop.ownerName || "—"}
              </span>
              <span className="flex items-center gap-1.5">
                <MapPin className="size-3.5 text-text-muted" aria-hidden="true" />
                {shop.city || "—"}
              </span>
            </div>

            <div className="flex items-center justify-between gap-3 border-t border-border-default pt-3">
              <ShopStatusBadge status={shop.status} />
              <Link to={manageHref(shop.id)} className={manageLinkClass}>
                <Eye className="size-3.5" aria-hidden="true" />
                Manage content
              </Link>
            </div>
          </li>
        ))}
      </ul>
    </>
  );
}
