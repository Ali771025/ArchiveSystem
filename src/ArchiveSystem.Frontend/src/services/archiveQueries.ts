import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { archiveApi } from "@/services/archiveApi";
import { useSessionStore } from "@/stores/useSessionStore";
import type {
  ArchiveTag,
  CaseDetails,
  CaseDynamicValue,
  CaseSummary,
  CaseSearchRequest,
  ClassificationAclEntryDto,
  DynamicTemplate,
  ExportJob,
  PaginatedResponse,
  TagAclEntryDto,
} from "@/types/archive";

export const queryKeys = {
  classifications: ["classifications"] as const,
  writableClassificationIds: ["writable-classification-ids"] as const,
  caseMoveAccessIds: ["case-move-access-ids"] as const,
  cases: (request: CaseSearchRequest) => ["cases", request] as const,
  caseDetails: (caseId: number | null) => ["caseDetails", caseId] as const,
  caseDetailsByNumber: (caseNumber: string | null) =>
    ["caseDetailsByNumber", caseNumber] as const,
  dynamicTemplate: (classificationId: number | null) =>
    ["dynamic-template", classificationId] as const,
  effectiveDynamicTemplate: (classificationId: number | null) =>
    ["effective-dynamic-template", classificationId] as const,
  caseDynamicValues: (caseId: number | null) =>
    ["case-dynamic-values", caseId] as const,
  caseFiles: (caseId: number | null) => ["case-files", caseId] as const,
  tags: ["tags"] as const,
  roles: ["roles"] as const,
  permissions: ["permissions"] as const,
  users: ["users"] as const,
  classificationAcl: (
    classificationId: number | null,
    includeDescendants = false,
  ) => ["classification-acl", classificationId, includeDescendants] as const,
  tagAcl: (tagId: number | null) => ["tag-acl", tagId] as const,
  activity: (page: number, pageSize: number) =>
    ["activity", page, pageSize] as const,
  exportJobs: ["export-jobs"] as const,
  behsazConfig: ["behsaz-config"] as const,
};

export const useClassificationTree = () =>
  useQuery({
    queryKey: queryKeys.classifications,
    queryFn: archiveApi.getClassificationTree,
  });

export const useWritableClassificationIds = () =>
  useQuery({
    queryKey: queryKeys.writableClassificationIds,
    queryFn: archiveApi.getWritableClassificationIds,
  });

export const useCaseMoveAccessIds = () =>
  useQuery({
    queryKey: queryKeys.caseMoveAccessIds,
    queryFn: archiveApi.getCaseMoveAccessIds,
  });

export const useCaseSearch = (request: CaseSearchRequest, enabled = true) =>
  useQuery({
    queryKey: queryKeys.cases(request),
    queryFn: () => archiveApi.searchCases(request),
    enabled,
  });

export const useCaseDetails = (caseId: number | null) =>
  useQuery({
    queryKey: queryKeys.caseDetails(caseId),
    queryFn: () => archiveApi.getCaseDetails(caseId ?? 0),
    enabled: Boolean(caseId),
  });

export const useCaseDetailsByNumber = (caseNumber: string | null) =>
  useQuery({
    queryKey: queryKeys.caseDetailsByNumber(caseNumber),
    queryFn: () => archiveApi.getCaseDetailsByNumber(caseNumber ?? ""),
    enabled: Boolean(caseNumber?.trim()),
  });

export const useDynamicTemplate = (classificationId: number | null) =>
  useQuery({
    queryKey: queryKeys.dynamicTemplate(classificationId),
    queryFn: () => archiveApi.getTemplate(classificationId ?? 0),
    enabled: Boolean(classificationId),
  });

export const useEffectiveDynamicTemplate = (classificationId: number | null) =>
  useQuery({
    queryKey: queryKeys.effectiveDynamicTemplate(classificationId),
    queryFn: () => archiveApi.getEffectiveTemplate(classificationId ?? 0),
    enabled: Boolean(classificationId),
  });

export const useCaseDynamicValues = (caseId: number | null) =>
  useQuery({
    queryKey: queryKeys.caseDynamicValues(caseId),
    queryFn: () => archiveApi.getCaseDynamicValues(caseId ?? 0),
    enabled: Boolean(caseId),
  });

export const useCaseFiles = (caseId: number | null) =>
  useQuery({
    queryKey: queryKeys.caseFiles(caseId),
    queryFn: () => archiveApi.getCaseFiles(caseId ?? 0),
    enabled: Boolean(caseId),
    refetchOnMount: "always",
    refetchInterval: (query) =>
      query.state.data?.some((file) =>
        file.processing.ocr === "Pending" || file.processing.ocr === "Processing")
        ? 3000
        : false,
  });

export const useTags = () => {
  const canLoadAdminTags = useSessionStore((state) =>
    state.hasPermission("admin.access"),
  );
  return useQuery({
    queryKey: queryKeys.tags,
    queryFn: archiveApi.getTags,
    enabled: canLoadAdminTags,
  });
};

export const useAssignableTags = () => {
  const canReadFiles = useSessionStore((state) =>
    state.hasPermission("files.read"),
  );
  return useQuery({
    queryKey: [...queryKeys.tags, "assignable"],
    queryFn: archiveApi.getAssignableTags,
    enabled: canReadFiles,
  });
};

export const useRoles = () =>
  useQuery({ queryKey: queryKeys.roles, queryFn: archiveApi.getRoles });

export const usePermissions = () =>
  useQuery({
    queryKey: queryKeys.permissions,
    queryFn: archiveApi.getPermissions,
  });

export const useUsers = () =>
  useQuery({ queryKey: queryKeys.users, queryFn: archiveApi.getUsers });

export const useClassificationAcl = (
  classificationId: number | null,
  includeDescendants = false,
) =>
  useQuery({
    queryKey: queryKeys.classificationAcl(classificationId, includeDescendants),
    queryFn: () =>
      archiveApi.getClassificationAcl(
        classificationId ?? 0,
        includeDescendants,
      ),
    enabled: Boolean(classificationId),
  });

export const useTagAcl = (tagId: number | null) =>
  useQuery({
    queryKey: queryKeys.tagAcl(tagId),
    queryFn: () => archiveApi.getTagAcl(tagId ?? 0),
    enabled: Boolean(tagId),
  });

export const useActivityReport = (page: number, pageSize: number) =>
  useQuery({
    queryKey: queryKeys.activity(page, pageSize),
    queryFn: () => archiveApi.getActivity(page, pageSize),
  });

export const useExportJobs = () =>
  useQuery({
    queryKey: queryKeys.exportJobs,
    queryFn: archiveApi.getExportJobs,
    refetchInterval: 3000,
  });

export const useCreateClassification = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: { title: string; parentId: number | null }) =>
      archiveApi.createClassification(payload.title, payload.parentId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: queryKeys.classifications,
        }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.writableClassificationIds,
        }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.caseMoveAccessIds,
        }),
        queryClient.invalidateQueries({
          queryKey: ["classification-acl"],
        }),
      ]);
    },
  });
};

export const useRenameClassification = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: { id: number; title: string }) =>
      archiveApi.renameClassification(payload.id, payload.title),
    onMutate: async ({ id, title }) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.classifications });
      const previous = queryClient.getQueryData(queryKeys.classifications);

      queryClient.setQueryData(
        queryKeys.classifications,
        (current: Array<{ id: number; title: string }> | undefined) =>
          (current ?? []).map((node) =>
            node.id === id ? { ...node, title } : node,
          ),
      );

      return { previous };
    },
    onError: (_error, _variables, context) => {
      if (context?.previous) {
        queryClient.setQueryData(queryKeys.classifications, context.previous);
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.classifications });
    },
  });
};

export const useMoveClassification = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: { id: number; parentId: number | null }) =>
      archiveApi.moveClassification(payload.id, payload.parentId),
    onMutate: async ({ id, parentId }) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.classifications });
      const previous = queryClient.getQueryData(queryKeys.classifications);

      queryClient.setQueryData(
        queryKeys.classifications,
        (current: Array<{ id: number; parentId: number | null }> | undefined) =>
          (current ?? []).map((node) =>
            node.id === id ? { ...node, parentId } : node,
          ),
      );

      return { previous };
    },
    onError: (_error, _variables, context) => {
      if (context?.previous) {
        queryClient.setQueryData(queryKeys.classifications, context.previous);
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.classifications });
    },
  });
};

export const useDeleteClassification = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => archiveApi.deleteClassification(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.classifications });
    },
  });
};

export const useSaveTemplate = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: {
      classificationId: number;
      template: DynamicTemplate;
    }) => archiveApi.saveTemplate(payload.classificationId, payload.template),
    onSuccess: (saved) => {
      queryClient.invalidateQueries({
        queryKey: queryKeys.dynamicTemplate(saved.classificationId),
      });
      queryClient.invalidateQueries({
        queryKey: ["effective-dynamic-template"],
      });
    },
  });
};

export const useSaveCaseDynamicValues = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: { caseId: number; values: CaseDynamicValue[] }) =>
      archiveApi.saveCaseDynamicValues(payload.caseId, payload.values),
    onSuccess: (savedValues, payload) => {
      queryClient.setQueryData(
        queryKeys.caseDynamicValues(payload.caseId),
        savedValues,
      );
    },
  });
};

export const useCreateCase = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (
      payload: Pick<
        CaseDetails,
        "classificationId" | "address" | "externalCaseNumber" | "postalCode"
      >,
    ) => archiveApi.createCase(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["cases"] });
      queryClient.invalidateQueries({ queryKey: queryKeys.classifications });
    },
  });
};

export const useUpdateCase = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: CaseDetails) => archiveApi.updateCase(payload),
    onSuccess: (updated) => {
      queryClient.invalidateQueries({
        queryKey: queryKeys.caseDetails(updated.id),
      });
      queryClient.invalidateQueries({ queryKey: ["cases"] });
    },
  });
};

export const useMoveCase = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: {
      caseId: number;
      classificationId: number;
      sourceClassificationId?: number;
    }) =>
      archiveApi.moveCase(
        payload.caseId,
        payload.classificationId,
        payload.sourceClassificationId,
      ),
    onSuccess: async (_, payload) => {
      await Promise.all([
        queryClient.refetchQueries({ queryKey: ["cases"], type: "active" }),
        queryClient.refetchQueries({
          queryKey: queryKeys.caseDetails(payload.caseId),
          type: "active",
        }),
        queryClient.refetchQueries({
          queryKey: ["caseDetailsByNumber"],
          type: "active",
        }),
        queryClient.refetchQueries({
          queryKey: queryKeys.classifications,
          type: "active",
        }),
      ]);
    },
  });
};

export const useDeleteCase = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (caseId: number) => archiveApi.deleteCase(caseId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["cases"] });
      queryClient.invalidateQueries({ queryKey: queryKeys.classifications });
    },
  });
};

export const useUploadCaseFile = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: {
      caseId: number;
      file: File;
      onProgress: (progress: number) => void;
      classificationId?: number;
      fileId?: number;
    }) =>
      archiveApi.uploadCaseFile(
        payload.caseId,
        payload.file,
        payload.onProgress,
        {
          classificationId: payload.classificationId,
          fileId: payload.fileId,
        },
      ),
    onSuccess: async (_, payload) => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: queryKeys.caseFiles(payload.caseId),
        }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.caseDetails(payload.caseId),
          exact: true,
        }),
        queryClient.invalidateQueries({
          queryKey: ["caseDetailsByNumber"],
          type: "active",
        }),
        queryClient.invalidateQueries({ queryKey: ["cases"] }),
      ]);
    },
  });
};

export const useUploadCaseAvatar = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: { caseId: number; file: File }) =>
      archiveApi.uploadCaseAvatar(payload.caseId, payload.file),
    onSuccess: async (result, payload) => {
      queryClient.setQueryData<CaseDetails | null>(
        queryKeys.caseDetails(payload.caseId),
        (current) =>
          current
            ? { ...current, avatarImagePath: result.avatarImagePath }
            : current,
      );
      queryClient.setQueriesData<PaginatedResponse<CaseSummary>>(
        { queryKey: ["cases"] },
        (current) =>
          current
            ? {
                ...current,
                items: current.items.map((item) =>
                  item.id === payload.caseId
                    ? { ...item, avatarImagePath: result.avatarImagePath }
                    : item,
                ),
              }
            : current,
      );

      await queryClient.invalidateQueries({
        queryKey: queryKeys.caseDetails(payload.caseId),
        exact: true,
      });
      await queryClient.invalidateQueries({
        queryKey: ["caseDetailsByNumber"],
      });
      await queryClient.invalidateQueries({ queryKey: ["cases"] });
    },
  });
};

export const useDeleteFile = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: { caseId: number; fileId: number }) =>
      archiveApi.deleteFile(payload.fileId),
    onSuccess: async (_, payload) => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: queryKeys.caseFiles(payload.caseId),
        }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.caseDetails(payload.caseId),
          exact: true,
        }),
        queryClient.invalidateQueries({
          queryKey: ["caseDetailsByNumber"],
          type: "active",
        }),
        queryClient.invalidateQueries({ queryKey: ["cases"] }),
      ]);
    },
  });
};

export const useUpdateFileTags = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: {
      caseId: number;
      fileId: number;
      tagIds: number[];
    }) => archiveApi.updateFileTags(payload.fileId, payload.tagIds),
    onSuccess: (_, payload) => {
      void Promise.all([
        queryClient.invalidateQueries({
          queryKey: queryKeys.caseFiles(payload.caseId),
        }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.tags,
        }),
      ]);
    },
  });
};

export const useSaveClassificationAcl = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: {
      classificationId: number;
      entries: ClassificationAclEntryDto[];
    }) =>
      archiveApi.saveClassificationAcl(
        payload.classificationId,
        payload.entries,
      ),
    onSuccess: (_, payload) => {
      queryClient.invalidateQueries({ queryKey: ["classification-acl"] });
      queryClient.invalidateQueries({
        queryKey: queryKeys.writableClassificationIds,
      });
      queryClient.invalidateQueries({ queryKey: queryKeys.caseMoveAccessIds });
    },
  });
};

export const useSaveTagAcl = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (payload: { tagId: number; entries: TagAclEntryDto[] }) =>
      archiveApi.saveTagAcl(payload.tagId, payload.entries),
    onSuccess: (_, payload) => {
      queryClient.invalidateQueries({
        queryKey: queryKeys.tagAcl(payload.tagId),
      });
    },
  });
};

export const useCreateRole = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (name: string) => archiveApi.createRole(name),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.roles });
    },
  });
};

export const useCreateExportJob = () => {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (
      request:
        | "activity"
        | "files"
        | {
            type: "files";
            caseId?: number | null;
            fileId?: number;
            classificationId?: number;
            tagId?: number;
            term?: string;
          },
    ) => {
      const created =
        request === "activity"
          ? await archiveApi.createActivityExport()
          : request === "files"
            ? await archiveApi.createFilesExport()
            : await archiveApi.createFilesExport(request);
      queryClient.setQueryData<ExportJob[]>(
        queryKeys.exportJobs,
        (current = []) => [
          created,
          ...current.filter((job) => job.id !== created.id),
        ],
      );

      let latest = created;
      for (let attempt = 0; attempt < 60; attempt += 1) {
        if (latest.status === "completed") {
          await archiveApi.downloadExportJob(latest.id);
          return latest;
        }

        if (latest.status === "failed" || latest.status === "expired") {
          throw new Error(
            latest.errorMessage || "Export job did not complete.",
          );
        }

        await new Promise((resolve) => window.setTimeout(resolve, 2000));
        latest = await archiveApi.getExportJob(created.id);
        queryClient.setQueryData<ExportJob[]>(
          queryKeys.exportJobs,
          (current = []) => [
            latest,
            ...current.filter((job) => job.id !== latest.id),
          ],
        );
      }

      throw new Error("Export job timed out before download was ready.");
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.exportJobs });
    },
  });
};

export const useSearchBehsaz = (
  query: { text: string; fromDate?: string; toDate?: string },
  enabled = true,
) =>
  useQuery({
    queryKey: ["behsaz", query],
    queryFn: () => archiveApi.searchBehsaz(query),
    enabled,
  });

export const useBehsazConfig = () =>
  useQuery({
    queryKey: queryKeys.behsazConfig,
    queryFn: archiveApi.getBehsazConfig,
  });

export const useTagsLookup = (): ArchiveTag[] => {
  const query = useTags();
  return query.data ?? [];
};
