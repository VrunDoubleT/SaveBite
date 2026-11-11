import { feedbackApi } from "@/features/feedbacks/api/feedbackApi";
import { emitFeedbackSync } from "@/features/feedbacks/utils/feedbackSync";
import { refundApi } from "@/features/refunds/api/refundApi";
import { productApi } from "@/features/products/api/productApi";
import {
  appendAttributes,
  appendPrices,
  combinations,
  combinationKey,
} from "@/features/products/utils/productForm";
import type { DraftAttribute, PriceMap } from "@/features/products/types/product.types";
import { shopApplicationApi } from "@/features/shop-applications/api/shopApplicationApi";
import type {
  ShopApplicationForm,
  ShopDocumentUpload,
} from "@/features/shop-applications/types/shopApplication.types";
import type { UploadDraft } from "./uploadDraftStore";
import { uploadReferences } from "./uploadDrafts";

/** Publish already uploaded draft assets using the saved business form. */
export async function submitUploadDraft(draft: UploadDraft): Promise<void> {
  if (draft.files.some((file) => file.status !== "uploaded" || !file.url))
    throw new Error("Some files have not finished uploading.");
  if (draft.purpose === "feedback") {
    const form = draft.formData as {
      kind: string;
      feedbackId?: string;
      orderItemId?: string;
      rating: number;
      comment: string;
      images: string[];
      assetIds: string[];
      itemDraft?: { rating: number; comment: string; images: string[] };
    };
    const values = form.kind === "create-review" ? form.itemDraft : form;
    if (!values || !Array.isArray(form.assetIds))
      throw new Error("Please edit this review draft before submitting.");
    const images = [
      ...new Set([
        ...values.images,
        ...draft.files.filter((file) => form.assetIds.includes(file.id)).map((file) => file.url!),
      ]),
    ];
    if (images.length > 5)
      throw new Error("A review can contain at most 5 images. Please edit your draft.");
    const input = { rating: values.rating, comment: values.comment.trim() || undefined, images };
    if (form.kind === "create-review" && form.orderItemId)
      await feedbackApi.addFeedback({ ...input, orderItemId: form.orderItemId });
    else if (form.kind === "edit-review" && form.feedbackId)
      await feedbackApi.editFeedback(form.feedbackId, input);
    else throw new Error("This review draft cannot be submitted. Please edit it.");
    emitFeedbackSync("updated", form.feedbackId);
    return;
  }
  if (draft.purpose === "shop-application") {
    const form = draft.formData as {
      applicationId?: string;
      form: ShopApplicationForm;
      logo?: File;
      coverImage?: File;
      documents: ShopDocumentUpload[];
    };
    if (form.applicationId)
      await shopApplicationApi.resubmit(
        form.applicationId,
        form.form,
        form.logo,
        form.coverImage,
        form.documents,
        uploadReferences(draft.files),
      );
    else
      await shopApplicationApi.create(
        form.form,
        form.logo,
        form.coverImage,
        form.documents,
        uploadReferences(draft.files),
      );
    window.dispatchEvent(new Event("shop-application:updated"));
    return;
  }
  if (draft.purpose === "refund:request-evidence") {
    const form = draft.formData as {
      orderId: string;
      selected: Record<string, number>;
      reasons: Record<string, string>;
      evidence: Record<string, File[]>;
    };
    await refundApi.request(
      form.orderId,
      Object.entries(form.selected)
        .filter(([, quantity]) => quantity > 0)
        .map(([orderItemId, quantity]) => {
          const evidenceUrls = draft.files
            .filter((file) => file.field === `evidence:${orderItemId}`)
            .map((file) => file.url!);
          if (evidenceUrls.length !== (form.evidence[orderItemId]?.length ?? 0))
            throw new Error("Evidence photos do not match the saved form. Please edit it.");
          return { orderItemId, quantity, reason: form.reasons[orderItemId].trim(), evidenceUrls };
        }),
    );
    return;
  }
  if (draft.purpose === "product") {
    const form = draft.formData as {
      operation?: string;
      name: string;
      description: string;
      categoryId: string;
      attributes: DraftAttribute[];
      drafts: DraftAttribute[];
      prices: PriceMap;
      retainedImageIds: string[];
    };
    const body = new FormData();
    body.append("__uploadAssets", JSON.stringify(uploadReferences(draft.files)));
    if (form.operation === "gallery" && draft.targetId) {
      form.retainedImageIds.forEach((id, index) => body.append(`RetainedImageIds[${index}]`, id));
      await productApi.images(draft.targetId, body);
    } else if (form.operation === "add-values" && draft.targetId) {
      const product = await productApi.get(draft.targetId);
      const attributes = product.attributes.map((attribute) => ({
        ...attribute,
        values: [
          ...attribute.values,
          ...(form.drafts.find((entry) => entry.id === attribute.id)?.values ?? []),
        ],
      }));
      appendAttributes(body, form.drafts, true, false);
      const existing = new Set(product.variants.map((variant) => combinationKey(variant.valueIds)));
      const rows = combinations(attributes).rows.filter((row) => !existing.has(row.key));
      appendPrices(body, rows, form.prices);
      await productApi.addValues(draft.targetId, body);
    } else if (!form.operation && !draft.targetId) {
      body.append("Name", form.name.trim());
      body.append("Description", form.description.trim());
      body.append("CategoryId", form.categoryId);
      appendAttributes(body, form.attributes, false, false);
      appendPrices(body, combinations(form.attributes).rows, form.prices);
      await productApi.create(body);
    } else throw new Error("Please edit this product draft before submitting.");
    return;
  }
  throw new Error("Please open this draft to complete the form.");
}
