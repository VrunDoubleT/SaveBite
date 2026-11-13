import { Edit, Trash2, Power } from "lucide-react";
import type { Category } from "@/features/categories/types/adminCategory.types";
import { RecordId } from "@/shared/components/RecordId";
import { CategoryStatusBadge } from "./CategoryStatusBadge";

interface CategoryTableProps {
  categories: Category[];
  onToggleStatus: (id: string, isActive: boolean) => void;
  onEdit: (category: Category) => void;
  onDelete: (id: string) => void;
}

export function CategoryTable({
  categories,
  onToggleStatus,
  onEdit,
  onDelete,
}: CategoryTableProps) {
  if (categories.length === 0) {
    return (
      <div className="p-10 text-center text-sm text-text-muted">
        No categories found matching your criteria.
      </div>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[960px] table-fixed divide-y divide-border-default text-left text-sm">
        <colgroup>
          <col className="w-[14%]" />
          <col className="w-[22%]" />
          <col className="w-[32%]" />
          <col className="w-[16%]" />
          <col className="w-[16%]" />
        </colgroup>
        <thead className="bg-neutral-50 text-xs font-semibold uppercase tracking-wide text-text-secondary">
          <tr>
            <th className="px-6 py-4 font-semibold">ID</th>
            <th className="px-6 py-4 font-semibold">Name</th>
            <th className="px-6 py-4 font-semibold">Description</th>
            <th className="px-6 py-4 font-semibold">Status</th>
            <th className="px-6 py-4 font-semibold text-right">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-border-default text-text-primary">
          {categories.map((c) => (
            <tr key={c.id} className="transition-colors hover:bg-neutral-50/50">
              <td className="max-w-40 px-6 py-4">
                <RecordId id={c.id} />
              </td>
              <td className="px-6 py-4">
                <p className="font-semibold text-text-primary">{c.name}</p>
              </td>
              <td className="px-6 py-4">
                {c.description ? (
                  <p className="line-clamp-2 max-w-[250px] text-xs leading-relaxed text-text-secondary">
                    {c.description}
                  </p>
                ) : (
                  <span className="text-xs italic text-text-muted">No description</span>
                )}
              </td>
              <td className="px-6 py-4">
                <CategoryStatusBadge isActive={c.isActive} />
              </td>
              <td className="px-6 py-4 text-right">
                <div className="flex justify-end gap-2">
                  <button
                    onClick={() => onToggleStatus(c.id, c.isActive)}
                    title={c.isActive ? "Deactivate" : "Activate"}
                    aria-label={c.isActive ? "Deactivate category" : "Activate category"}
                    className={`rounded p-1.5 transition ${c.isActive ? "text-neutral-500 hover:bg-orange-50 hover:text-orange-600" : "text-neutral-500 hover:bg-primary-50 hover:text-primary-600"}`}
                  >
                    <Power className="size-4" aria-hidden="true" />
                  </button>
                  <button
                    onClick={() => onEdit(c)}
                    title="Edit Category"
                    aria-label="Edit category"
                    className="rounded p-1.5 text-neutral-500 transition hover:bg-blue-50 hover:text-blue-600"
                  >
                    <Edit className="size-4" aria-hidden="true" />
                  </button>
                  <button
                    onClick={() => onDelete(c.id)}
                    title="Delete Category"
                    aria-label="Delete category"
                    className="rounded p-1.5 text-neutral-500 transition hover:bg-red-50 hover:text-danger"
                  >
                    <Trash2 className="size-4" aria-hidden="true" />
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
