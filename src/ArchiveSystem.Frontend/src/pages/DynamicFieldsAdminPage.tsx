import { Alert, Box, Stack, Typography } from '@mui/material';
import { useMemo } from 'react';
import { CategoryRadioTreeView, type Category } from '@/components/dossiers/CategoryRadioTreeView';
import { DynamicTemplateBuilder } from '@/components/dynamic/DynamicTemplateBuilder';
import { useClassificationTree } from '@/services/archiveQueries';
import { useArchiveStore } from '@/stores/useArchiveStore';
import type { ClassificationNode } from '@/types/archive';
import { compareClassificationsArchiveLast } from '@/utils/classificationOrder';

const buildCategoryTree = (nodes: ClassificationNode[]): Category[] => {
  const byId = new Map<string, Category>();
  const roots: Category[] = [];

  nodes.forEach((node) => {
    byId.set(String(node.id), {
      id: String(node.id),
      name: node.title,
      systemKey: node.systemKey,
      children: [],
    });
  });

  nodes.forEach((node) => {
    const category = byId.get(String(node.id));
    if (!category) {
      return;
    }

    if (node.parentId == null) {
      roots.push(category);
      return;
    }

    const parent = byId.get(String(node.parentId));
    if (!parent) {
      roots.push(category);
      return;
    }

    parent.children = [...(parent.children ?? []), category];
  });

  const sortTree = (items: Category[]): Category[] =>
    items
      .sort(compareClassificationsArchiveLast)
      .map((item) => ({
        ...item,
        children: sortTree(item.children ?? []),
      }));

  return sortTree(roots);
};

export const DynamicFieldsAdminPage = () => {
  const classificationsQuery = useClassificationTree();
  const selectedClassificationId = useArchiveStore((state) => state.selectedClassificationId);
  const setSelectedClassificationId = useArchiveStore((state) => state.setSelectedClassificationId);

  const classifications = classificationsQuery.data ?? [];
  const categoryTree = useMemo(() => buildCategoryTree(classifications), [classifications]);

  return (
    <Stack spacing={1.5}>
      <Typography variant="h5">مدیریت اطلاعات تکمیلی</Typography>
      <Typography variant="body2" color="text.secondary">
        برای هر طبقه‌بندی می‌توانید گروه‌های فیلدی (تب‌ها) و فیلدهای پویا را مدیریت کنید.
      </Typography>

      <Box
        sx={{
          display: 'flex',
          flexDirection: { xs: 'column', xl: 'row' },
          alignItems: 'flex-start',
          gap: 2,
          width: '100%',
          minWidth: 0,
        }}
      >
        <Box
          component="aside"
          sx={{
            width: { xs: '100%', xl: 360 },
            flex: { xl: '0 0 360px' },
            minWidth: 0,
            position: { xl: 'sticky' },
            top: { xl: 16 },
          }}
        >
          {classificationsQuery.isLoading ? (
            <Alert severity="info">در حال بارگذاری طبقه‌بندی‌ها...</Alert>
          ) : (
            <CategoryRadioTreeView
              categories={categoryTree}
              value={selectedClassificationId == null ? null : String(selectedClassificationId)}
              label="انتخاب طبقه‌بندی برای مدیریت فیلدهای پویا"
              emptyText="طبقه‌بندی قابل مدیریت وجود ندارد."
              desktopHeight="calc(100dvh - 190px)"
              onChange={(id) => setSelectedClassificationId(Number(id))}
            />
          )}
        </Box>

        <Box component="main" sx={{ flex: 1, width: '100%', minWidth: 0 }}>
          {!selectedClassificationId ? (
            <Alert severity="info">ابتدا یک طبقه‌بندی انتخاب کنید.</Alert>
          ) : (
            <DynamicTemplateBuilder classificationId={selectedClassificationId} />
          )}
        </Box>
      </Box>
    </Stack>
  );
};
