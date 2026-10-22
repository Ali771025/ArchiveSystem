import { beforeEach, describe, expect, it } from "vitest";
import { useArchiveStore } from "@/stores/useArchiveStore";

describe("useArchiveStore upload isolation", () => {
  beforeEach(() => {
    useArchiveStore.setState({ uploads: [] });
  });

  it("clears upload tasks only for the requested case", () => {
    const { upsertUploadTask, clearUploadTasks } = useArchiveStore.getState();

    upsertUploadTask({
      id: "case-10-upload",
      caseId: 10,
      fileName: "first.pdf",
      progress: 100,
      status: "completed",
    });
    upsertUploadTask({
      id: "case-20-upload",
      caseId: 20,
      fileName: "second.pdf",
      progress: 25,
      status: "uploading",
    });

    clearUploadTasks(10);

    expect(useArchiveStore.getState().uploads).toEqual([
      expect.objectContaining({ id: "case-20-upload", caseId: 20 }),
    ]);
  });
});
