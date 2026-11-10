import { Link } from "react-router-dom";
import { Eye, MapPin, User } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { RecordId } from "@/shared/components/RecordId";
import { ShopStatusBadge } from "./ShopProfileHeader";
import type { ShopListManagement } from "../types/shopManagement.types";
import { useState } from "react";

const manageLinkClass =
  "inline-flex items-center gap-1.5 rounded-md border border-border-default bg-white px-2.5 py-1.5 text-xs font-semibold text-neutral-600 transition hover:bg-neutral-50 hover:text-neutral-900 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700";

const manageHref = (id: string) => APP_PATHS.ADMIN_SHOP_CONTENT.replace(":shopId", id);

function ShopMeta({ shop }: { shop: ShopListManagement }) {
  const [imageError, setImageError] = useState(false);

  const showLogo = Boolean(shop.logoUrl) && !imageError;

  return (
    <div className="flex min-w-0 items-center gap-3">
      <div className="flex size-11 shrink-0 items-center justify-center overflow-hidden rounded-lg border border-border-default bg-neutral-50">
        {showLogo ? (
          <img
            src={shop.logoUrl!}
            alt={`${shop.name} logo`}
            className="h-full w-full object-cover"
            onError={() => setImageError(true)}
          />
        ) : (
          <span className="text-sm font-bold text-text-muted">
            {shop.name?.trim().charAt(0).toUpperCase() || "S"}
          </span>
        )}
      </div>

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
    </div>
  );
}

export function ShopManagementTable({ items }: { items: ShopListManagement[] }) {
  return (
    <>
      {/* Desktop table */}
      <div className="hidden overflow-x-auto md:block">
        <table className="w-full min-w-[940px] table-fixed text-left text-sm">
          <colgroup>
            <col className="w-[17%]" />
            <col className="w-[26%]" />
            <col className="w-[19%]" />
            <col className="w-[16%]" />
            <col className="w-[12%]" />
            <col className="w-[10%]" />
          </colgroup>
          <thead className="bg-neutral-50 text-xs font-semibold uppercase tracking-wide text-text-secondary">
            <tr>
              <th scope="col" className="px-6 py-4">
                ID
              </th>
              <th scope="col" className="px-6 py-4">
                Shop
              </th>
              <th scope="col" className="px-6 py-4">
                Owner
              </th>
              <th scope="col" className="px-6 py-4">
                City
              </th>
              <th scope="col" className="px-6 py-4">
                Status
              </th>
              <th scope="col" className="px-6 py-4 text-right">
                Actions
              </th>
            </tr>
          </thead>

          <tbody className="divide-y divide-border-default text-text-primary">
            {items.map((shop) => (
              <tr key={shop.id} className="transition-colors hover:bg-neutral-50/70">
                <td className="max-w-40 px-6 py-4">
                  <RecordId id={shop.id} />
                </td>
                <td className="max-w-sm px-6 py-4">
                  <ShopMeta shop={shop} />
                </td>

                <td className="px-6 py-4 font-medium">{shop.ownerName || "—"}</td>

                <td className="whitespace-nowrap px-6 py-4 text-text-secondary">
                  {shop.city || "—"}
                </td>

                <td className="px-6 py-4">
                  <ShopStatusBadge status={shop.status} />
                </td>

                <td className="px-6 py-4 text-right">
                  <Link
                    to={manageHref(shop.id)}
                    className={manageLinkClass}
                    title="View"
                    aria-label="View shop"
                  >
                    <Eye className="size-3.5" aria-hidden="true" />
                    <span>View</span>
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Mobile list */}
      <ul className="divide-y divide-border-default md:hidden">
        {items.map((shop) => (
          <li key={shop.id} className="space-y-4 p-4">
            <div className="flex items-center gap-2 text-xs text-text-secondary">
              <span>ID</span>
              <RecordId id={shop.id} />
            </div>
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
              <Link
                to={manageHref(shop.id)}
                className={manageLinkClass}
                title="View"
                aria-label="View shop"
              >
                <Eye className="size-3.5" aria-hidden="true" />
                <span>View</span>
              </Link>
            </div>
          </li>
        ))}
      </ul>
    </>
  );
}
