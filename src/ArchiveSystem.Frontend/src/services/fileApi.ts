import apiClient from "./apiClient";
import { SearchFilesResponse } from "../types";

const MAX_CONCURRENCY = 4;
const MAX_PART_RETRIES = 3;

interface UploadSessionState {
  sessionId: string;
  fileId: number;
  partSizeBytes: number;
  uploadedParts: Record<number, number>;
}

interface InitiateUploadResponse {
  sessionId: string;
  fileId: number;
  partSizeBytes: number;
  status: string;
  createdAt: string;
}

export interface FinalizeUploadResponse {
  fileId: number;
  versionId: number;
  versionNumber: number;
  sha256Checksum: string;
  sizeBytes: number;
  storagePath: string;
  alreadyFinalized: boolean;
}

const buildUploadStateKey = (
  file: File,
  options?: { caseId?: number; classificationId?: number; fileId?: number },
): string => {
  return [
    "multipart-upload",
    options?.caseId ?? "no-case",
    options?.classificationId ?? "no-classification",
    options?.fileId ?? "new-file",
    file.name,
    file.size,
    file.lastModified,
  ].join(":");
};

const toHex = (buffer: ArrayBuffer): string => {
  return Array.from(new Uint8Array(buffer))
    .map((b) => b.toString(16).padStart(2, "0"))
    .join("");
};

const toHexFromWords = (words: Uint32Array): string =>
  Array.from(words)
    .map((word) => word.toString(16).padStart(8, "0"))
    .join("");

const rightRotate = (value: number, bits: number): number =>
  (value >>> bits) | (value << (32 - bits));

const sha256Constants = new Uint32Array([
  0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b,
  0x59f111f1, 0x923f82a4, 0xab1c5ed5, 0xd807aa98, 0x12835b01,
  0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7,
  0xc19bf174, 0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc,
  0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da, 0x983e5152,
  0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147,
  0x06ca6351, 0x14292967, 0x27b70a85, 0x2e1b2138, 0x4d2c6dfc,
  0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
  0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819,
  0xd6990624, 0xf40e3585, 0x106aa070, 0x19a4c116, 0x1e376c08,
  0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f,
  0x682e6ff3, 0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208,
  0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2,
]);

const computeSha256Fallback = (buffer: ArrayBuffer): string => {
  const bytes = new Uint8Array(buffer);
  const bitLength = bytes.length * 8;
  const paddedLength = (((bytes.length + 9 + 63) >> 6) << 6);
  const padded = new Uint8Array(paddedLength);
  padded.set(bytes);
  padded[bytes.length] = 0x80;

  const view = new DataView(padded.buffer);
  view.setUint32(paddedLength - 8, Math.floor(bitLength / 0x100000000));
  view.setUint32(paddedLength - 4, bitLength >>> 0);

  const hash = new Uint32Array([
    0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a,
    0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19,
  ]);
  const words = new Uint32Array(64);

  for (let offset = 0; offset < paddedLength; offset += 64) {
    for (let index = 0; index < 16; index += 1) {
      words[index] = view.getUint32(offset + index * 4);
    }

    for (let index = 16; index < 64; index += 1) {
      const s0 =
        rightRotate(words[index - 15], 7) ^
        rightRotate(words[index - 15], 18) ^
        (words[index - 15] >>> 3);
      const s1 =
        rightRotate(words[index - 2], 17) ^
        rightRotate(words[index - 2], 19) ^
        (words[index - 2] >>> 10);
      words[index] =
        (words[index - 16] + s0 + words[index - 7] + s1) >>> 0;
    }

    let a = hash[0];
    let b = hash[1];
    let c = hash[2];
    let d = hash[3];
    let e = hash[4];
    let f = hash[5];
    let g = hash[6];
    let h = hash[7];

    for (let index = 0; index < 64; index += 1) {
      const s1 = rightRotate(e, 6) ^ rightRotate(e, 11) ^ rightRotate(e, 25);
      const ch = (e & f) ^ (~e & g);
      const temp1 = (h + s1 + ch + sha256Constants[index] + words[index]) >>> 0;
      const s0 = rightRotate(a, 2) ^ rightRotate(a, 13) ^ rightRotate(a, 22);
      const maj = (a & b) ^ (a & c) ^ (b & c);
      const temp2 = (s0 + maj) >>> 0;

      h = g;
      g = f;
      f = e;
      e = (d + temp1) >>> 0;
      d = c;
      c = b;
      b = a;
      a = (temp1 + temp2) >>> 0;
    }

    hash[0] = (hash[0] + a) >>> 0;
    hash[1] = (hash[1] + b) >>> 0;
    hash[2] = (hash[2] + c) >>> 0;
    hash[3] = (hash[3] + d) >>> 0;
    hash[4] = (hash[4] + e) >>> 0;
    hash[5] = (hash[5] + f) >>> 0;
    hash[6] = (hash[6] + g) >>> 0;
    hash[7] = (hash[7] + h) >>> 0;
  }

  return toHexFromWords(hash);
};

const computeSha256 = async (file: File): Promise<string> => {
  const data = await file.arrayBuffer();
  const subtle = globalThis.crypto?.subtle;
  if (!subtle?.digest) {
    return computeSha256Fallback(data);
  }

  const hash = await subtle.digest("SHA-256", data);
  return toHex(hash);
};

const uploadPartWithRetry = async (
  sessionId: string,
  partNumber: number,
  chunk: Blob,
): Promise<void> => {
  let attempt = 0;
  while (attempt < MAX_PART_RETRIES) {
    try {
      await apiClient.put(
        `/files/uploads/${sessionId}/parts/${partNumber}`,
        chunk,
        {
          headers: {
            "Content-Type": "application/octet-stream",
          },
        },
      );
      return;
    } catch (error) {
      attempt += 1;
      if (attempt >= MAX_PART_RETRIES) {
        throw error;
      }

      await new Promise((resolve) => setTimeout(resolve, 400 * attempt));
    }
  }
};

export const fileApi = {
  getFiles: async (params?: {
    term?: string;
    fromDate?: string;
    toDate?: string;
    page?: number;
    pageSize?: number;
    sortBy?: string;
    sortAscending?: boolean;
  }) => {
    const response = await apiClient.get<SearchFilesResponse>("/files", {
      params,
    });
    return response.data;
  },

  getFile: async (id: string) => {
    const response = await apiClient.get(`/files/${id}`);
    return response.data;
  },

  uploadFile: async (
    file: File,
    onProgress?: (progress: number) => void,
    options?: { caseId?: number; classificationId?: number; fileId?: number },
  ): Promise<FinalizeUploadResponse> => {
    const stateKey = buildUploadStateKey(file, options);
    const totalPartsFromState = localStorage.getItem(stateKey);

    let sessionState: UploadSessionState | null = totalPartsFromState
      ? (JSON.parse(totalPartsFromState) as UploadSessionState)
      : null;

    if (!sessionState) {
      const expectedSha256 = await computeSha256(file);
      const initiated = await apiClient.post<InitiateUploadResponse>(
        "/files/uploads/initiate",
        {
          originalFileName: file.name,
          contentType: file.type || "application/octet-stream",
          expectedSizeBytes: file.size,
          expectedSha256,
          fileId: options?.fileId,
          caseId: options?.caseId,
          classificationId: options?.classificationId,
        },
      );

      sessionState = {
        sessionId: initiated.data.sessionId,
        fileId: initiated.data.fileId,
        partSizeBytes: initiated.data.partSizeBytes,
        uploadedParts: {},
      };
      localStorage.setItem(stateKey, JSON.stringify(sessionState));
    }

    const totalParts = Math.ceil(file.size / sessionState.partSizeBytes);
    let completedBytes = Object.values(sessionState.uploadedParts).reduce(
      (sum, value) => sum + value,
      0,
    );

    const notifyProgress = () => {
      if (!onProgress) {
        return;
      }

      const progress =
        file.size === 0
          ? 100
          : Math.min(100, (completedBytes / file.size) * 100);
      onProgress(progress);
    };

    notifyProgress();

    const pendingParts: number[] = [];
    for (let partNumber = 1; partNumber <= totalParts; partNumber += 1) {
      if (!sessionState.uploadedParts[partNumber]) {
        pendingParts.push(partNumber);
      }
    }

    let cursor = 0;
    const runWorker = async () => {
      while (cursor < pendingParts.length) {
        const currentIndex = cursor;
        cursor += 1;

        const partNumber = pendingParts[currentIndex];
        const start = (partNumber - 1) * sessionState!.partSizeBytes;
        const end = Math.min(file.size, start + sessionState!.partSizeBytes);
        const chunk = file.slice(start, end);

        await uploadPartWithRetry(sessionState!.sessionId, partNumber, chunk);

        const chunkSize = end - start;
        if (!sessionState!.uploadedParts[partNumber]) {
          sessionState!.uploadedParts[partNumber] = chunkSize;
          completedBytes += chunkSize;
          localStorage.setItem(stateKey, JSON.stringify(sessionState));
          notifyProgress();
        }
      }
    };

    const workers = Array.from(
      { length: Math.min(MAX_CONCURRENCY, pendingParts.length || 1) },
      () => runWorker(),
    );
    await Promise.all(workers);

    const finalized = await apiClient.post<FinalizeUploadResponse>(
      `/files/uploads/${sessionState.sessionId}/complete`,
    );
    localStorage.removeItem(stateKey);
    notifyProgress();

    return finalized.data;
  },

  abortUpload: async (sessionId: string, reason?: string) => {
    await apiClient.post(`/files/uploads/${sessionId}/abort`, { reason });
  },

  listVersions: async (fileId: string) => {
    const response = await apiClient.get(`/files/${fileId}/versions`);
    return response.data;
  },

  deleteVersion: async (fileId: string, versionId: string) => {
    await apiClient.delete(`/files/${fileId}/versions/${versionId}`);
  },

  deleteFile: async (id: string) => {
    const response = await apiClient.delete(`/files/${id}`);
    return response.data;
  },

  searchFiles: async (query: string, filters?: any) => {
    const response = await apiClient.get<SearchFilesResponse>("/files", {
      params: {
        term: query,
        ...filters,
      },
    });
    return response.data;
  },

  getDownloadUrl: async (id: string) => {
    const response = await apiClient.get(`/files/${id}/download`, {
      responseType: "blob",
    });

    return {
      url: URL.createObjectURL(response.data as Blob),
    };
  },
};
