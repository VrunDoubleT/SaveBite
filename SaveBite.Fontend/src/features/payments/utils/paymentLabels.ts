const types: Record<string, string> = { Full: "Full payment", Deposit: "Deposit", Remaining: "Remaining payment" };
export const paymentTypeLabel = (type: string) => types[type] ?? type;
export const paymentMethodLabel = (method: string) => method === "Cash" ? "Cash" : method === "OnlinePayment" ? "Online payment" : method === "PayAtShop" ? "Pay at shop" : method;
