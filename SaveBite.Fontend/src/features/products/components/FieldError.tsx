export function FieldError({ id, message }: { id: string; message?: string }) {
  return message ? (
    <p id={id} role="alert" className="mt-1 text-sm font-normal text-danger">
      {message}
    </p>
  ) : null;
}
