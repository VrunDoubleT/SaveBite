import type { ComponentProps } from "react";
import { Button, ButtonLink } from "@/shared/components/ui";
import { useAuthStore } from "@/shared/stores/authStore";

const message = "Customer features are suspended.";
function useCustomerSuspended() {
  return useAuthStore(
    (state) => state.isAuthenticated && state.user?.customerStatus === "suspended",
  );
}
export function CustomerActionButton({ disabled, ...props }: ComponentProps<typeof Button>) {
  const suspended = useCustomerSuspended();
  return (
    <Button
      {...props}
      disabled={disabled || suspended}
      title={suspended ? message : props.title}
      onClick={(event) => {
        if (useAuthStore.getState().user?.customerStatus === "suspended") {
          event.preventDefault();
          return;
        }
        props.onClick?.(event);
      }}
    />
  );
}
export function CustomerActionNativeButton({
  disabled,
  className = "",
  ...props
}: ComponentProps<"button">) {
  const suspended = useCustomerSuspended();
  return (
    <button
      {...props}
      disabled={disabled || suspended}
      title={suspended ? message : props.title}
      className={`${className} disabled:cursor-not-allowed disabled:opacity-50`}
      onClick={(event) => {
        if (useAuthStore.getState().user?.customerStatus === "suspended") {
          event.preventDefault();
          return;
        }
        props.onClick?.(event);
      }}
    />
  );
}
export function CustomerActionLink(props: ComponentProps<typeof ButtonLink>) {
  const suspended = useCustomerSuspended();
  if (suspended)
    return (
      <Button
        {...{ variant: props.variant, size: props.size, className: props.className }}
        disabled
        title={message}
      >
        {props.children}
      </Button>
    );
  return (
    <ButtonLink
      {...props}
      onClick={(event) => {
        if (useAuthStore.getState().user?.customerStatus === "suspended") {
          event.preventDefault();
          return;
        }
        props.onClick?.(event);
      }}
    />
  );
}
// Also guard Enter-to-submit and forms left open when a realtime suspension arrives.
export function CustomerMutationForm(props: ComponentProps<"form">) {
  useCustomerSuspended();
  return (
    <form
      {...props}
      onSubmit={(event) => {
        if (useAuthStore.getState().user?.customerStatus === "suspended") {
          event.preventDefault();
          event.stopPropagation();
          return;
        }
        props.onSubmit?.(event);
      }}
    />
  );
}

export function CustomerActionFileInput({
  disabled,
  ...props
}: Omit<ComponentProps<"input">, "type">) {
  const suspended = useCustomerSuspended();
  return (
    <input
      {...props}
      type="file"
      disabled={disabled || suspended}
      onChange={(event) => {
        if (useAuthStore.getState().user?.customerStatus === "suspended") {
          event.target.value = "";
          return;
        }
        props.onChange?.(event);
      }}
    />
  );
}
