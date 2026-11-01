import type { AttributeValueContent } from "../types/product.types";

export function attributeValueChanges(
  before: AttributeValueContent[] | null,
  after: AttributeValueContent[] | null,
) {
  const oldValues = new Map((before ?? []).map((v) => [v.valueId, v]));
  const newValues = new Map((after ?? []).map((v) => [v.valueId, v]));
  return [...new Set([...oldValues.keys(), ...newValues.keys()])].map((id) => {
    const oldValue = oldValues.get(id),
      newValue = newValues.get(id);
    return {
      id,
      before: oldValue,
      after: newValue,
      changed:
        !oldValue ||
        !newValue ||
        oldValue.attributeId !== newValue.attributeId ||
        oldValue.attributeName !== newValue.attributeName ||
        oldValue.value !== newValue.value ||
        oldValue.imageUrl !== newValue.imageUrl,
    };
  });
}
