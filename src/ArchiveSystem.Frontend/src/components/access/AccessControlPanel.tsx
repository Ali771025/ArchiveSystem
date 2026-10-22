import SecurityIcon from '@mui/icons-material/Security';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  FormControlLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Switch,
  Tab,
  Tabs,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
import { useEffect, useMemo, useState } from 'react';
import {
  useClassificationAcl,
  useClassificationTree,
  useRoles,
  useSaveClassificationAcl,
  useUsers,
} from '@/services/archiveQueries';
import { useSessionStore } from '@/stores/useSessionStore';
import { useToastStore } from '@/stores/useToastStore';
import { RolePermissionsManager } from '@/components/access/RolePermissionsManager';
import { UserRoleAssigner } from '@/components/access/UserRoleAssigner';
import { CategoryRadioTreeView, type Category } from '@/components/dossiers/CategoryRadioTreeView';
import type { ClassificationAclEntryDto, ClassificationNode } from '@/types/archive';
import { compareClassificationsArchiveLast } from '@/utils/classificationOrder';
import {
  CLASSIFICATION_MOVE_MASK as MOVE_MASK,
  CLASSIFICATION_READ_MASK as READ_MASK,
  CLASSIFICATION_WRITE_MASK as WRITE_MASK,
  normalizeEditableClassificationMask,
} from '@/components/access/classificationAclMask';

type SubjectType = 1 | 2;
type CategoryAclNode = ClassificationNode & { children: CategoryAclNode[] };

const normalizeMask = normalizeEditableClassificationMask;

const buildCategoryTree = (nodes: ClassificationNode[]): CategoryAclNode[] => {
  const byId = new Map<number, CategoryAclNode>();
  const roots: CategoryAclNode[] = [];

  nodes.forEach((node) => byId.set(node.id, { ...node, children: [] }));
  byId.forEach((node) => {
    if (node.parentId && byId.has(node.parentId)) {
      byId.get(node.parentId)?.children.push(node);
      return;
    }

    roots.push(node);
  });

  const sortTree = (items: CategoryAclNode[]) => {
    items.sort(compareClassificationsArchiveLast);
    items.forEach((item) => sortTree(item.children));
  };

  sortTree(roots);
  return roots;
};

const mapToRadioCategories = (nodes: CategoryAclNode[]): Category[] =>
  nodes.map((node) => ({
    id: String(node.id),
    name: node.title,
    systemKey: node.systemKey,
    children: mapToRadioCategories(node.children),
  }));

const findCategoryNodeById = (nodes: CategoryAclNode[], id: number): CategoryAclNode | null => {
  for (const node of nodes) {
    if (node.id === id) {
      return node;
    }

    const nested = findCategoryNodeById(node.children, id);
    if (nested) {
      return nested;
    }
  }

  return null;
};

const rebaseLevels = (node: CategoryAclNode, baseLevel: number): CategoryAclNode => ({
  ...node,
  level: Math.max(0, node.level - baseLevel),
  children: node.children.map((child) => rebaseLevels(child, baseLevel)),
});

const collectCategoryIds = (node: CategoryAclNode): number[] => {
  return [node.id, ...node.children.flatMap((child) => collectCategoryIds(child))];
};

interface CategoryAclTreeViewProps {
  nodes: CategoryAclNode[];
  entries: ClassificationAclEntryDto[];
  subjectType: SubjectType;
  subjectId: number | null;
  cascadeByClassificationId: Record<number, boolean>;
  onCascadeChange: (classificationId: number, cascade: boolean) => void;
  onPermissionChange: (classificationId: number, mask: number) => void;
}

function CategoryAclTreeView({
  nodes,
  entries,
  subjectType,
  subjectId,
  cascadeByClassificationId,
  onCascadeChange,
  onPermissionChange,
}: CategoryAclTreeViewProps) {
  return (
    <Stack spacing={0.75}>
      {nodes.map((node) => {
        const current = entries.find(
          (entry) =>
            entry.classificationId === node.id &&
            entry.subjectType === subjectType &&
            entry.subjectId === subjectId,
        );
        const mask = normalizeMask(current?.permissionMask ?? 0);
        const canEdit = (mask & WRITE_MASK) === WRITE_MASK;
        const canMove = (mask & MOVE_MASK) === MOVE_MASK;
        const canView = canEdit || canMove || (mask & READ_MASK) === READ_MASK;

        return (
          <Stack key={node.id} spacing={0.75}>
            <Paper
              variant="outlined"
              sx={{
                p: 1,
                borderRadius: 1,
                bgcolor: 'background.paper',
                marginInlineStart: `${node.level * 16}px`,
              }}
            >
              <Stack
                direction={{ xs: 'column', md: 'row' }}
                spacing={1}
                alignItems={{ xs: 'stretch', md: 'center' }}
                justifyContent="space-between"
              >
                <Typography fontWeight={700}>{node.title}</Typography>

                <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                  <FormControlLabel
                    label="مشاهده"
                    control={
                      <Checkbox
                        checked={canView}
                        disabled={!subjectId || canEdit || canMove}
                        onChange={(event) =>
                          onPermissionChange(
                            node.id,
                            event.target.checked ? mask | READ_MASK : mask & ~READ_MASK,
                          )
                        }
                      />
                    }
                  />
                  <FormControlLabel
                    label="ویرایش"
                    control={
                      <Checkbox
                        checked={canEdit}
                        disabled={!subjectId}
                        onChange={(event) =>
                          onPermissionChange(
                            node.id,
                            event.target.checked ? mask | WRITE_MASK | READ_MASK : mask & ~WRITE_MASK,
                          )
                        }
                      />
                    }
                  />
                  <FormControlLabel
                    label="انتقال"
                    control={
                      <Checkbox
                        checked={canMove}
                        disabled={!subjectId}
                        onChange={(event) =>
                          onPermissionChange(
                            node.id,
                            event.target.checked ? mask | MOVE_MASK | READ_MASK : mask & ~MOVE_MASK,
                          )
                        }
                      />
                    }
                  />
                  <FormControlLabel
                    label="اعمال روی زیرشاخه‌ها"
                    control={
                      <Switch
                        checked={Boolean(cascadeByClassificationId[node.id])}
                        onChange={(event) => onCascadeChange(node.id, event.target.checked)}
                      />
                    }
                  />
                </Stack>
              </Stack>
            </Paper>

            {node.children.length > 0 && (
              <CategoryAclTreeView
                nodes={node.children}
                entries={entries}
                subjectType={subjectType}
                subjectId={subjectId}
                cascadeByClassificationId={cascadeByClassificationId}
                onCascadeChange={onCascadeChange}
                onPermissionChange={onPermissionChange}
              />
            )}
          </Stack>
        );
      })}
    </Stack>
  );
}

export const AccessControlPanel = () => {
  const theme = useTheme();
  const isCompact = useMediaQuery(theme.breakpoints.down('md'));
  const canManage = useSessionStore((state) => state.hasPermission('roles.manage'));
  const pushToast = useToastStore((state) => state.push);

  const [activeTab, setActiveTab] = useState(0);
  const [selectedSubjectType, setSelectedSubjectType] = useState<SubjectType>(2);
  const [selectedSubjectId, setSelectedSubjectId] = useState<number | null>(null);
  const [selectedClassification, setSelectedClassification] = useState<number | null>(null);
  const [cascadeByClassificationId, setCascadeByClassificationId] = useState<Record<number, boolean>>({});

  const rolesQuery = useRoles();
  const usersQuery = useUsers();
  const classificationQuery = useClassificationTree();
  const roles = rolesQuery.data ?? [];
  const users = usersQuery.data ?? [];
  const classifications = classificationQuery.data ?? [];
  const categoryTree = useMemo(() => buildCategoryTree(classifications), [classifications]);
  const categorySelectorTree = useMemo(() => mapToRadioCategories(categoryTree), [categoryTree]);
  const selectedCategoryNode = useMemo(
    () => (selectedClassification == null ? null : findCategoryNodeById(categoryTree, selectedClassification)),
    [categoryTree, selectedClassification],
  );
  const focusedAclTree = useMemo(() => {
    if (selectedCategoryNode == null) {
      return categoryTree;
    }

    return [rebaseLevels(selectedCategoryNode, selectedCategoryNode.level)];
  }, [categoryTree, selectedCategoryNode]);
  const selectedSubtreeIds = useMemo(() => {
    if (selectedCategoryNode) {
      return new Set(collectCategoryIds(selectedCategoryNode));
    }

    if (selectedClassification != null) {
      return new Set([selectedClassification]);
    }

    return new Set<number>();
  }, [selectedCategoryNode, selectedClassification]);
  const saveClassificationAclMutation = useSaveClassificationAcl();
  const classificationAclQuery = useClassificationAcl(selectedClassification, true);
  const [classificationDraft, setClassificationDraft] = useState<ClassificationAclEntryDto[]>([]);

  useEffect(() => {
    if (classifications.length > 0 && selectedClassification == null) {
      setSelectedClassification(classifications[0].id);
    }
  }, [classifications, selectedClassification]);

  useEffect(() => {
    if (selectedSubjectType === 1) {
      if (users.length === 0) {
        setSelectedSubjectId(null);
        return;
      }

      if (selectedSubjectId == null || !users.some((user) => user.id === selectedSubjectId)) {
        setSelectedSubjectId(users[0].id);
      }

      return;
    }

    if (roles.length === 0) {
      setSelectedSubjectId(null);
      return;
    }

    if (selectedSubjectId == null || !roles.some((role) => role.id === selectedSubjectId)) {
      setSelectedSubjectId(roles[0].id);
    }
  }, [roles, selectedSubjectId, selectedSubjectType, users]);

  useEffect(() => {
    setClassificationDraft(classificationAclQuery.data ?? []);
    setCascadeByClassificationId({});
  }, [classificationAclQuery.data]);

  if (!canManage) {
    return <Alert severity="warning">شما مجوز مدیریت نقش‌ها و کنترل دسترسی را ندارید.</Alert>;
  }

  const upsertClassificationEntry = (classificationId: number, permissionMask: number) => {
    if (!selectedSubjectId) {
      return;
    }

    setClassificationDraft((current) => {
      const classificationNode = findCategoryNodeById(categoryTree, classificationId);
      const targetIds =
        cascadeByClassificationId[classificationId] && classificationNode
          ? collectCategoryIds(classificationNode)
          : [classificationId];
      const targetIdSet = new Set(targetIds);
      const base = current.filter(
        (entry) =>
          !(targetIdSet.has(entry.classificationId) &&
            entry.subjectType === selectedSubjectType &&
            entry.subjectId === selectedSubjectId),
      );

      return [
        ...base,
        ...targetIds.map((targetClassificationId) => ({
          id: 0,
          classificationId: targetClassificationId,
          subjectType: selectedSubjectType,
          subjectId: selectedSubjectId,
          permissionMask: normalizeMask(permissionMask),
          isExplicit: true,
          inheritMode: 1,
          cascade:
            targetClassificationId === classificationId &&
            Boolean(cascadeByClassificationId[classificationId]),
        })),
      ];
    });
  };

  const updateCascade = (classificationId: number, cascade: boolean) => {
    setCascadeByClassificationId((current) => ({ ...current, [classificationId]: cascade }));
    setClassificationDraft((current) => {
      const sourceEntry = current.find(
        (entry) =>
          entry.classificationId === classificationId &&
          entry.subjectType === selectedSubjectType &&
          entry.subjectId === selectedSubjectId,
      );
      const classificationNode = findCategoryNodeById(categoryTree, classificationId);

      if (!sourceEntry || !cascade || !classificationNode) {
        return current.map((entry) =>
          entry.classificationId === classificationId &&
          entry.subjectType === selectedSubjectType &&
          entry.subjectId === selectedSubjectId
            ? { ...entry, cascade }
            : entry,
        );
      }

      const targetIds = collectCategoryIds(classificationNode);
      const targetIdSet = new Set(targetIds);
      const base = current.filter(
        (entry) =>
          !(targetIdSet.has(entry.classificationId) &&
            entry.subjectType === selectedSubjectType &&
            entry.subjectId === selectedSubjectId),
      );

      return [
        ...base,
        ...targetIds.map((targetClassificationId) => ({
          ...sourceEntry,
          id: targetClassificationId === classificationId ? sourceEntry.id : 0,
          classificationId: targetClassificationId,
          isExplicit: true,
          cascade: targetClassificationId === classificationId,
        })),
      ];
    });
  };

  const handleRolesChanged = () => {
    void rolesQuery.refetch();
  };

  return (
    <Stack spacing={1.5} >
      <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
        <SecurityIcon color="primary" />
        <Typography variant="h5">مدیریت کنترل دسترسی</Typography>
      </Stack>

      <Tabs
        value={activeTab}
        onChange={(_event, value) => setActiveTab(value)}
        variant="scrollable"
        scrollButtons="auto"
        
      >
        <Tab label="نقش‌ها و مجوزها" />
        <Tab label="دسترسی طبقه‌بندی" />
      </Tabs>

      {activeTab === 0 && (
        <Stack spacing={1.5}>
          <RolePermissionsManager onRolesChanged={handleRolesChanged} />
          <UserRoleAssigner />
        </Stack>
      )}

      {activeTab === 1 && (
        <Paper variant="outlined" sx={{ p: { xs: 1.5, md: 2 }, borderRadius: 1 }}>
          <Stack spacing={1.5}>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
              <Select
                value={selectedSubjectType}
                onChange={(event) => {
                  const value = Number(event.target.value) as SubjectType;
                  setSelectedSubjectType(value);
                  setSelectedSubjectId(null);
                }}
              >
                <MenuItem value={1}>کاربر</MenuItem>
                <MenuItem value={2}>نقش</MenuItem>
              </Select>
              <Select
                value={selectedSubjectId ?? ''}
                onChange={(event) => setSelectedSubjectId(Number(event.target.value))}
                displayEmpty
              >
                
                <MenuItem value="" disabled>
                  انتخاب {selectedSubjectType === 1 ? 'کاربر' : 'نقش'}
                </MenuItem>
                {(selectedSubjectType === 1 ? users : roles).map((subject) => (
                  <MenuItem key={subject.id} value={subject.id}>
                    {'userName' in subject ? subject.userName : subject.name}
                  </MenuItem>
                ))}
              </Select>
            </Stack>

            <Stack direction={{ xs: 'column', lg: 'row' }} spacing={1.25} alignItems="stretch">
              <Box sx={{ width: { xs: '100%', lg: 360 }, flexShrink: 0 }}>
                <CategoryRadioTreeView
                  categories={categorySelectorTree}
                  value={selectedClassification != null ? String(selectedClassification) : null}
                  onChange={(id) => setSelectedClassification(Number(id))}
                  label="انتخاب طبقه‌بندی برای ویرایش ACL"
                  emptyText="هیچ طبقه‌بندی فعالی برای نمایش وجود ندارد."
                  maxHeight={420}
                  minHeight={220}
                  showSearch
                  searchPlaceholder="جستجو در طبقه‌بندی‌ها..."
                />
              </Box>

              <Paper variant="outlined" sx={{ p: 1, borderRadius: 1, flex: 1, minWidth: 0 }}>
                {selectedCategoryNode && (
                  <Alert severity="info" sx={{ mb: 1 }}>
                    در حال ویرایش ACL زیرشاخه‌های: <strong>{selectedCategoryNode.title}</strong>
                  </Alert>
                )}
                <Stack spacing={0.5} sx={{ maxHeight: { xs: 420, md: 560 }, overflow: 'auto', pe: 0.5 }}>
                  <CategoryAclTreeView
                    nodes={focusedAclTree}
                    entries={classificationDraft}
                    subjectType={selectedSubjectType}
                    subjectId={selectedSubjectId}
                    cascadeByClassificationId={cascadeByClassificationId}
                    onCascadeChange={updateCascade}
                    onPermissionChange={upsertClassificationEntry}
                  />
                </Stack>
              </Paper>
            </Stack>

            <Button
              variant="contained"
              disabled={!selectedClassification || !selectedCategoryNode || !selectedSubjectId || saveClassificationAclMutation.isPending}
              onClick={async () => {
                if (!selectedClassification || !selectedCategoryNode) {
                  return;
                }

                const scopedDraft = classificationDraft.filter((entry) => selectedSubtreeIds.has(entry.classificationId));
                const entriesByClassification = scopedDraft.reduce<Record<number, ClassificationAclEntryDto[]>>(
                  (groups, entry) => {
                    groups[entry.classificationId] = [...(groups[entry.classificationId] ?? []), entry];
                    return groups;
                  },
                  {},
                );

                const targets = Object.entries(entriesByClassification).sort(
                  ([leftId, leftEntries], [rightId, rightEntries]) => {
                    const leftCascades = leftEntries.some((entry) => entry.cascade);
                    const rightCascades = rightEntries.some((entry) => entry.cascade);
                    if (leftCascades !== rightCascades) {
                      return leftCascades ? 1 : -1;
                    }

                    if (leftCascades && rightCascades) {
                      const leftLevel =
                        findCategoryNodeById(categoryTree, Number(leftId))?.level ?? 0;
                      const rightLevel =
                        findCategoryNodeById(categoryTree, Number(rightId))?.level ?? 0;
                      return rightLevel - leftLevel;
                    }

                    return Number(leftId) - Number(rightId);
                  },
                );
                if (targets.length === 0) {
                  await saveClassificationAclMutation.mutateAsync({
                    classificationId: selectedClassification,
                    entries: [],
                  });
                } else {
                  for (const [classificationId, entries] of targets) {
                    await saveClassificationAclMutation.mutateAsync({
                      classificationId: Number(classificationId),
                      entries,
                    });
                  }
                }

                const refreshedAcl = await classificationAclQuery.refetch();
                setClassificationDraft(refreshedAcl.data ?? []);
                setCascadeByClassificationId({});
                pushToast('ACL طبقه‌بندی ذخیره شد.', 'success');
              }}
              fullWidth={isCompact}
            >
              {saveClassificationAclMutation.isPending ? 'در حال ذخیره...' : 'ذخیره ACL طبقه‌بندی'}
            </Button>
          </Stack>
        </Paper>
      )}
    </Stack>
  );
};
