import AddIcon from '@mui/icons-material/Add';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import FolderOpenOutlinedIcon from '@mui/icons-material/FolderOpenOutlined';
import FolderOutlinedIcon from '@mui/icons-material/FolderOutlined';
import LockOutlinedIcon from '@mui/icons-material/LockOutlined';
import MoreHorizIcon from '@mui/icons-material/MoreHoriz';
import SearchIcon from '@mui/icons-material/Search';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Collapse,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  ListItemButton,
  Menu,
  MenuItem,
  Stack,
  TextField,
  Typography,
  useTheme,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import { useDebouncedValue } from '@/utils/useDebouncedValue';
import type { ClassificationNode } from '@/types/archive';
import { mapApiError } from '@/services/errorMapper';
import { compareClassificationsArchiveLast } from '@/utils/classificationOrder';

interface TreeNode extends ClassificationNode {
  children: TreeNode[];
}

interface LineGuidesProps {
  ancestryHasNext: boolean[];
  isRoot: boolean;
  isLast: boolean;
}

export interface ClassificationTreeProps {
  nodes: ClassificationNode[];
  selectedId: number | null;
  loading?: boolean;
  onSelect: (id: number | null) => void;
  onCreate: (title: string, parentId: number | null) => Promise<void>;
  onRename: (id: number, title: string) => Promise<void>;
  onDelete: (id: number) => Promise<void>;
  onMove: (id: number, parentId: number | null) => Promise<void>;
  hasCreatePermission: boolean;
  hasEditPermission: boolean;
  hasManagePermission: boolean;
}

const INDENT_UNIT = 18;
const isSystemNode = (node?: ClassificationNode | null): boolean =>
  Boolean(node?.systemKey);

const buildTree = (nodes: ClassificationNode[]): TreeNode[] => {
  const map = new Map<number, TreeNode>();

  nodes.forEach((node) => {
    map.set(node.id, { ...node, children: [] });
  });

  const roots: TreeNode[] = [];

  map.forEach((node) => {
    if (node.parentId == null) {
      roots.push(node);
      return;
    }

    const parent = map.get(node.parentId);
    if (!parent) {
      roots.push(node);
      return;
    }

    parent.children.push(node);
  });

  const sortTree = (items: TreeNode[]): TreeNode[] => {
    items.sort(compareClassificationsArchiveLast);
    items.forEach((item) => {
      item.children = sortTree(item.children);
    });
    return items;
  };

  return sortTree(roots);
};

const flattenVisibleIds = (roots: TreeNode[], expanded: Set<number>): number[] => {
  const ids: number[] = [];

  const walk = (items: TreeNode[]) => {
    items.forEach((item) => {
      ids.push(item.id);
      if (expanded.has(item.id)) {
        walk(item.children);
      }
    });
  };

  walk(roots);
  return ids;
};

const findAncestorIds = (nodeId: number, nodeMap: Map<number, ClassificationNode>): number[] => {
  const ancestors: number[] = [];
  let pointer = nodeMap.get(nodeId);

  while (pointer?.parentId != null) {
    ancestors.unshift(pointer.parentId);
    pointer = nodeMap.get(pointer.parentId);
  }

  return ancestors;
};

const isDescendant = (candidateId: number, ancestorId: number, nodeMap: Map<number, ClassificationNode>): boolean => {
  let pointer = nodeMap.get(candidateId);

  while (pointer?.parentId != null) {
    if (pointer.parentId === ancestorId) {
      return true;
    }
    pointer = nodeMap.get(pointer.parentId);
  }

  return false;
};

const LineGuides = ({ ancestryHasNext, isRoot, isLast }: LineGuidesProps) => (
  <Box sx={{ display: 'flex', flexShrink: 0 }} aria-hidden>
    {ancestryHasNext.map((hasNext, index) => (
      <Box key={`${index}-${hasNext}`} sx={{ width: INDENT_UNIT, position: 'relative' }}>
        {hasNext ? (
          <Box
            sx={{
              position: 'absolute',
              top: -8,
              bottom: -8,
              left: '50%',
              transform: 'translateX(-50%)',
              borderLeft: '1px solid',
              borderColor: 'divider',
              opacity: 0.8,
            }}
          />
        ) : null}
      </Box>
    ))}

    {isRoot ? null : (
      <Box sx={{ width: INDENT_UNIT, position: 'relative' }}>
        <Box
          sx={{
            position: 'absolute',
            left: '50%',
            top: -8,
            bottom: isLast ? '50%' : -8,
            transform: 'translateX(-50%)',
            borderLeft: '1px solid',
            borderColor: 'divider',
            opacity: 0.8,
          }}
        />
        <Box
          sx={{
            position: 'absolute',
            top: '50%',
            left: '50%',
            width: '50%',
            borderTop: '1px solid',
            borderColor: 'divider',
            opacity: 0.8,
          }}
        />
      </Box>
    )}
  </Box>
);

export const ClassificationTree = ({
  nodes,
  selectedId,
  loading = false,
  onSelect,
  onCreate,
  onRename,
  onDelete,
  onMove,
  hasCreatePermission,
  hasEditPermission,
  hasManagePermission,
}: ClassificationTreeProps) => {
  const theme = useTheme();
  const isRtl = theme.direction === 'rtl';
  const [expanded, setExpanded] = useState<Set<number>>(new Set());
  const [query, setQuery] = useState('');
  const [activeNodeId, setActiveNodeId] = useState<number | null>(selectedId);
  const [draggingNodeId, setDraggingNodeId] = useState<number | null>(null);

  const [menuAnchorEl, setMenuAnchorEl] = useState<HTMLElement | null>(null);
  const [menuNodeId, setMenuNodeId] = useState<number | null>(null);

  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [createParentId, setCreateParentId] = useState<number | null>(null);
  const [createTitle, setCreateTitle] = useState('');
  const [createTitleTouched, setCreateTitleTouched] = useState(false);
  const [createSubmitError, setCreateSubmitError] = useState<string | null>(null);

  const [renameDialogOpen, setRenameDialogOpen] = useState(false);
  const [renameNodeId, setRenameNodeId] = useState<number | null>(null);
  const [renameTitle, setRenameTitle] = useState('');

  const debouncedQuery = useDebouncedValue(query);

  const tree = useMemo(() => buildTree(nodes), [nodes]);
  const nodeMap = useMemo(() => new Map(nodes.map((node) => [node.id, node])), [nodes]);
  const visibleIds = useMemo(() => flattenVisibleIds(tree, expanded), [tree, expanded]);
  const createTitleIsEmpty = createTitle.trim().length === 0;
  const showCreateTitleError = createTitleTouched && createTitleIsEmpty;

  const searchRows = useMemo(() => {
    const needle = debouncedQuery.trim().toLowerCase();
    if (!needle) {
      return [];
    }

    return nodes.filter((node) => node.title.toLowerCase().includes(needle));
  }, [debouncedQuery, nodes]);

  const closeCreateDialog = () => {
    setCreateDialogOpen(false);
    setCreateTitleTouched(false);
    setCreateSubmitError(null);
  };

  useEffect(() => {
    setActiveNodeId(selectedId);

    if (selectedId == null) {
      return;
    }

    const ancestorIds = findAncestorIds(selectedId, nodeMap);
    if (ancestorIds.length === 0) {
      return;
    }

    setExpanded((current) => {
      const next = new Set(current);
      ancestorIds.forEach((ancestorId) => next.add(ancestorId));
      return next;
    });
  }, [selectedId, nodeMap]);

  const closeNodeMenu = () => {
    setMenuAnchorEl(null);
    setMenuNodeId(null);
  };

  const toggleExpanded = (id: number) => {
    setExpanded((current) => {
      const next = new Set(current);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const selectNode = (id: number) => {
    setActiveNodeId(id);
    onSelect(id);
  };

  const clearSelection = () => {
    setActiveNodeId(null);
    onSelect(null);
  };

  const openCreateDialog = (parentId: number | null) => {
    setCreateTitle('');
    setCreateTitleTouched(false);
    setCreateSubmitError(null);
    setCreateParentId(parentId);
    setCreateDialogOpen(true);
  };

  const openToNode = (id: number) => {
    const ancestorIds = findAncestorIds(id, nodeMap);

    if (ancestorIds.length > 0) {
      setExpanded((current) => {
        const next = new Set(current);
        ancestorIds.forEach((ancestorId) => next.add(ancestorId));
        return next;
      });
    }

    selectNode(id);
  };

  const hasChildren = (id: number) => nodes.some((node) => node.parentId === id);
  const hasDescendants = (id: number): boolean => {
    const directChildren = nodes.filter((node) => node.parentId === id);
    if (directChildren.length > 0) {
      return true;
    }

    const current = nodeMap.get(id);
    if (!current) {
      return false;
    }

    const parentPath = current.path.endsWith('/') ? current.path : `${current.path}/`;
    return nodes.some((node) => node.id != id && node.path.startsWith(parentPath));
  };

  const canDelete = (node: ClassificationNode): { allowed: boolean; reason?: string } => {
    if (isSystemNode(node)) {
      return { allowed: false, reason: 'این دسته‌بندی سیستمی قابل حذف نیست.' };
    }

    if (hasDescendants(node.id)) {
      return { allowed: false, reason: 'این پوشه دارای زیرشاخه است.' };
    }

    if (node.caseCount > 0) {
      return { allowed: false, reason: 'این پوشه دارای پرونده است.' };
    }

    return { allowed: true };
  };

  const handleTreeKeyboard = (event: React.KeyboardEvent<HTMLDivElement>) => {
    if (searchRows.length > 0 || visibleIds.length === 0) {
      return;
    }

    const currentId = activeNodeId ?? selectedId ?? visibleIds[0];
    const currentIndex = visibleIds.findIndex((id) => id === currentId);

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      const nextId = visibleIds[Math.min(visibleIds.length - 1, Math.max(0, currentIndex + 1))];
      if (nextId != null) {
        selectNode(nextId);
      }
      return;
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault();
      const previousId = visibleIds[Math.max(0, currentIndex - 1)];
      if (previousId != null) {
        selectNode(previousId);
      }
      return;
    }

    if (currentId == null) {
      return;
    }

    const current = nodeMap.get(currentId);
    if (!current) {
      return;
    }

    const expandKey = isRtl ? 'ArrowLeft' : 'ArrowRight';
    const collapseKey = isRtl ? 'ArrowRight' : 'ArrowLeft';

    if (event.key === expandKey) {
      event.preventDefault();
      if (hasChildren(current.id)) {
        setExpanded((prev) => new Set(prev).add(current.id));
      }
      return;
    }

    if (event.key === collapseKey) {
      event.preventDefault();
      if (expanded.has(current.id)) {
        setExpanded((prev) => {
          const next = new Set(prev);
          next.delete(current.id);
          return next;
        });
        return;
      }

      if (current.parentId != null) {
        selectNode(current.parentId);
      }
      return;
    }

    if (event.key === 'Enter') {
      event.preventDefault();
      selectNode(current.id);
    }
  };

  const renderNodes = (items: TreeNode[], ancestryHasNext: boolean[] = []): JSX.Element[] => {
    return items.map((node, index) => {
      const isLast = index === items.length - 1;
      const isRoot = ancestryHasNext.length === 0;
      const nodeHasChildren = node.children.length > 0;
      const isExpanded = expanded.has(node.id);
      const isSelected = selectedId === node.id;
      const deletable = canDelete(node);
      const isSystem = isSystemNode(node);

      const row = (
        <Box
          key={node.id}
          component="li"
          role="treeitem"
          aria-level={ancestryHasNext.length + 1}
          aria-expanded={nodeHasChildren ? isExpanded : undefined}
          aria-selected={isSelected}
          draggable={hasManagePermission && !isSystem}
          onDragStart={() => setDraggingNodeId(node.id)}
          onDragEnd={() => setDraggingNodeId(null)}
          onDragOver={(event) => {
            event.preventDefault();
            event.dataTransfer.dropEffect = 'move';
          }}
          onDrop={async (event) => {
            event.preventDefault();
            if (!hasManagePermission || isSystem || draggingNodeId == null || draggingNodeId === node.id) {
              return;
            }

            const draggingNode = nodeMap.get(draggingNodeId);
            if (isSystemNode(draggingNode)) {
              return;
            }

            if (isDescendant(node.id, draggingNodeId, nodeMap)) {
              return;
            }

            await onMove(draggingNodeId, node.id);
            setDraggingNodeId(null);
          }}
          sx={{ listStyle: 'none' }}
        >
          <Box
            sx={{
              display: 'flex',
              alignItems: 'stretch',
              minHeight: 36,
              borderRadius: 1,
              backgroundColor: isSelected ? 'primary.main' : 'transparent',
              color: isSelected ? 'primary.contrastText' : 'text.primary',
              transition: 'background-color 140ms ease',
            }}
          >
            <LineGuides ancestryHasNext={ancestryHasNext} isRoot={isRoot} isLast={isLast} />

            <ListItemButton
              dense
              onClick={() => selectNode(node.id)}
              selected={isSelected}
              sx={{
                px: 0.5,
                py: 0.25,
                minHeight: 36,
                borderRadius: 1,
                '&.Mui-selected': {
                  backgroundColor: 'transparent',
                },
                '&:hover': {
                  backgroundColor: isSelected ? 'primary.dark' : 'action.hover',
                },
              }}
            >
              {nodeHasChildren ? (
                <IconButton
                  size="small"
                  edge="start"
                  aria-label={isExpanded ? 'collapse' : 'expand'}
                  onClick={(event) => {
                    event.stopPropagation();
                    toggleExpanded(node.id);
                  }}
                  sx={{ ml: 0.25, mr: 0.5 }}
                >
                  <ChevronRightIcon
                    fontSize="small"
                    sx={{
                      transform: isExpanded
                        ? 'rotate(90deg)'
                        : isRtl
                          ? 'rotate(180deg)'
                          : 'rotate(0deg)',
                      transition: 'transform 180ms ease',
                    }}
                  />
                </IconButton>
              ) : (
                <Box sx={{ width: 30, flexShrink: 0 }} />
              )}

              {isSystem ? (
                <LockOutlinedIcon fontSize="small" sx={{ mr: 1 }} />
              ) : nodeHasChildren && isExpanded ? (
                <FolderOpenOutlinedIcon fontSize="small" sx={{ mr: 1 }} />
              ) : (
                <FolderOutlinedIcon fontSize="small" sx={{ mr: 1 }} />
              )}

              <Typography
                variant="body2"
                noWrap
                sx={{
                  fontWeight: isSelected ? 700 : 500,
                  minWidth: 0,
                  flexGrow: 1,
                }}
              >
                {node.title}
              </Typography>

              <Chip
                size="small"
                label={node.caseCount}
                sx={{
                  mx: 0.75,
                  height: 20,
                  minWidth: 44,
                  flexShrink: 0,
                  color: isSelected ? 'primary.contrastText' : 'text.secondary',
                  borderColor: isSelected ? 'primary.contrastText' : 'divider',
                  backgroundColor: 'transparent',
                  '& .MuiChip-label': { px: 1, fontSize: 11, fontVariantNumeric: 'tabular-nums' },
                }}
                variant="outlined"
              />

              {isSystem ? (
                <Chip
                  size="small"
                  label="سیستمی"
                  sx={{
                    ml: 0.5,
                    height: 20,
                    color: isSelected ? 'primary.contrastText' : 'text.secondary',
                    borderColor: isSelected ? 'primary.contrastText' : 'divider',
                    backgroundColor: 'transparent',
                    '& .MuiChip-label': { px: 0.8, fontSize: 11 },
                  }}
                  variant="outlined"
                />
              ) : null}

              {(hasCreatePermission || hasEditPermission) && !isSystem ? (
                <IconButton
                  size="small"
                  aria-label="node-actions"
                  onClick={(event) => {
                    event.stopPropagation();
                    setMenuNodeId(node.id);
                    setMenuAnchorEl(event.currentTarget);
                  }}
                >
                  <MoreHorizIcon fontSize="small" />
                </IconButton>
              ) : null}

              {hasManagePermission ? (
                <IconButton
                  size="small"
                  aria-label="delete-node"
                  title={deletable.reason ?? 'حذف پوشه'}
                  disabled={!deletable.allowed}
                  onClick={async (event) => {
                    event.stopPropagation();
                    if (deletable.allowed) {
                      await onDelete(node.id);
                    }
                  }}
                >
                  <DeleteOutlineIcon fontSize="small" />
                </IconButton>
              ) : null}
            </ListItemButton>
          </Box>

          {nodeHasChildren ? (
            <Collapse in={isExpanded} timeout={180} unmountOnExit>
              <Box component="ul" role="group" sx={{ m: 0, p: 0 }}>
                {renderNodes(node.children, [...ancestryHasNext, !isLast])}
              </Box>
            </Collapse>
          ) : null}
        </Box>
      );

      return row;
    });
  };

  return (
    <Stack spacing={1} sx={{ flex: 1, minHeight: 0, overflow: 'hidden' }}>
      <Box
        sx={{
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          gap: 1,
        }}
      >
        <TextField
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder="جستجوی طبقه‌بندی"
          size="small"
          sx={{
            flex: '1 1 190px',
            minWidth: 0,
          }}
          InputProps={{
            startAdornment: <SearchIcon fontSize="small" sx={{ color: 'text.secondary', mr: 1 }} />,
          }}
          inputProps={{ 'aria-label': 'جستجوی طبقه‌بندی' }}
        />

        {hasCreatePermission ? (
          <Button
            variant="outlined"
            size="small"
            startIcon={<AddIcon />}
            sx={{ flex: '0 0 auto', whiteSpace: 'nowrap' }}
            onClick={() => openCreateDialog(selectedId)}
            disabled={selectedId == null}
          >
            زیرپوشه جدید
          </Button>
        ) : null}

        {hasCreatePermission ? (
          <Button
            variant={selectedId == null ? 'contained' : 'outlined'}
            size="small"
            startIcon={<AddIcon />}
            sx={{ flex: '0 0 auto', whiteSpace: 'nowrap' }}
            onClick={() => openCreateDialog(null)}
          >
            پوشه جدید
          </Button>
        ) : null}

        {selectedId != null ? (
          <Button
            variant="text"
            size="small"
            sx={{ flex: '0 0 auto', whiteSpace: 'nowrap' }}
            onClick={clearSelection}
          >
            لغو انتخاب
          </Button>
        ) : null}
      </Box>

      <Box
        role="tree"
        tabIndex={0}
        aria-label="classification-tree"
        onKeyDown={handleTreeKeyboard}
        sx={{
          flex: 1,
          minHeight: 0,
          overflow: 'auto',
          borderRadius: 1.5,
          border: '1px solid',
          borderColor: 'divider',
          backgroundColor: 'background.paper',
          p: 1,
        }}
      >
        {loading ? (
          <Stack alignItems="center" justifyContent="center" sx={{ height: 180 }} spacing={1}>
            <CircularProgress size={22} />
            <Typography variant="caption" color="text.secondary">
              در حال بارگذاری طبقه‌بندی...
            </Typography>
          </Stack>
        ) : searchRows.length > 0 ? (
          <Stack spacing={0.5}>
            {searchRows.map((node) => (
              <ListItemButton
                key={node.id}
                selected={selectedId === node.id}
                onClick={() => openToNode(node.id)}
                sx={{ borderRadius: 1 }}
              >
                <FolderOutlinedIcon fontSize="small" sx={{ mr: 1 }} />
                <Box sx={{ minWidth: 0, flexGrow: 1 }}>
                  <Typography variant="body2" noWrap>
                    {node.title}
                  </Typography>
                  <Typography variant="caption" color="text.secondary" noWrap>
                    {node.path}
                  </Typography>
                </Box>
              </ListItemButton>
            ))}
          </Stack>
        ) : nodes.length === 0 ? (
          <Typography variant="body2" color="text.secondary" sx={{ p: 1 }}>
            طبقه‌بندی‌ای ثبت نشده است.
          </Typography>
        ) : (
          <Box component="ul" sx={{ m: 0, p: 0 }}>
            {renderNodes(tree)}
          </Box>
        )}
      </Box>

      <Menu anchorEl={menuAnchorEl} open={Boolean(menuAnchorEl)} onClose={closeNodeMenu}>
        {hasCreatePermission ? (
          <MenuItem
            onClick={() => {
              openCreateDialog(menuNodeId);
              closeNodeMenu();
            }}
          >
            <AddIcon fontSize="small" sx={{ mr: 1 }} />
            ایجاد زیرپوشه
          </MenuItem>
        ) : null}

        {hasEditPermission ? (
          <MenuItem
          onClick={() => {
            if (menuNodeId == null) {
              closeNodeMenu();
              return;
            }

            const target = nodeMap.get(menuNodeId);
            if (!target) {
              closeNodeMenu();
              return;
            }

            setRenameNodeId(menuNodeId);
            setRenameTitle(target.title);
            setRenameDialogOpen(true);
            closeNodeMenu();
          }}
        >
          <EditOutlinedIcon fontSize="small" sx={{ mr: 1 }} />
          تغییر نام
          </MenuItem>
        ) : null}
      </Menu>

      {hasCreatePermission ? <Dialog open={createDialogOpen} onClose={closeCreateDialog} fullWidth maxWidth="xs">
        <DialogTitle>{createParentId == null ? 'ایجاد پوشه اصلی' : 'ایجاد زیرپوشه'}</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ pt: 1 }}>
            {createParentId == null
              ? 'پوشه جدید در سطح ریشه ایجاد می‌شود.'
              : `پوشه جدید زیرمجموعه «${nodeMap.get(createParentId)?.title ?? ''}» ایجاد می‌شود.`}
          </Typography>
          {createSubmitError ? (
            <Alert severity="error" sx={{ mt: 1 }}>
              {createSubmitError}
            </Alert>
          ) : null}
          <TextField
            autoFocus
            required
            margin="dense"
            label="عنوان"
            value={createTitle}
            onBlur={() => setCreateTitleTouched(true)}
            onChange={(event) => {
              setCreateTitle(event.target.value);
              setCreateSubmitError(null);
            }}
            error={showCreateTitleError || Boolean(createSubmitError)}
            helperText={showCreateTitleError ? 'عنوان پوشه اجباری است.' : createSubmitError ?? ' '}
            fullWidth
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={closeCreateDialog}>انصراف</Button>
          <Button
            variant="contained"
            disabled={createTitleIsEmpty}
            onClick={async () => {
              const title = createTitle.trim();
              if (!title) {
                setCreateTitleTouched(true);
                return;
              }

              try {
                setCreateSubmitError(null);
                await onCreate(title, createParentId);
                closeCreateDialog();
              } catch (error) {
                setCreateSubmitError(mapApiError(error).message);
              }
            }}
          >
            ایجاد
          </Button>
        </DialogActions>
      </Dialog> : null}

      {hasEditPermission ? <Dialog open={renameDialogOpen} onClose={() => setRenameDialogOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>تغییر نام پوشه</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            margin="dense"
            label="عنوان جدید"
            value={renameTitle}
            onChange={(event) => setRenameTitle(event.target.value)}
            fullWidth
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRenameDialogOpen(false)}>انصراف</Button>
          <Button
            variant="contained"
            onClick={async () => {
              const title = renameTitle.trim();
              if (!title || renameNodeId == null) {
                return;
              }

              await onRename(renameNodeId, title);
              setRenameDialogOpen(false);
            }}
          >
            ذخیره
          </Button>
        </DialogActions>
      </Dialog> : null}
    </Stack>
  );
};
