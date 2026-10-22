import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { Alert, Button, Paper, Skeleton, Stack, Typography } from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { CaseListPanel } from '@/components/cases/CaseListPanel';
import { CaseDetailsTabs } from '@/components/cases/CaseDetailsTabs';
import { useCaseDetailsByNumber, useClassificationTree } from '@/services/archiveQueries';
import { useArchiveStore } from '@/stores/useArchiveStore';
import { mapApiError } from '@/services/errorMapper';
import { parseCaseListUrlState } from '@/utils/caseListUrlState';

const STAGNANT_ARCHIVE_SYSTEM_KEY = 'ArchivedCases';
const STAGNANT_ARCHIVE_TITLE = 'بایگانی راکد';
const STAGNANT_ARCHIVE_HELP_DISMISSED_KEY = 'archive:stagnant-archive-help-dismissed';

export const ArchiveWorkspacePage = () => {
  const navigate = useNavigate();
  const { caseId: routeCaseNumber } = useParams<{ caseId?: string }>();
  const [searchParams] = useSearchParams();
  const selectedCaseNumber = routeCaseNumber && routeCaseNumber !== 'undefined' ? routeCaseNumber : undefined;
  const storedClassificationId = useArchiveStore((state) => state.selectedClassificationId);
  const setSelectedClassificationId = useArchiveStore((state) => state.setSelectedClassificationId);
  const listUrlState = useMemo(() => parseCaseListUrlState(searchParams), [searchParams]);
  const selectedClassificationId = selectedCaseNumber
    ? storedClassificationId
    : listUrlState.classificationId;

  const classificationQuery = useClassificationTree();
  const caseByNumberQuery = useCaseDetailsByNumber(selectedCaseNumber ?? null);
  const selectedClassification = useMemo(
    () => classificationQuery.data?.find((node) => node.id === selectedClassificationId) ?? null,
    [classificationQuery.data, selectedClassificationId],
  );
  const isStagnantArchiveSelected =
    selectedClassification?.systemKey === STAGNANT_ARCHIVE_SYSTEM_KEY ||
    selectedClassification?.title.trim() === STAGNANT_ARCHIVE_TITLE;
  const [stagnantArchiveHelpDismissed, setStagnantArchiveHelpDismissed] = useState(() => {
    if (typeof window === 'undefined') {
      return false;
    }

    return window.localStorage.getItem(STAGNANT_ARCHIVE_HELP_DISMISSED_KEY) === 'true';
  });

  useEffect(() => {
    if (!selectedCaseNumber && storedClassificationId !== listUrlState.classificationId) {
      setSelectedClassificationId(listUrlState.classificationId);
    }
  }, [
    listUrlState.classificationId,
    selectedCaseNumber,
    setSelectedClassificationId,
    storedClassificationId,
  ]);

  const returnToList = () => {
    navigate({
      pathname: '/',
      search: searchParams.toString(),
    });
  };

  const dismissStagnantArchiveHelp = () => {
    setStagnantArchiveHelpDismissed(true);
    if (typeof window !== 'undefined') {
      window.localStorage.setItem(STAGNANT_ARCHIVE_HELP_DISMISSED_KEY, 'true');
    }
  };

  if (!selectedCaseNumber && selectedClassificationId == null) {
    return (
      <Paper variant="outlined" sx={{ p: { xs: 2, sm: 3, md: 4 } }}>
        <Stack spacing={1} alignItems="center">
          <Typography variant="h5">یک طبقه‌بندی انتخاب کنید</Typography>
          <Typography variant="body2" color="text.secondary">
            از پنل سمت راست، طبقه موردنظر را انتخاب کنید تا فهرست پرونده‌ها نمایش داده شود.
          </Typography>
        </Stack>
      </Paper>
    );
  }

  return (
    <Stack spacing={1.5}>
      {selectedCaseNumber ? (
        <>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }}>
            <Typography variant="h5">جزئیات پرونده</Typography>
            <Button startIcon={<ArrowBackIcon />} onClick={returnToList} sx={{ alignSelf: { xs: 'flex-start', sm: 'auto' } }}>
              بازگشت به فهرست پرونده‌ها
            </Button>
          </Stack>
          {caseByNumberQuery.isLoading ? (
            <Stack spacing={1}>
              <Skeleton variant="rounded" height={36} />
              <Skeleton variant="rounded" height={42} />
              <Skeleton variant="rounded" height={160} />
            </Stack>
          ) : caseByNumberQuery.data ? (
            <CaseDetailsTabs
              caseId={caseByNumberQuery.data.id}
              classifications={classificationQuery.data ?? []}
              onCancel={returnToList}
            />
          ) : (
            <Alert severity="error">
              {caseByNumberQuery.error ? mapApiError(caseByNumberQuery.error).message : 'پرونده موردنظر یافت نشد.'}
            </Alert>
          )}
        </>
      ) : (
        <>
          {isStagnantArchiveSelected && !stagnantArchiveHelpDismissed ? (
            <Alert severity="info" onClose={dismissStagnantArchiveHelp}>
              <Stack spacing={0.5}>
                <Typography variant="subtitle2" component="strong">
                  بایگانی راکد
                </Typography>
                <Typography variant="body2">
                  پرونده‌های موجود در این بخش از چرخه فعال خارج شده‌اند و صرفاً جهت نگهداری سوابق تاریخی و قانونی ذخیره می‌شوند. این پرونده‌ها جزئی از ساختار موضوعی آرشیو نیستند.
                </Typography>
              </Stack>
            </Alert>
          ) : null}
          <CaseListPanel classificationId={selectedClassificationId} />
        </>
      )}

      {classificationQuery.isError && (
        <Alert severity="error">
          {mapApiError(classificationQuery.error).message}
        </Alert>
      )}
    </Stack>
  );
};
