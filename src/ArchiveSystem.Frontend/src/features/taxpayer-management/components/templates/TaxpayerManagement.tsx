import AddIcon from "@mui/icons-material/Add";
import RefreshIcon from "@mui/icons-material/Refresh";
import { Alert, Box, Button, Paper, Stack, Typography } from "@mui/material";
import { useMemo, useState } from "react";
import type { GridFilterModel, GridPaginationModel } from "@mui/x-data-grid";
import { TaxpayerFiltersSidebar } from "@/features/taxpayer-management/components/organisms/TaxpayerFiltersSidebar";
import { TaxpayerFormModal } from "@/features/taxpayer-management/components/organisms/TaxpayerFormModal";
import { TaxpayersDataGrid } from "@/features/taxpayer-management/components/organisms/TaxpayersDataGrid";
import {
  useCreateTaxpayerMutation,
  useDeleteTaxpayerMutation,
  useTaxpayersQuery,
  useUpdateTaxpayerMutation,
} from "@/features/taxpayer-management/hooks/useTaxpayersQuery";
import { useTaxpayerManagementStore } from "@/features/taxpayer-management/stores/useTaxpayerManagementStore";
import { mapApiError } from "@/services/errorMapper";
import { useToastStore } from "@/stores/useToastStore";
import { useDebouncedValue } from "@/utils/useDebouncedValue";
import type { TaxpayerListQueryParams } from "@/features/taxpayer-management/types";
import { useConfirm } from "@/components/common/useConfirm";
import { buildDeleteConfirmOptions } from "@/components/common/confirmPresets";
import type { TaxpayerListItemDTO } from "@/features/taxpayer-management/types";
import { getServerColumnFilter } from "@/utils/gridColumnFilter";

const getTaxpayerDisplayName = (taxpayer: TaxpayerListItemDTO) => {
  if (taxpayer.taxpayerType === "LegalEntity" || taxpayer.taxpayerType === 2) {
    return taxpayer.companyName?.trim() || "مودی حقوقی";
  }

  return (
    `${taxpayer.firstName ?? ""} ${taxpayer.lastName ?? ""}`.trim() ||
    "مودی حقیقی"
  );
};

export const TaxpayerManagement = () => {
  const {
    filters,
    pageNumber,
    pageSize,
    isModalOpen,
    selectedTaxpayerId,
    setFilter,
    resetFilters,
    setPagination,
    openCreateModal,
    openEditModal,
    closeModal,
  } = useTaxpayerManagementStore();
  const pushToast = useToastStore((state) => state.push);

  const debouncedFilters = useDebouncedValue(filters, 450);
  const [gridFilterModel, setGridFilterModel] = useState<GridFilterModel>({ items: [] });
  const debouncedGridFilter = useDebouncedValue(gridFilterModel, 350);

  const query: TaxpayerListQueryParams = useMemo(
    () => ({
      pageNumber,
      pageSize,
      firstName: debouncedFilters.firstName,
      lastName: debouncedFilters.lastName,
      nationalId: debouncedFilters.nationalId,
      ...getServerColumnFilter(debouncedGridFilter),
    }),
    [debouncedFilters, debouncedGridFilter, pageNumber, pageSize],
  );

  const taxpayersQuery = useTaxpayersQuery(query);
  const createMutation = useCreateTaxpayerMutation();
  const updateMutation = useUpdateTaxpayerMutation();
  const deleteMutation = useDeleteTaxpayerMutation();
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
            مدیریت مودیان
          </Typography>
          <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
            <Button
              variant="outlined"
              startIcon={<RefreshIcon />}
              onClick={() => taxpayersQuery.refetch()}
              disabled={taxpayersQuery.isFetching}
            >
              بازخوانی
            </Button>
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={openCreateModal}
            >
              ایجاد مودی جدید
            </Button>
          </Stack>
        </Stack>
      </Paper>

      <Stack spacing={1.5} alignItems="stretch">
        <Box sx={{ width: "100%", minWidth: 0 }}>
          <TaxpayerFiltersSidebar
            filters={filters}
            onFilterChange={setFilter}
            onResetFilters={resetFilters}
          />
        </Box>

        <Box sx={{ minWidth: 0, width: "100%" }}>
          {taxpayersQuery.isError ? (
            <Alert severity="error">
              {mapApiError(taxpayersQuery.error).message ||
                "بارگذاری مودیان ناموفق بود."}
            </Alert>
          ) : (
            <TaxpayersDataGrid
              data={taxpayersQuery.data}
              isLoading={taxpayersQuery.isLoading || taxpayersQuery.isFetching}
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
              onEdit={(taxpayer) => openEditModal(taxpayer.id)}
              onDelete={async (taxpayer) => {
                const fullName = getTaxpayerDisplayName(taxpayer);
                const shouldDelete = await confirm(
                  buildDeleteConfirmOptions(`مودی ${fullName}`),
                );
                if (!shouldDelete) {
                  return;
                }

                deleteMutation.mutate(taxpayer.id, {
                  onSuccess: () => {
                    pushToast(`مودی ${fullName} حذف شد.`, "success");
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

      <TaxpayerFormModal
        open={isModalOpen}
        taxpayerId={selectedTaxpayerId}
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
          if (!selectedTaxpayerId) {
            return;
          }

          await updateMutation.mutateAsync(
            { taxpayerId: selectedTaxpayerId, payload },
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
