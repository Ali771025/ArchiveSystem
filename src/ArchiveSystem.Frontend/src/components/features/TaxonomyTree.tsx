import AddIcon from '@mui/icons-material/Add';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import DescriptionOutlinedIcon from '@mui/icons-material/DescriptionOutlined';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import FolderOutlinedIcon from '@mui/icons-material/FolderOutlined';
import KeyboardArrowDownIcon from '@mui/icons-material/KeyboardArrowDown';
import KeyboardArrowRightIcon from '@mui/icons-material/KeyboardArrowRight';
import {
  Badge,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  IconButton,
  InputAdornment,
  Stack,
  Switch,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import { useQuery } from '@tanstack/react-query';
import { useEffect, useMemo, useState } from 'react';
import { toast } from 'sonner';
import { api } from '@/services/api';
import { buildDeleteConfirmOptions } from '@/components/common/confirmPresets';
import { useConfirm } from '@/components/common/useConfirm';
import {
  useClassificationTree,
  useCreateClassification,
  useDeleteClassification,
  useMoveClassification,
  useRenameClassification,
} from '@/services/archiveQueries';
import type { ClassificationNode } from '@/types/archive';
import { useDebouncedValue } from '@/utils/useDebouncedValue';

type LegacyTreeNode = {
  id: string;
  name: string;
  children?: LegacyTreeNode[];
};

interface TaxonomyTreeProps {
  nodes?: LegacyTreeNode[];
}

interface TaxonomyTreeNode extends ClassificationNode {
  children: TaxonomyTreeNode[];
}

interface SearchNodeResult {
  id: number;
  title: string;
  parentId: number | null;
  hierarchyPath: string;
  dossierCount: number;
}

type ModalMode = 'create' | 'rename';

const buildTree = (nodes: ClassificationNode[]): TaxonomyTreeNode[] => {
  const map = new Map<number, TaxonomyTreeNode>();
  nodes.forEach((node) => {
    map.set(node.id, { ...node, children: [] });
  });

  const roots: TaxonomyTreeNode[] = [];
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

  const sortNodes = (items: TaxonomyTreeNode[]): TaxonomyTreeNode[] => {
    items.sort((a, b) => a.title.localeCompare(b.title, 'fa'));
    items.forEach((item) => sortNodes(item.children));
    return items;
  };

  return sortNodes(roots);
};

const flattenLegacyNodes = (nodes: LegacyTreeNode[]): ClassificationNode[] => {
  const flat: ClassificationNode[] = [];

  const walk = (items: LegacyTreeNode[], parentId: number | null, level: number) => {
    items.forEach((item) => {
      const id = Number(item.id);
      if (!Number.isFinite(id)) {
        return;
      }

      flat.push({
        id,
        title: item.name,
        parentId,
        level,
        path: parentId == null ? `/${id}/` : `/${parentId}/${id}/`,
        caseCount: 0,
        isActive: true,
      });

      if (item.children && item.children.length > 0) {
        walk(item.children, id, level + 1);
      }
    });
  };

  walk(nodes, null, 0);
  return flat;
};

const collectAncestorIds = (nodeId: number, nodeMap: Map<number, ClassificationNode>): number[] => {
  const ancestors: number[] = [];
  let pointer = nodeMap.get(nodeId);

  while (pointer?.parentId != null) {
    ancestors.unshift(pointer.parentId);
    pointer = nodeMap.get(pointer.parentId);
  }

  return ancestors;
};

const isDescendant = (candidateParentId: number | null, movingId: number, nodeMap: Map<number, ClassificationNode>): boolean => {
  if (candidateParentId == null) {
    return false;
  }

  let pointer = nodeMap.get(candidateParentId);
  while (pointer?.parentId != null) {
    if (pointer.parentId === movingId) {
      return true;
    }
    pointer = nodeMap.get(pointer.parentId);
  }

  return candidateParentId === movingId;
};

const highlightLabel = (label: string, query: string) => {
  if (!query.trim()) {
    return label;
  }

  const escaped = query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const regex = new RegExp(`(${escaped})`, 'ig');
  const segments = label.split(regex);

  return segments.map((segment, index) => {
    if (segment.toLowerCase() === query.toLowerCase()) {
      return (
        <Box
          key={`${segment}-${index}`}
          component="span"
          sx={{
            backgroundColor: 'warning.light',
            color: 'warning.contrastText',
            borderRadius: 0.5,
            px: 0.25,
          }}
        >
          {segment}
        </Box>
      );
    }

    return <span key={`${segment}-${index}`}>{segment}</span>;
  });
};

const readApiError = (error: unknown, fallback: string): string => {
  if (typeof error === 'object' && error && 'response' in error) {
    const response = (error as { response?: { data?: { detail?: string; title?: string } } }).response;
    return response?.data?.detail ?? response?.data?.title ?? fallback;
  }

  return fallback;
};

const TaxonomyTree = ({ nodes = [] }: TaxonomyTreeProps) => {
  const [searchQuery, setSearchQuery] = useState('');
  const debouncedSearch = useDebouncedValue(searchQuery, 250);
  const [expandedIds, setExpandedIds] = useState<Set<number>>(new Set());
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [draggingId, setDraggingId] = useState<number | null>(null);

  const [modalOpen, setModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState<ModalMode>('create');
  const [modalTitle, setModalTitle] = useState('');
  const [modalTitleTouched, setModalTitleTouched] = useState(false);
  const [modalNodeId, setModalNodeId] = useState<number | null>(null);
  const [modalParentId, setModalParentId] = useState<number | null>(null);
  const [createAsRoot, setCreateAsRoot] = useState(false);
  const modalTitleIsEmpty = modalTitle.trim().length === 0;
  const showModalTitleError = modalTitleTouched && modalTitleIsEmpty;

  const classificationTreeQuery = useClassificationTree();
  const createMutation = useCreateClassification();
  const renameMutation = useRenameClassification();
  const moveMutation = useMoveClassification();
  const deleteMutation = useDeleteClassification();
  const confirm = useConfirm();

  const searchQueryResult = useQuery({
    queryKey: ['classification-search', debouncedSearch],
    enabled: debouncedSearch.trim().length >= 2,
    queryFn: async () => {
      const response = await api.get<SearchNodeResult[]>('/classifications/search', {
        params: { q: debouncedSearch.trim() },
      });
      return response.data;
    },
  });

  const fallbackNodes = useMemo(() => flattenLegacyNodes(nodes), [nodes]);
  const sourceNodes = classificationTreeQuery.data && classificationTreeQuery.data.length > 0
    ? classificationTreeQuery.data
    : fallbackNodes;

  const tree = useMemo(() => buildTree(sourceNodes), [sourceNodes]);
  const nodeMap = useMemo(() => new Map(sourceNodes.map((node) => [node.id, node])), [sourceNodes]);

  const matchIds = useMemo(() => {
    const fromApi = searchQueryResult.data?.map((item) => item.id) ?? [];
    if (fromApi.length > 0) {
      return new Set(fromApi);
    }

    if (!debouncedSearch.trim()) {
      return new Set<number>();
    }

    const needle = debouncedSearch.trim().toLowerCase();
    return new Set(
      sourceNodes
        .filter((node) => node.title.toLowerCase().includes(needle))
        .map((node) => node.id),
    );
  }, [debouncedSearch, searchQueryResult.data, sourceNodes]);

  useEffect(() => {
    if (matchIds.size === 0) {
      return;
    }

    setExpandedIds((current) => {
      const next = new Set(current);
      matchIds.forEach((matchId) => {
        collectAncestorIds(matchId, nodeMap).forEach((ancestorId) => next.add(ancestorId));
      });
      return next;
    });
  }, [matchIds, nodeMap]);

  const toggleExpanded = (id: number) => {
    setExpandedIds((current) => {
      const next = new Set(current);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const openCreateModal = (parentId: number | null) => {
    setModalMode('create');
    setModalTitle('');
    setModalTitleTouched(false);
    setModalNodeId(null);
    setModalParentId(parentId);
    setCreateAsRoot(parentId == null);
    setModalOpen(true);
  };

  const openRenameModal = (nodeId: number, title: string) => {
    setModalMode('rename');
    setModalTitle(title);
    setModalTitleTouched(false);
    setModalNodeId(nodeId);
    setModalParentId(null);
    setModalOpen(true);
  };

  const saveModal = async () => {
    const title = modalTitle.trim();
    if (!title) {
      setModalTitleTouched(true);
      return;
    }

    try {
      if (modalMode === 'create') {
        await createMutation.mutateAsync({ title, parentId: createAsRoot ? null : modalParentId });
      } else if (modalNodeId != null) {
        await renameMutation.mutateAsync({ id: modalNodeId, title });
      }

      setModalOpen(false);
      setModalTitleTouched(false);
    } catch (error) {
      toast.error(readApiError(error, 'Taxonomy operation failed.'));
    }
  };

  const deleteNode = async (node: TaxonomyTreeNode) => {
    const approved = await confirm(buildDeleteConfirmOptions(`پوشه «${node.title}»`));
    if (!approved) {
      return;
    }

    try {
      await deleteMutation.mutateAsync(node.id);
      toast.success('پوشه با موفقیت حذف شد.');
    } catch (error) {
      toast.error(readApiError(error, 'حذف پوشه ناموفق بود.'));
    }
  };

  const moveNode = async (nodeId: number, newParentId: number | null) => {
    if (nodeId === newParentId) {
      toast.error('A node cannot be moved under itself.');
      return;
    }

    if (isDescendant(newParentId, nodeId, nodeMap)) {
      toast.error('Invalid move: a node cannot become a child of its own descendant.');
      return;
    }

    try {
      await moveMutation.mutateAsync({ id: nodeId, parentId: newParentId });
    } catch (error) {
      toast.error(readApiError(error, 'Move failed.'));
    }
  };

  const renderNode = (node: TaxonomyTreeNode, depth = 0): JSX.Element => {
    const hasChildren = node.children.length > 0;
    const isExpanded = expandedIds.has(node.id);
    const isMatch = matchIds.has(node.id);

    return (
      <Box key={node.id} sx={{ userSelect: 'none' }}>
        <Box
          className="group"
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 0.5,
            px: 1,
            py: 0.5,
            ml: depth * 1.75,
            borderRadius: 1,
            backgroundColor: selectedId === node.id ? 'action.selected' : 'transparent',
            border: '1px solid transparent',
            ...(isMatch
              ? {
                  borderColor: 'warning.main',
                  backgroundColor: 'warning.50',
                }
              : null),
            '&:hover': { backgroundColor: 'action.hover' },
          }}
          draggable
          onDragStart={() => setDraggingId(node.id)}
          onDragEnd={() => setDraggingId(null)}
          onDragOver={(event) => {
            event.preventDefault();
            event.dataTransfer.dropEffect = 'move';
          }}
          onDrop={async (event) => {
            event.preventDefault();
            if (draggingId == null || draggingId === node.id) {
              return;
            }

            await moveNode(draggingId, node.id);
            setDraggingId(null);
          }}
          onClick={() => {
            setSelectedId(node.id);
            if (hasChildren) {
              toggleExpanded(node.id);
            }
          }}
        >
          <IconButton
            size="small"
            onClick={(event) => {
              event.stopPropagation();
              if (hasChildren) {
                toggleExpanded(node.id);
              }
            }}
            sx={{ width: 20, height: 20 }}
          >
            {hasChildren ? (
              isExpanded ? <KeyboardArrowDownIcon fontSize="small" /> : <KeyboardArrowRightIcon fontSize="small" />
            ) : null}
          </IconButton>

          {hasChildren ? <FolderOutlinedIcon fontSize="small" color="primary" /> : <DescriptionOutlinedIcon fontSize="small" />}

          <Typography variant="body2" sx={{ flex: 1, minWidth: 0 }} noWrap>
            {highlightLabel(node.title, debouncedSearch.trim())}
          </Typography>

          <Badge
            color="secondary"
            badgeContent={node.caseCount}
            sx={{ '& .MuiBadge-badge': { fontSize: 11, minWidth: 18, height: 18 } }}
          >
            <Box sx={{ width: 14 }} />
          </Badge>

          <Stack direction="row" spacing={0.5} className="opacity-0 transition-opacity group-hover:opacity-100">
            <Tooltip title="Add child">
              <IconButton
                size="small"
                disabled={createMutation.isPending}
                onClick={(event) => {
                  event.stopPropagation();
                  openCreateModal(node.id);
                }}
              >
                <AddIcon fontSize="small" />
              </IconButton>
            </Tooltip>

            <Tooltip title="Rename">
              <IconButton
                size="small"
                disabled={renameMutation.isPending}
                onClick={(event) => {
                  event.stopPropagation();
                  openRenameModal(node.id, node.title);
                }}
              >
                <EditOutlinedIcon fontSize="small" />
              </IconButton>
            </Tooltip>

            <Tooltip title="Delete">
              <IconButton
                size="small"
                color="error"
                disabled={deleteMutation.isPending}
                onClick={(event) => {
                  event.stopPropagation();
                  void deleteNode(node);
                }}
              >
                {deleteMutation.isPending ? <CircularProgress size={16} /> : <DeleteOutlineIcon fontSize="small" />}
              </IconButton>
            </Tooltip>
          </Stack>
        </Box>

        {hasChildren && isExpanded ? node.children.map((child) => renderNode(child, depth + 1)) : null}
      </Box>
    );
  };

  const loading = classificationTreeQuery.isLoading && sourceNodes.length === 0;

  return (
    <Stack spacing={1} sx={{ height: '100%' }}>
      <TextField
        size="small"
        value={searchQuery}
        onChange={(event) => setSearchQuery(event.target.value)}
        placeholder="Search taxonomy"
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <SearchIcon fontSize="small" />
            </InputAdornment>
          ),
        }}
      />

      <Button
        variant="outlined"
        size="small"
        startIcon={createMutation.isPending ? <CircularProgress size={16} /> : <AddIcon />}
        onClick={() => openCreateModal(null)}
        disabled={createMutation.isPending}
      >
        Add Root Category
      </Button>

      <Box
        sx={{
          flex: 1,
          overflow: 'auto',
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 1,
          p: 1,
        }}
        onDragOver={(event) => {
          event.preventDefault();
          event.dataTransfer.dropEffect = 'move';
        }}
        onDrop={async (event) => {
          event.preventDefault();
          if (draggingId == null) {
            return;
          }

          await moveNode(draggingId, null);
          setDraggingId(null);
        }}
      >
        {loading ? (
          <Typography variant="body2" color="text.secondary">
            Loading taxonomy...
          </Typography>
        ) : tree.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            No categories found.
          </Typography>
        ) : (
          tree.map((node) => renderNode(node))
        )}
      </Box>

      <Dialog open={modalOpen} onClose={() => {
        setModalOpen(false);
        setModalTitleTouched(false);
      }} maxWidth="xs" fullWidth>
        <DialogTitle>{modalMode === 'create' ? 'Create Category' : 'Rename Category'}</DialogTitle>
        <DialogContent>
          {modalMode === 'create' ? (
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} alignItems={{ sm: 'center' }} sx={{ mt: 1 }}>
              <FormControlLabel
                control={
                  <Switch
                    checked={createAsRoot}
                    onChange={(event) => {
                      const checked = event.target.checked;
                      setCreateAsRoot(checked);
                      if (checked) {
                        setModalParentId(null);
                      }
                    }}
                  />
                }
                label="Create as Root Category"
              />
              <Button
                size="small"
                variant="text"
                onClick={() => {
                  setCreateAsRoot(true);
                  setModalParentId(null);
                }}
              >
                Clear Selection
              </Button>
            </Stack>
          ) : null}
          <TextField
            autoFocus
            required
            fullWidth
            margin="dense"
            label="Title"
            value={modalTitle}
            onBlur={() => setModalTitleTouched(true)}
            onChange={(event) => setModalTitle(event.target.value)}
            error={showModalTitleError}
            helperText={showModalTitleError ? 'Title is required.' : ' '}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => {
            setModalOpen(false);
            setModalTitleTouched(false);
          }} disabled={createMutation.isPending || renameMutation.isPending}>
            Cancel
          </Button>
          <Button
            variant="contained"
            onClick={() => void saveModal()}
            disabled={modalTitleIsEmpty || createMutation.isPending || renameMutation.isPending}
            startIcon={createMutation.isPending || renameMutation.isPending ? <CircularProgress size={16} /> : null}
          >
            {createMutation.isPending || renameMutation.isPending ? 'Saving...' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
};

export default TaxonomyTree;
