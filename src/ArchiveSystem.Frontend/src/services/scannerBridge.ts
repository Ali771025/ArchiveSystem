const configuredScannerBridgeUrl =
  import.meta.env.VITE_SCANNER_BRIDGE_URL ?? "http://127.0.0.1:17891";

const scannerBridgeUrl = configuredScannerBridgeUrl.replace(/\/+$/, "");

export interface ScannerDevice {
  id: string;
  name: string;
  driver?: "twain" | "wia" | string;
  isDefault?: boolean;
}

export type ScannerColorMode = "color" | "grayscale" | "black-and-white";
export type ScannerPaperSource = "auto" | "flatbed" | "feeder" | "duplex";

export interface ScannerSettings {
  scannerId: string;
  dpi: number;
  colorMode: ScannerColorMode;
  pageSize: "a4" | "auto";
  paperSource: ScannerPaperSource;
  autoDeskew: boolean;
}

interface ScannerPagePayload {
  base64: string;
  contentType?: string;
  fileName?: string;
}

interface ScannerResultPayload {
  pages: ScannerPagePayload[];
  partial?: boolean;
  warning?: string;
}

interface BridgeErrorPayload {
  message?: string;
  error?: string;
}

export interface ScannerScanResult {
  files: File[];
  partial: boolean;
  warning?: string;
}

const scannerUnavailableMessage =
  "ارتباط با سرویس محلی اسکنر برقرار نشد. Scanner Bridge را روی این رایانه اجرا کنید و از اتصال اسکنر مطمئن شوید.";

class ScannerBridgeRequestError extends Error {}

const readBridgeError = async (response: Response): Promise<string> => {
  try {
    const payload = (await response.json()) as BridgeErrorPayload;
    return payload.message || payload.error || "";
  } catch {
    return "";
  }
};

const request = async (
  path: string,
  init?: RequestInit,
  timeoutMs = 20_000,
): Promise<Response> => {
  const controller = new AbortController();
  const timeoutId = window.setTimeout(() => controller.abort(), timeoutMs);

  try {
    const response = await fetch(`${scannerBridgeUrl}${path}`, {
      ...init,
      cache: "no-store",
      credentials: "omit",
      signal: controller.signal,
      headers: {
        Accept: "application/json, image/*",
        ...init?.headers,
      },
    });

    if (!response.ok) {
      const serverMessage = await readBridgeError(response);
      throw new ScannerBridgeRequestError(
        serverMessage ||
          (response.status === 404
            ? "سرویس اسکنر یا مسیر درخواست‌شده در دسترس نیست."
            : response.status === 409
              ? "اسکنر در حال استفاده است یا برای عملیات انتخاب‌شده آماده نیست."
              : `سرویس اسکنر با خطای ${response.status} پاسخ داد.`),
      );
    }

    return response;
  } catch (error) {
    if (error instanceof ScannerBridgeRequestError) {
      throw error;
    }

    if (error instanceof DOMException && error.name === "AbortError") {
      throw new ScannerBridgeRequestError(
        "پاسخی از اسکنر دریافت نشد و زمان عملیات به پایان رسید.",
      );
    }

    throw new ScannerBridgeRequestError(scannerUnavailableMessage);
  } finally {
    window.clearTimeout(timeoutId);
  }
};

const base64ToBlob = (value: string, contentType: string): Blob => {
  const normalizedValue = value.includes(",")
    ? value.slice(value.indexOf(",") + 1)
    : value;
  const binary = window.atob(normalizedValue);
  const bytes = new Uint8Array(binary.length);

  for (let index = 0; index < binary.length; index += 1) {
    bytes[index] = binary.charCodeAt(index);
  }

  return new Blob([bytes], { type: contentType });
};

const buildScannedFileName = (
  pageNumber: number,
  contentType: string,
  suppliedName?: string,
): string => {
  if (suppliedName?.trim()) {
    return suppliedName.trim();
  }

  const extension = contentType.includes("png") ? "png" : "jpg";
  const timestamp = new Date()
    .toISOString()
    .replace(/:/g, "-")
    .replace(/\.\d{3}Z$/, "");

  return `scan-${timestamp}-page-${pageNumber}.${extension}`;
};

export const scannerBridge = {
  get baseUrl(): string {
    return scannerBridgeUrl;
  },

  async listDevices(): Promise<ScannerDevice[]> {
    const response = await request("/api/scanners");
    const payload = (await response.json()) as
      | ScannerDevice[]
      | { devices?: ScannerDevice[] };
    const devices = Array.isArray(payload) ? payload : (payload.devices ?? []);

    return devices.filter(
      (device) =>
        typeof device.id === "string" &&
        device.id.length > 0 &&
        typeof device.name === "string" &&
        device.name.length > 0,
    );
  },

  async scan(settings: ScannerSettings): Promise<ScannerScanResult> {
    const response = await request(
      "/api/scan",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          ...settings,
          format: "jpeg",
        }),
      },
      330_000,
    );
    const contentType = response.headers.get("content-type") ?? "";

    if (!contentType.includes("application/json")) {
      const blob = await response.blob();
      const resolvedContentType = blob.type || "image/jpeg";
      return {
        files: [
          new File([blob], buildScannedFileName(1, resolvedContentType), {
            type: resolvedContentType,
          }),
        ],
        partial: false,
      };
    }

    const payload = (await response.json()) as ScannerResultPayload;
    if (!Array.isArray(payload.pages) || payload.pages.length === 0) {
      throw new Error("سرویس اسکنر هیچ تصویری برنگرداند.");
    }

    const files = payload.pages.map((page, index) => {
      if (!page.base64) {
        throw new Error("داده تصویر دریافت‌شده از اسکنر معتبر نیست.");
      }

      const pageContentType = page.contentType || "image/jpeg";
      const blob = base64ToBlob(page.base64, pageContentType);
      return new File(
        [blob],
        buildScannedFileName(index + 1, pageContentType, page.fileName),
        { type: pageContentType },
      );
    });

    return {
      files,
      partial: payload.partial === true,
      warning: payload.warning,
    };
  },
};
