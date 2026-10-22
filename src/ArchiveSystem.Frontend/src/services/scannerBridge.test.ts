import { afterEach, describe, expect, it, vi } from "vitest";
import { scannerBridge } from "./scannerBridge";

const jsonResponse = (payload: unknown, status = 200): Response =>
  new Response(JSON.stringify(payload), {
    status,
    headers: { "Content-Type": "application/json" },
  });

describe("scannerBridge", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("reads scanner devices from the local bridge contract", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({
        devices: [
          {
            id: "twain:abc",
            name: "RICOH fi-8170",
            driver: "twain",
            isDefault: true,
          },
        ],
      }),
    );
    vi.stubGlobal("fetch", fetchMock);

    await expect(scannerBridge.listDevices()).resolves.toEqual([
      {
        id: "twain:abc",
        name: "RICOH fi-8170",
        driver: "twain",
        isDefault: true,
      },
    ]);
    expect(fetchMock).toHaveBeenCalledWith(
      "http://127.0.0.1:17891/api/scanners",
      expect.objectContaining({ cache: "no-store", credentials: "omit" }),
    );
  });

  it("sends ADF settings and converts returned pages to files", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse({
        pages: [
          {
            base64: window.btoa("jpeg-page"),
            contentType: "image/jpeg",
            fileName: "scan-page-001.jpg",
          },
        ],
        partial: true,
        warning: "اسکن پس از صفحه اول متوقف شد.",
      }),
    );
    vi.stubGlobal("fetch", fetchMock);

    const result = await scannerBridge.scan({
      scannerId: "twain:abc",
      dpi: 300,
      colorMode: "grayscale",
      pageSize: "auto",
      paperSource: "duplex",
      autoDeskew: true,
    });

    expect(result.partial).toBe(true);
    expect(result.warning).toBe("اسکن پس از صفحه اول متوقف شد.");
    expect(result.files).toHaveLength(1);
    expect(result.files[0]).toEqual(
      expect.objectContaining({ name: "scan-page-001.jpg", type: "image/jpeg" }),
    );

    const [, request] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(JSON.parse(String(request.body))).toEqual({
      scannerId: "twain:abc",
      dpi: 300,
      colorMode: "grayscale",
      pageSize: "auto",
      paperSource: "duplex",
      autoDeskew: true,
      format: "jpeg",
    });
  });

  it("shows the safe Persian error returned by the bridge", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        jsonResponse(
          { code: "paper_jam", message: "کاغذ در اسکنر گیر کرده است." },
          409,
        ),
      ),
    );

    await expect(
      scannerBridge.scan({
        scannerId: "twain:abc",
        dpi: 300,
        colorMode: "color",
        pageSize: "a4",
        paperSource: "feeder",
        autoDeskew: true,
      }),
    ).rejects.toThrow("کاغذ در اسکنر گیر کرده است.");
  });
});
