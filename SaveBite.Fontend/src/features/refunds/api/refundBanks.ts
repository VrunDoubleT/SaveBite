export interface Bank {
  bin: string;
  name: string;
  shortName: string;
}
let request: Promise<Bank[]> | undefined;
export function banks() {
  if (!request)
    request = fetch("https://api.vietqr.io/v2/banks")
      .then(async (response) => {
        if (!response.ok) throw new Error("Banks unavailable");
        const result = (await response.json()) as { code?: string; data?: Bank[] };
        if (result.code !== "00" || !Array.isArray(result.data))
          throw new Error("Banks unavailable");
        return result.data
          .filter((bank) => /^[0-9]{6}$/.test(bank.bin))
          .sort((a, b) => a.shortName.localeCompare(b.shortName));
      })
      .catch((error) => {
        request = undefined;
        throw error;
      });
  return request;
}

export async function refundBankName(bin: string) {
  return (await banks()).find((bank) => bank.bin === bin)?.name ?? "Bank account";
}
