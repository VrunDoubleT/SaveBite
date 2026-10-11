import { Edit, Trash2, Power, Layers } from "lucide-react";
import type { Category } from "@/features/categories/types/adminCategory.types";
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
  return (
    <table className="min-w-full divide-y divide-border-default text-left text-sm">
      <thead className="bg-neutral-50 text-text-secondary">
        <tr>
          <th className="px-6 py-4 font-semibold">Name</th>
          <th className="px-6 py-4 font-semibold">Description</th>
          <th className="px-6 py-4 font-semibold">Status</th>
          <th className="px-6 py-4 font-semibold text-right">Actions</th>
        </tr>
      </thead>
      <tbody className="divide-y divide-border-default text-text-primary">
        {categories.map((c) => (
          <tr key={c.id} className="hover:bg-neutral-50/50 transition-colors">
            <td className="px-6 py-4">
              <p className="font-semibold text-text-primary">{c.name}</p>
            </td>
            <td className="px-6 py-4">
              {c.description ? (
                <p className="text-xs text-text-secondary line-clamp-2 max-w-[250px] leading-relaxed">
                  {c.description}
                </p>
              ) : (
                <span className="text-xs text-text-muted italic">No description</span>
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
                  className={`p-1.5 rounded transition ${c.isActive ? "text-neutral-500 hover:text-orange-600 hover:bg-orange-50" : "text-neutral-500 hover:text-primary-600 hover:bg-primary-50"}`}
                >
                  <Power className="size-4.5" />
                </button>
                <button
                  onClick={() => onEdit(c)}
                  title="Edit Category"
                  className="p-1.5 text-neutral-500 hover:text-blue-600 hover:bg-blue-50 rounded transition"
                >
                  <Edit className="size-4.5" />
                </button>
                <button
                  onClick={() => onDelete(c.id)}
                  title="Delete Category"
                  className="p-1.5 text-neutral-500 hover:text-danger hover:bg-red-50 rounded transition"
                >
                  <Trash2 className="size-4.5" />
                </button>
              </div>
            </td>
          </tr>
        ))}
        {categories.length === 0 && (
          <tr>
            <td colSpan={4} className="p-10 text-center">
              <Layers className="mx-auto size-8 text-neutral-300 mb-3" />
              <p className="text-sm text-text-muted font-medium">
                No categories found matching your criteria.
              </p>
            </td>
          </tr>
        )}
      </tbody>
    </table>
  );
}
