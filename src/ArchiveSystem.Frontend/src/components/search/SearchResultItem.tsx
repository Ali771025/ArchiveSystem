import DescriptionOutlinedIcon from '@mui/icons-material/DescriptionOutlined';
import DownloadOutlinedIcon from '@mui/icons-material/DownloadOutlined';
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined';
import { Button, Paper, Stack, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { archiveApi } from '@/services/archiveApi';
import { useToastStore } from '@/stores/useToastStore';
import type { SearchResultItem } from '@/types/advancedSearch';

interface SearchResultItemProps {
  item: SearchResultItem;
  showCreatedAt?: boolean;
  showCreatedBy?: boolean;
}

export const SearchResultItemView = ({
  item,
  showCreatedAt = false,
  showCreatedBy = false,
}: SearchResultItemProps) => {
  const navigate = useNavigate();
  const pushToast = useToastStore((state) => state.push);
  const numericFileId = Number(item.id);
  const canDownload = Number.isFinite(numericFileId) && numericFileId > 0;

  const viewDossier = () => {
    const target = item.dossierNumber?.trim();
    if (!target) {
      pushToast('شماره پرونده برای مشاهده موجود نیست.', 'warning');
      return;
    }

    navigate(`/cases/${encodeURIComponent(target)}`);
  };

  const downloadFile = async () => {
    try {
      if (!canDownload) {
        throw new Error('invalid_file_id');
      }

      const url = await archiveApi.downloadFile(numericFileId);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = item.fileName || `file-${numericFileId}`;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    } catch {
      pushToast('دانلود فایل ناموفق بود.', 'error');
    }
  };

  return (
    <Paper variant="outlined" sx={{ p: 1.5 }}>
      <Stack spacing={0.5}>
        <Stack direction="row" spacing={1} alignItems="center">
          <DescriptionOutlinedIcon fontSize="small" color="action" />
          <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
            {item.fileName || 'فایل بدون نام'}
          </Typography>
        </Stack>

        <Typography variant="caption" color="text.secondary">
          شماره پرونده: {item.dossierNumber || '-'}
        </Typography>

        {showCreatedAt ? (
          <Typography variant="caption" color="text.secondary">
            تاریخ و ساعت فایل:{' '}
            {item.createdAt
              ? new Date(item.createdAt).toLocaleString('fa-IR')
              : '-'}
          </Typography>
        ) : null}

        {showCreatedBy ? (
          <Typography variant="caption" color="text.secondary">
            کاربر ثبت‌کننده: {item.createdBy || '-'}
          </Typography>
        ) : null}

        {item.highlights.length > 0 ? (
          <Stack spacing={0.5} sx={{ pt: 0.5 }}>
            {item.highlights.map((snippet, index) => (
              <Typography
                key={`${item.id}-highlight-${index}`}
                variant="body2"
                color="text.secondary"
                dangerouslySetInnerHTML={{ __html: snippet }}
              />
            ))}
          </Stack>
        ) : null}

        <Stack direction="row" spacing={1} sx={{ pt: 1 }}>
          <Button
            size="small"
            variant="outlined"
            startIcon={<VisibilityOutlinedIcon />}
            onClick={viewDossier}
          >
            مشاهده پرونده
          </Button>
          <Button
            size="small"
            variant="outlined"
            startIcon={<DownloadOutlinedIcon />}
            disabled={!canDownload}
            onClick={() => void downloadFile()}
          >
            دانلود فایل
          </Button>
        </Stack>
      </Stack>
    </Paper>
  );
};
