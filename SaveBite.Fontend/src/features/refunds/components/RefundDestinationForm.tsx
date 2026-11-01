import { useState } from "react";
import { RefundBankSelect } from "./RefundBankSelect";
import { refundBankName } from "../api/refundBanks";
import { uploadRefundFiles } from "../api/uploadRefundFiles";
import { RefundFileUpload } from "./RefundFileUpload";
import { refundApi } from "../api/refundApi";
import type { Refund, RefundBank } from "../types/refund.types";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
export function RefundDestinationForm({
  refund,
  busy,
  mutate,
}: {
  refund: Refund;
  busy: boolean;
  mutate: (action: () => Promise<Refund>) => Promise<void>;
}) {
  const [bank, setBank] = useState<RefundBank>({
    receiverMethod: refund.receiverMethod ?? "BankAccount",
    qrImageUrl: refund.receiverQrImageUrl,
    bankBin: refund.bankBin ?? "",
    bankName: refund.bankName ?? "",
    accountNumber: refund.accountNumber ?? "",
    accountHolder: refund.accountHolder ?? "",
  });
  const [files, setFiles] = useState<File[]>([]);
  const [code, setCode] = useState("");
  const [errors, setErrors] = useState<Record<string, string | undefined>>({});
  function clear(field: string) {
    setErrors((old) => ({ ...old, [field]: undefined }));
  }
  const disabled = busy;
  const qr = bank.receiverMethod === "QrImage";
  const input = "mt-1 w-full rounded-lg border border-neutral-200 bg-white px-3 py-2 text-sm";
  return (
    <div className="mt-4 border-t border-neutral-100 pt-4">
      {refund.customerDetailsDeadlineAt && (
        <p className="mb-3 rounded-lg bg-amber-50 p-3 text-xs font-medium text-amber-900">
          Confirm your refund destination by {checkoutTime(refund.customerDetailsDeadlineAt)} ·
          GMT+7. After this deadline, you lose eligibility for this refund.
        </p>
      )}
      <p className="text-xs text-neutral-500">
        Choose how to provide your refund destination. Confirm it using the code sent to your email.
      </p>
      <div className="my-3 flex gap-2">
        {(["BankAccount", "QrImage"] as const).map((method) => (
          <button
            type="button"
            key={method}
            disabled={disabled}
            onClick={() => {
              setBank({ ...bank, receiverMethod: method });
              setErrors({});
            }}
            className={`rounded-lg border px-3 py-2 text-xs font-medium ${bank.receiverMethod === method ? "border-emerald-300 bg-emerald-50 text-emerald-800" : "border-neutral-200 text-neutral-500"}`}
          >
            {method === "BankAccount" ? "Enter bank details" : "Upload bank QR"}
          </button>
        ))}
      </div>
      <form
        noValidate
        className="grid gap-3 sm:grid-cols-2"
        onSubmit={(event) => {
          event.preventDefault();
          const validation = {
            bank: !qr && !bank.bankBin ? "Select your bank." : undefined,
            account:
              !qr && !/^[0-9]{1,50}$/.test(bank.accountNumber)
                ? "Enter an account number containing 1–50 digits."
                : undefined,
            holder: !bank.accountHolder.trim()
              ? "Enter the account holder's name."
              : bank.accountHolder.trim().length > 150
                ? "Use no more than 150 characters."
                : undefined,
            qr: qr && !files.length && !bank.qrImageUrl ? "Choose your bank QR image." : undefined,
          };
          setErrors(validation);
          if (Object.values(validation).some(Boolean)) return;
          setCode("");
          void mutate(async () => {
            let destination = { ...bank, accountHolder: bank.accountHolder.trim() };
            if (qr && files.length) {
              const [image] = await uploadRefundFiles(refund.orderId, files, "destination-qr");
              const name = await refundBankName(image.bankBin!).catch(() => "Bank account");
              destination = {
                ...bank,
                qrImageUrl: image.url,
                bankBin: image.bankBin!,
                bankName: name,
                accountNumber: image.accountNumber!,
                accountHolder: bank.accountHolder.trim() || image.accountHolder || "",
              };
              setBank(destination);
            }
            return refundApi.destination(refund.id, destination);
          });
        }}
      >
        {qr ? (
          <div className="space-y-2 sm:col-span-2">
            <RefundFileUpload
              purpose="destination-qr"
              max={1}
              files={files}
              disabled={disabled}
              onChange={(selected) => {
                setFiles(selected);
                clear("qr");
                setBank((old) => ({
                  ...old,
                  qrImageUrl: undefined,
                  bankBin: "",
                  bankName: "",
                  accountNumber: "",
                }));
              }}
            />
            {errors.qr && (
              <p role="alert" className="text-xs text-red-600">
                {errors.qr}
              </p>
            )}
            {files.length > 0 && (
              <p className="text-xs text-neutral-500">
                Bank details will be read from this QR when you submit.
              </p>
            )}
            {bank.accountNumber && (
              <p className="rounded-lg bg-emerald-50 p-3 text-xs text-emerald-800">
                Account detected: {bank.bankName} · {bank.accountNumber}. Check the account holder
                below.
              </p>
            )}
          </div>
        ) : (
          <>
            <RefundBankSelect
              validationError={errors.bank}
              value={bank.bankBin}
              disabled={disabled}
              onChange={(bin, name) => {
                setBank({ ...bank, bankBin: bin, bankName: name });
                clear("bank");
              }}
            />
            <label className="text-xs font-medium">
              Account number
              <input
                required
                inputMode="numeric"
                pattern="[0-9]{1,50}"
                maxLength={50}
                aria-invalid={!!errors.account}
                className={`${input} ${errors.account ? "border-red-500" : ""}`}
                value={bank.accountNumber}
                disabled={disabled}
                onChange={(event) => {
                  setBank({ ...bank, accountNumber: event.target.value });
                  clear("account");
                }}
              />
              {errors.account && (
                <p role="alert" className="mt-1 text-xs text-red-600">
                  {errors.account}
                </p>
              )}
            </label>
          </>
        )}
        <label className="text-xs font-medium sm:col-span-2">
          Account holder
          <input
            required
            maxLength={150}
            aria-invalid={!!errors.holder}
            className={`${input} ${errors.holder ? "border-red-500" : ""}`}
            value={bank.accountHolder}
            disabled={disabled}
            onChange={(event) => {
              setBank({ ...bank, accountHolder: event.target.value });
              clear("holder");
            }}
          />
          {errors.holder && (
            <p role="alert" className="mt-1 text-xs text-red-600">
              {errors.holder}
            </p>
          )}
        </label>
        <button
          disabled={disabled}
          className="justify-self-start rounded-lg bg-emerald-700 px-3 py-2 text-xs font-semibold text-white disabled:opacity-50 sm:col-span-2"
        >
          {refund.confirmationExpiresAt ? "Resend confirmation email" : "Send confirmation email"}
        </button>
      </form>
      {refund.confirmationExpiresAt && (
        <form
          noValidate
          className="mt-4 flex flex-wrap items-end gap-2 border-t border-neutral-100 pt-4"
          onSubmit={(event) => {
            event.preventDefault();
            if (!/^[0-9]{6}$/.test(code)) {
              setErrors((old) => ({ ...old, code: "Enter the 6-digit code from your email." }));
              return;
            }
            clear("code");
            void mutate(() => refundApi.confirm(refund.id, code));
          }}
        >
          <label className="text-xs font-medium">
            Email code
            <input
              required
              inputMode="numeric"
              pattern="[0-9]{6}"
              maxLength={6}
              aria-invalid={!!errors.code}
              className={`${input} ${errors.code ? "border-red-500" : ""}`}
              value={code}
              disabled={disabled}
              onChange={(event) => {
                setCode(event.target.value);
                clear("code");
              }}
            />
            {errors.code && (
              <p role="alert" className="mt-1 text-xs text-red-600">
                {errors.code}
              </p>
            )}
          </label>
          <button
            disabled={disabled}
            className="rounded-lg bg-emerald-700 px-3 py-2 text-xs font-semibold text-white disabled:opacity-50"
          >
            Confirm destination
          </button>
          <p className="w-full text-xs text-neutral-400">
            Code expires {checkoutTime(refund.confirmationExpiresAt)} · GMT+7
          </p>
        </form>
      )}
    </div>
  );
}
