import AddIcon from "@mui/icons-material/Add";
import RefreshIcon from "@mui/icons-material/Refresh";
import { Alert, Box, Button, Paper, Stack, Typography } from "@mui/material";
import type { GridFilterModel, GridPaginationModel } from "@mui/x-data-grid";
import { useMemo, useState } from "react";
import { CaseLoanFiltersSidebar } from "@/features/case-loan-management/components/organisms/CaseLoanFiltersSidebar";
import { CaseLoanModal } from "@/features/case-loan-management/components/organisms/CaseLoanModal";
import { CaseLoansDataGrid } from "@/features/case-loan-management/components/organisms/CaseLoansDataGrid";
import {
  useCaseLoansQuery,
  useCreateCaseLoanMutation,
  useDeleteCaseLoanMutation,
  useUpdateCaseLoanMutation,
} from "@/features/case-loan-management/hooks/useCaseLoansQuery";
import { useCaseLoanManagementStore } from "@/features/case-loan-management/stores/useCaseLoanManagementStore";
import type { CaseLoanListQueryParams } from "@/features/case-loan-management/types";
import { mapApiError } from "@/services/errorMapper";
import { useToastStore } from "@/stores/useToastStore";
import { useDebouncedValue } from "@/utils/useDebouncedValue";
import { useConfirm } from "@/components/common/useConfirm";
import { buildDeleteConfirmOptions } from "@/components/common/confirmPresets";
import { getServerColumnFilter } from "@/utils/gridColumnFilter";

export const CaseLoanManagement = () => {
  const {
    filters,
    pageNumber,
    pageSize,
    isModalOpen,
    selectedCaseLoanId,
    setFilter,
    resetFilters,
    setPagination,
    openCreateModal,
    openEditModal,
    closeModal,
  } = useCaseLoanManagementStore();
  const pushToast = useToastStore((state) => state.push);

  const debouncedFilters = useDebouncedValue(filters, 450);
  const [gridFilterModel, setGridFilterModel] = useState<GridFilterModel>({ items: [] });
  const debouncedGridFilter = useDebouncedValue(gridFilterModel, 350);

  const query: CaseLoanListQueryParams = useMemo(
    () => ({
      pageNumber,
      pageSize,
      caseNumber: debouncedFilters.caseNumber,
      nationalId: debouncedFilters.nationalId,
      borrowerLastName: debouncedFilters.borrowerLastName,
      ...getServerColumnFilter(debouncedGridFilter),
    }),
    [debouncedFilters, debouncedGridFilter, pageNumber, pageSize],
  );

  const caseLoansQuery = useCaseLoansQuery(query);
  const createMutation = useCreateCaseLoanMutation();
  const updateMutation = useUpdateCaseLoanMutation();
  const deleteMutation = useDeleteCaseLoanMutation();
  const confirm = useConfirm();

  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  const paginationModel: GridPaginationModel = useMemo(
    () => ({ page: pageNumber - 1, pageSize }),
    [pageNumber, pageSize],
  );

  return (
    <Stack spacing={1.5}>
      <Paper variant="outlined" sx={{ p: 2, borderRadius: 2 }}>
        <Stack
          direction={{ xs: "column", sm: "row" }}
          spacing={1}
          justifyContent="space-between"
          alignItems={{ xs: "stretch", sm: "center" }}
        >
          <Typography variant="h5" fontWeight={700}>
            مدیریت امانات پرونده فیزیکی
          </Typography>
          <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
            <Button
              variant="outlined"
              startIcon={<RefreshIcon />}
              onClick={() => caseLoansQuery.refetch()}
              disabled={caseLoansQuery.isFetching}
            >
              بازخوانی
            </Button>
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={openCreateModal}
            >
              ثبت امانت جدید
            </Button>
          </Stack>
        </Stack>
      </Paper>

      <Stack spacing={1.5} alignItems="stretch">
        <Box sx={{ width: "100%", minWidth: 0 }}>
          <CaseLoanFiltersSidebar
            filters={filters}
            onFilterChange={setFilter}
            onResetFilters={resetFilters}
          />
        </Box>

        <Box sx={{ minWidth: 0, width: "100%" }}>
          {caseLoansQuery.isError ? (
            <Alert severity="error">
              {mapApiError(caseLoansQuery.error).message ||
                "بارگذاری امانات پرونده ناموفق بود."}
            </Alert>
          ) : (
            <CaseLoansDataGrid
              data={caseLoansQuery.data}
              isLoading={caseLoansQuery.isLoading || caseLoansQuery.isFetching}
              paginationModel={paginationModel}
              onPaginationModelChange={(model) =>
                setPagination(
                  model.page + 1,
                  model.pageSize || paginationModel.pageSize,
                )
              }
              filterModel={gridFilterModel}
              onFilterModelChange={(model) => {
                setGridFilterModel(model);
                setPagination(1, pageSize);
              }}
              onEdit={(caseLoan) => openEditModal(caseLoan.id)}
              onDelete={async (caseLoan) => {
                const shouldDelete = await confirm(
                  buildDeleteConfirmOptions(
                    `امانت پرونده ${caseLoan.caseNumber}`,
                  ),
                );
                if (!shouldDelete) {
                  return;
                }

                deleteMutation.mutate(caseLoan.id, {
                  onSuccess: () => {
                    pushToast(
                      `امانت پرونده ${caseLoan.caseNumber} حذف شد.`,
                      "success",
                    );
                  },
                  onError: (error) => {
                    const mapped = mapApiError(error);
                    pushToast(mapped.message, "error");
                  },
                });
              }}
            />
          )}
        </Box>
      </Stack>

      <CaseLoanModal
        open={isModalOpen}
        caseLoanId={selectedCaseLoanId}
        onClose={closeModal}
        onCreate={async (payload) => {
          await createMutation.mutateAsync(payload, {
            onError: (error) => {
              const mapped = mapApiError(error);
              pushToast(mapped.message, "error");
            },
          });
        }}
        onUpdate={async (payload) => {
          if (!selectedCaseLoanId) {
            return;
          }

          await updateMutation.mutateAsync(
            { caseLoanId: selectedCaseLoanId, payload },
            {
              onError: (error) => {
                const mapped = mapApiError(error);
                pushToast(mapped.message, "error");
              },
            },
          );
        }}
        isSubmitting={isSubmitting}
      />
    </Stack>
  );
};
