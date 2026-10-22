import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined';
import { Box, Chip, Divider, ListItemButton, Stack, Typography } from '@mui/material';
import { useMemo, useState } from 'react';
import { ClassificationTree } from '@/components/classification/ClassificationTree';
import { DynamicBreadcrumbs, type BreadcrumbPathItem } from '@/components/classification/DynamicBreadcrumbs';
import type { ClassificationTreeProps } from '@/components/classification/ClassificationTree';
import type { ClassificationNode } from '@/types/archive';
import {
  ARCHIVED_CASES_SYSTEM_KEY,
  compareClassificationsArchiveLast,
} from '@/utils/classificationOrder';

const STAGNANT_ARCHIVE_SYSTEM_KEY = ARCHIVED_CASES_SYSTEM_KEY;

interface ClassificationExplorerProps {
  nodes: ClassificationNode[];
  loading?: boolean;
  selectedId?: number | null;
  defaultSelectedId?: number | null;
  onSelectionChange?: (id: number | null) => void;
  onCreate: ClassificationTreeProps['onCreate'];
  onRename: ClassificationTreeProps['onRename'];
  onDelete: ClassificationTreeProps['onDelete'];
  onMove: ClassificationTreeProps['onMove'];
  hasCreatePermission: boolean;
  hasEditPermission: boolean;
  hasManagePermission: boolean;
}

const buildBreadcrumbItems = (
  selectedId: number | null,
  nodeMap: Map<number, ClassificationNode>,
): BreadcrumbPathItem[] => {
  if (selectedId == null) {
    return [];
  }

  const path: BreadcrumbPathItem[] = [];
  let pointer = nodeMap.get(selectedId);

  while (pointer) {
    path.unshift({ id: pointer.id, label: pointer.title });

    if (pointer.parentId == null) {
      break;
    }

    pointer = nodeMap.get(pointer.parentId);
  }

  return path;
};

export const ClassificationExplorer = ({
  nodes,
  loading = false,
  selectedId,
  defaultSelectedId = null,
  onSelectionChange,
  onCreate,
  onRename,
  onDelete,
  onMove,
  hasCreatePermission,
  hasEditPermission,
  hasManagePermission,
}: ClassificationExplorerProps) => {
  const [internalSelectedId, setInternalSelectedId] = useState<number | null>(defaultSelectedId);

  const activeSelectedId = selectedId === undefined ? internalSelectedId : selectedId;
  const nodeMap = useMemo(() => new Map(nodes.map((node) => [node.id, node])), [nodes]);
  const subjectNodes = useMemo(() => nodes.filter((node) => !node.systemKey), [nodes]);
  const systemNodes = useMemo(
    () =>
      nodes
        .filter((node) => node.systemKey)
        .sort(compareClassificationsArchiveLast),
    [nodes],
  );
  const treeSelectedId = activeSelectedId != null && !nodeMap.get(activeSelectedId)?.systemKey
    ? activeSelectedId
    : null;

  const breadcrumbItems = useMemo(
    () => buildBreadcrumbItems(activeSelectedId ?? null, nodeMap),
    [activeSelectedId, nodeMap],
  );

  const handleSelect = (id: number | null) => {
    if (selectedId === undefined) {
      setInternalSelectedId(id);
    }

    onSelectionChange?.(id);
  };

  return (
    <Stack spacing={1} sx={{ flex: 1, minHeight: 0, overflow: 'hidden' }}>
      <DynamicBreadcrumbs
        items={breadcrumbItems}
        onNavigate={handleSelect}
        emptyLabel="مسیر انتخاب‌شده در اینجا نمایش داده می‌شود"
      />

      <Box sx={{ flex: 1, minHeight: 0, display: 'flex' }}>
        <ClassificationTree
          nodes={subjectNodes}
          loading={loading}
          selectedId={treeSelectedId}
          onSelect={handleSelect}
          onCreate={onCreate}
          onRename={onRename}
          onDelete={onDelete}
          onMove={onMove}
          hasCreatePermission={hasCreatePermission}
          hasEditPermission={hasEditPermission}
          hasManagePermission={hasManagePermission}
        />
      </Box>

      {systemNodes.length > 0 ? (
        <Stack spacing={0.75} sx={{ flexShrink: 0, width: '100%' }}>
          <Divider flexItem />
          {systemNodes.map((node) => {
            const isSelected = activeSelectedId === node.id;
            const isStagnantArchive = node.systemKey === STAGNANT_ARCHIVE_SYSTEM_KEY;

            return (
              <ListItemButton
                key={node.id}
                dir="rtl"
                selected={isSelected}
                onClick={() => handleSelect(node.id)}
                sx={{
                  width: '100%',
                  minWidth: 0,
                  display: 'grid',
                  gridTemplateColumns: 'auto minmax(0, 1fr) auto',
                  columnGap: 1,
                  alignItems: 'center',
                  border: '1px solid',
                  borderColor: isSelected ? 'primary.main' : 'divider',
                  borderRadius: 1,
                  bgcolor: isSelected ? 'action.selected' : 'background.paper',
                  minHeight: 40,
                  opacity: node.isActive ? 1 : 0.82,
                  px: 1,
                  py: 0.75,
                }}
              >
                <Inventory2OutlinedIcon
                  fontSize="small"
                  sx={{
                    color: isSelected ? 'primary.main' : 'text.secondary',
                  }}
                />
                <Typography
                  variant="body2"
                  noWrap
                  sx={{
                    minWidth: 0,
                    flexGrow: 1,
                    justifySelf: 'stretch',
                    textAlign: 'start',
                    fontWeight: isStagnantArchive || isSelected ? 800 : 600,
                  }}
                >
                  {node.title}
                </Typography>
                <Chip
                  size="small"
                  label={node.caseCount}
                  variant="outlined"
                  sx={{
                    height: 20,
                    minWidth: 44,
                    flexShrink: 0,
                    color: isSelected ? 'primary.main' : 'text.secondary',
                    borderColor: isSelected ? 'primary.main' : 'divider',
                    backgroundColor: 'transparent',
                    '& .MuiChip-label': {
                      px: 1,
                      fontSize: 11,
                      fontVariantNumeric: 'tabular-nums',
                    },
                  }}
                />
              </ListItemButton>
            );
          })}
        </Stack>
      ) : null}
    </Stack>
  );
};
