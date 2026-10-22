import AddIcon from '@mui/icons-material/Add';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import EditIcon from '@mui/icons-material/Edit';
import {
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Fab,
  IconButton,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
} from '@mui/material';
import { useState } from 'react';
import { AttachOwnerModal } from '@/components/owners/AttachOwnerModal';
import { CreateOwnerModal } from '@/components/owners/CreateOwnerModal';
import { useDossierOwners } from '@/hooks/useDossierOwners';
import { ownersApi } from '@/services/ownersApi';
import { useToastStore } from '@/stores/useToastStore';
import { OWNER_TYPE_LABELS, type AttachedOwner, type Owner } from '@/types/owners';

interface DossierOwnersTabProps {
  dossierId: number | string;
  caseNumber?: string | null;
  canWrite?: boolean;
  canEditOwner?: boolean;
}

export const DossierOwnersTab = ({
  dossierId,
  caseNumber,
  canWrite = true,
  canEditOwner = false,
}: DossierOwnersTabProps) => {
  const numericDossierId = typeof dossierId === 'string' ? Number(dossierId) : dossierId;
  const safeDossierId = Number.isFinite(numericDossierId) && numericDossierId > 0 ? numericDossierId : null;
  const [isAttachModalOpen, setIsAttachModalOpen] = useState(false);
  const [editingOwner, setEditingOwner] = useState<Owner | null>(null);
  const [ownerLoadingForEdit, setOwnerLoadingForEdit] = useState<number | null>(null);
  const [ownerPendingDelete, setOwnerPendingDelete] = useState<AttachedOwner | null>(null);
  const pushToast = useToastStore((state) => state.push);

  const { owners, ownersQuery, detachOwner, isDetachPending } = useDossierOwners(safeDossierId, caseNumber);

  const openEditModal = async (ownerId: number) => {
    if (!canEditOwner) {
      return;
    }

    setOwnerLoadingForEdit(ownerId);
    try {
      const owner = await ownersApi.getOwnerById(ownerId);
      setEditingOwner(owner);
    } catch {
      pushToast('دریافت اطلاعات مالک ناموفق بود.', 'error');
    } finally {
      setOwnerLoadingForEdit(null);
    }
  };

  const confirmDeleteOwner = async () => {
    if (!ownerPendingDelete) {
      return;
    }

    try {
      await detachOwner({ ownerId: ownerPendingDelete.id });
      setOwnerPendingDelete(null);
    } catch {
      // The mutation hook already shows the error toast and rolls back the cache.
    }
  };

  if (safeDossierId === null) {
    return (
      <Typography color="warning.main" variant="body2">
        شناسه پرونده معتبر نیست.
      </Typography>
    );
  }

  if (ownersQuery.isLoading) {
    return (
      <Box sx={{ minHeight: 260, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box sx={{ position: 'relative', minHeight: 320, pb: 8 }}>
      <TableContainer component={Paper} variant="outlined" sx={{ borderRadius: 2 }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell sx={{ fontWeight: 700 }}>نام/عنوان</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>کد ملی/شناسه ملی</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>نوع مودی</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>شماره تماس</TableCell>
              {canWrite && <TableCell align="center" sx={{ fontWeight: 700, width: 120 }}>عملیات</TableCell>}
            </TableRow>
          </TableHead>
          <TableBody>
            {owners.length === 0 ? (
              <TableRow>
                <TableCell colSpan={canWrite ? 5 : 4}>
                  <Box sx={{ py: 5, textAlign: 'center' }}>
                    <Typography color="text.secondary">هیچ مالکی به این پرونده متصل نیست.</Typography>
                  </Box>
                </TableCell>
              </TableRow>
            ) : (
              owners.map((owner) => (
                <TableRow key={owner.id} hover>
                  <TableCell>{owner.nameOrTitle || '-'}</TableCell>
                  <TableCell>{owner.nationalIdentifier || '-'}</TableCell>
                  <TableCell>{OWNER_TYPE_LABELS[owner.ownerType]}</TableCell>
                  <TableCell>{owner.mobile || owner.homePhone || '-'}</TableCell>
                  {canWrite && (
                    <TableCell align="center">
                      <Box sx={{ display: 'inline-flex', gap: 0.5 }}>
                        <Tooltip
                          title={
                            canEditOwner
                              ? "ویرایش مالک"
                              : "برای ویرایش مالک، دسترسی مدیریت مودیان لازم است."
                          }
                        >
                          <span>
                            <IconButton
                              size="small"
                              color="primary"
                              onClick={() => void openEditModal(owner.id)}
                              disabled={
                                !canEditOwner ||
                                ownerLoadingForEdit === owner.id ||
                                isDetachPending
                              }
                              aria-label={`ویرایش مالک ${owner.nameOrTitle || owner.id}`}
                            >
                              {ownerLoadingForEdit === owner.id ? <CircularProgress size={18} /> : <EditIcon fontSize="small" />}
                            </IconButton>
                          </span>
                        </Tooltip>
                        <Tooltip title="حذف مالک از پرونده">
                          <span>
                            <IconButton
                              size="small"
                              color="error"
                              onClick={() => setOwnerPendingDelete(owner)}
                              disabled={ownerLoadingForEdit === owner.id || isDetachPending}
                              aria-label={`حذف مالک ${owner.nameOrTitle || owner.id}`}
                            >
                              <DeleteOutlineIcon fontSize="small" />
                            </IconButton>
                          </span>
                        </Tooltip>
                      </Box>
                    </TableCell>
                  )}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {canWrite && (
        <Fab
          color="primary"
          aria-label="اتصال مودی"
          sx={{ position: 'absolute', right: 16, bottom: 16 }}
          onClick={() => setIsAttachModalOpen(true)}
        >
          <AddIcon />
        </Fab>
      )}

      <AttachOwnerModal
        isOpen={isAttachModalOpen}
        onClose={() => setIsAttachModalOpen(false)}
        dossierId={safeDossierId}
        caseNumber={caseNumber}
        onSuccess={() => {
          void ownersQuery.refetch();
        }}
      />

      <CreateOwnerModal
        isOpen={editingOwner !== null}
        onClose={() => setEditingOwner(null)}
        mode="edit"
        owner={editingOwner}
        onUpdated={() => {
          void ownersQuery.refetch();
        }}
      />

      <Dialog
        open={ownerPendingDelete !== null}
        onClose={() => {
          if (!isDetachPending) {
            setOwnerPendingDelete(null);
          }
        }}
        PaperProps={{ sx: { direction: 'rtl', borderRadius: 2, minWidth: { xs: 'calc(100vw - 32px)', sm: 420 } } }}
      >
        <DialogTitle sx={{ fontWeight: 700 }}>حذف مالک از پرونده</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary">
            آیا از حذف «{ownerPendingDelete?.nameOrTitle || 'این مالک'}» از این پرونده مطمئن هستید؟
          </Typography>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setOwnerPendingDelete(null)} color="inherit" disabled={isDetachPending}>
            انصراف
          </Button>
          <Button onClick={() => void confirmDeleteOwner()} color="error" variant="contained" disabled={isDetachPending}>
            {isDetachPending ? <CircularProgress size={18} color="inherit" /> : 'حذف'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};
