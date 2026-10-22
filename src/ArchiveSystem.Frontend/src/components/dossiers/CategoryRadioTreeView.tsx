import ChevronLeftIcon from '@mui/icons-material/ChevronLeft';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import FolderOutlinedIcon from '@mui/icons-material/FolderOutlined';
import SearchIcon from '@mui/icons-material/Search';
import UnfoldLessIcon from '@mui/icons-material/UnfoldLess';
import UnfoldMoreIcon from '@mui/icons-material/UnfoldMore';
import {
  Box,
  Breadcrumbs,
  IconButton,
  InputAdornment,
  Paper,
  Radio,
  Stack,
  TextField,
  Tooltip,
  Typography,
  useTheme,
} from '@mui/material';
import { alpha } from '@mui/material/styles';
import { useEffect, useMemo, useState } from 'react';
import { compareClassificationsArchiveLast } from '@/utils/classificationOrder';

export interface Category {
  id: string;
  name: string;
  systemKey?: string | null;
  children?: Category[];
}

export interface CategoryRadioTreeViewProps {
  categories: Category[];
  value: string | null;
  onChange: (id: string) => void;
  label?: string;
  emptyText?: string;
  maxHeight?: number | string;
  minHeight?: number | string;
  searchPlaceholder?: string;
  showSearch?: boolean;
  noResultText?: string;
  disabledIds?: string[];
  disableParentSelection?: boolean;
  desktopHeight?: number | string;
}

export const getCategoryPath = (
  categories: Category[],
  selectedId: string | null,
  separator = ' / ',
): string => {
  if (!selectedId) {
    return '';
  }

  const walk = (nodes: Category[], trail: string[]): string[] | null => {
    for (const node of nodes) {
      const nextTrail = [...trail, node.name];

      if (node.id === selectedId) {
        return nextTrail;
      }

      const childPath = walk(node.children ?? [], nextTrail);
      if (childPath) {
        return childPath;
      }
    }

    return null;
  };

  return walk(categories, [])?.join(separator) ?? '';
};

const collectExpandableIds = (categories: Category[]): string[] => {
  const ids: string[] = [];

  categories.forEach((category) => {
    if ((category.children ?? []).length > 0) {
      ids.push(category.id, ...collectExpandableIds(category.children ?? []));
    }
  });

  return ids;
};

const findPathIds = (categories: Category[], selectedId: string | null): string[] => {
  if (!selectedId) {
    return [];
  }

  const walk = (nodes: Category[], trail: string[]): string[] | null => {
    for (const node of nodes) {
      const nextTrail = [...trail, node.id];
      if (node.id === selectedId) {
        return nextTrail;
      }

      const nested = walk(node.children ?? [], nextTrail);
      if (nested) {
        return nested;
      }
    }

    return null;
  };

  return walk(categories, []) ?? [];
};

const collectParentIds = (categories: Category[]): Set<string> => {
  const ids = new Set<string>();
  const walk = (nodes: Category[]) => {
    nodes.forEach((node) => {
      if ((node.children ?? []).length > 0) {
        ids.add(node.id);
        walk(node.children ?? []);
      }
    });
  };

  walk(categories);
  return ids;
};

const countCategories = (categories: Category[]): number =>
  categories.reduce((total, category) => total + 1 + countCategories(category.children ?? []), 0);

const sortCategoryTree = (categories: Category[]): Category[] =>
  categories
    .map((category) => ({
      ...category,
      children: sortCategoryTree(category.children ?? []),
    }))
    .sort(compareClassificationsArchiveLast);

const filterCategories = (categories: Category[], query: string): Category[] => {
  const normalizedQuery = query.trim().toLocaleLowerCase('fa-IR');
  if (!normalizedQuery) {
    return categories;
  }

  return categories
    .map((category): Category | null => {
      const children = filterCategories(category.children ?? [], normalizedQuery);
      const matches = category.name.toLocaleLowerCase('fa-IR').includes(normalizedQuery);

      if (!matches && children.length === 0) {
        return null;
      }

      return {
        ...category,
        children,
      };
    })
    .filter((category): category is Category => category !== null);
};

interface RecursiveTreeItemProps {
  category: Category;
  depth: number;
  selectedId: string | null;
  onSelect: (id: string) => void;
  expandedIds: Set<string>;
  onToggle: (id: string) => void;
  disabledIds: Set<string>;
  parentIds: Set<string>;
  disableParentSelection: boolean;
}

const RecursiveTreeItem = ({
  category,
  depth,
  selectedId,
  onSelect,
  expandedIds,
  onToggle,
  disabledIds,
  parentIds,
  disableParentSelection,
}: RecursiveTreeItemProps) => {
  const theme = useTheme();
  const isSelected = selectedId === category.id;
  const isDisabled = disabledIds.has(category.id) || (disableParentSelection && parentIds.has(category.id));
  const hasChildren = (category.children ?? []).length > 0;
  const isExpanded = hasChildren && expandedIds.has(category.id);

  const selectCategory = () => {
    if (!isDisabled) {
      onSelect(category.id);
    }
  };

  return (
    <Box component="li" role="none" sx={{ m: 0, p: 0, listStyle: 'none' }}>
      <Box
        role="treeitem"
        dir="rtl"
        aria-level={depth + 1}
        aria-selected={isSelected}
        aria-disabled={isDisabled || undefined}
        aria-expanded={hasChildren ? isExpanded : undefined}
        tabIndex={isSelected ? 0 : -1}
        style={{
          // Inline physical spacing deliberately bypasses Emotion's RTL
          // transformer, which would otherwise swap right and left again.
          paddingRight: `${6 + depth * 20}px`,
          paddingLeft: '6px',
          textAlign: 'right',
        }}
        onClick={selectCategory}
        onKeyDown={(event) => {
          if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            selectCategory();
          } else if (event.key === 'ArrowLeft' && hasChildren && !isExpanded) {
            event.preventDefault();
            onToggle(category.id);
          } else if (event.key === 'ArrowRight' && hasChildren && isExpanded) {
            event.preventDefault();
            onToggle(category.id);
          }
        }}
        sx={{
          boxSizing: 'border-box',
          width: '100%',
          minHeight: 38,
          my: 0.25,
          display: 'flex',
          flexDirection: 'row',
          alignItems: 'center',
          gap: 0.75,
          textAlign: 'start',
          borderRadius: 1,
          cursor: isDisabled ? 'default' : 'pointer',
          opacity: isDisabled ? 0.55 : 1,
          color: isSelected ? 'primary.main' : 'text.primary',
          bgcolor: isSelected ? alpha(theme.palette.primary.main, 0.13) : 'transparent',
          transition: 'background-color 120ms ease, color 120ms ease',
          '&:hover': {
            bgcolor: isSelected
              ? alpha(theme.palette.primary.main, 0.18)
              : alpha(theme.palette.primary.main, 0.08),
          },
          '&:focus-visible': {
            outline: `2px solid ${alpha(theme.palette.primary.main, 0.65)}`,
            outlineOffset: -2,
          },
        }}
      >
        {hasChildren ? (
          <IconButton
            size="small"
            aria-label={isExpanded ? `بستن ${category.name}` : `باز کردن ${category.name}`}
            aria-expanded={isExpanded}
            onClick={(event) => {
              event.stopPropagation();
              onToggle(category.id);
            }}
            sx={{ width: 28, height: 28, flex: '0 0 28px' }}
          >
            {isExpanded ? <ExpandMoreIcon fontSize="small" /> : <ChevronLeftIcon fontSize="small" />}
          </IconButton>
        ) : (
          <Box aria-hidden sx={{ width: 28, height: 28, flex: '0 0 28px' }} />
        )}

        <Radio
          size="small"
          checked={isSelected}
          disabled={isDisabled}
          onChange={selectCategory}
          onClick={(event) => event.stopPropagation()}
          inputProps={{ 'aria-label': category.name }}
          sx={{ p: 0.5, flexShrink: 0 }}
        />

        <FolderOutlinedIcon
          fontSize="small"
          color={isSelected ? 'primary' : 'action'}
          sx={{ flexShrink: 0 }}
        />

        <Typography
          variant="body2"
          noWrap
          title={category.name}
          dir="rtl"
          sx={{
            minWidth: 0,
            flex: 1,
            textAlign: 'start',
            unicodeBidi: 'plaintext',
            fontWeight: isSelected ? 700 : 500,
          }}
        >
          {category.name}
        </Typography>
      </Box>

      {isExpanded && (
        <Box component="ul" role="group" dir="rtl" sx={{ m: 0, p: 0 }}>
          {(category.children ?? []).map((child) => (
            <RecursiveTreeItem
              key={child.id}
              category={child}
              depth={depth + 1}
              selectedId={selectedId}
              onSelect={onSelect}
              expandedIds={expandedIds}
              onToggle={onToggle}
              disabledIds={disabledIds}
              parentIds={parentIds}
              disableParentSelection={disableParentSelection}
            />
          ))}
        </Box>
      )}
    </Box>
  );
};

export const CategoryRadioTreeView = ({
  categories,
  value,
  onChange,
  label = 'دسته‌بندی اسناد',
  emptyText = 'دسته‌بندی قابل نمایش وجود ندارد.',
  maxHeight = 360,
  minHeight = 220,
  searchPlaceholder = 'جستجوی طبقه‌بندی...',
  showSearch = true,
  noResultText = 'نتیجه‌ای برای جستجو یافت نشد.',
  disabledIds = [],
  disableParentSelection = false,
  desktopHeight,
}: CategoryRadioTreeViewProps) => {
  const theme = useTheme();
  const [query, setQuery] = useState('');
  const orderedCategories = useMemo(() => sortCategoryTree(categories), [categories]);
  const [expandedItems, setExpandedItems] = useState<string[]>(() => collectExpandableIds(orderedCategories));

  const visibleCategories = useMemo(() => filterCategories(orderedCategories, query), [orderedCategories, query]);
  const selectedPath = useMemo(() => getCategoryPath(orderedCategories, value), [orderedCategories, value]);
  const totalCount = useMemo(() => countCategories(orderedCategories), [orderedCategories]);
  const visibleCount = useMemo(() => countCategories(visibleCategories), [visibleCategories]);
  const disabledIdSet = useMemo(() => new Set(disabledIds), [disabledIds]);
  const parentIds = useMemo(() => collectParentIds(orderedCategories), [orderedCategories]);

  useEffect(() => {
    if (!query.trim()) {
      return;
    }

    setExpandedItems(collectExpandableIds(visibleCategories));
  }, [query, visibleCategories]);

  useEffect(() => {
    if (query.trim() || !value) {
      return;
    }

    const selectedPathIds = findPathIds(orderedCategories, value);
    if (selectedPathIds.length === 0) {
      return;
    }

    setExpandedItems((current) => Array.from(new Set([...current, ...selectedPathIds])));
  }, [orderedCategories, query, value]);

  const handleSelect = (id: string) => {
    if (disabledIdSet.has(id) || (disableParentSelection && parentIds.has(id))) {
      return;
    }

    if (id !== value) {
      onChange(id);
    }
  };

  const expandAll = () => setExpandedItems(collectExpandableIds(visibleCategories));
  const collapseAll = () => setExpandedItems([]);
  const expandedIdSet = useMemo(() => new Set(expandedItems), [expandedItems]);
  const toggleExpanded = (id: string) => {
    setExpandedItems((current) =>
      current.includes(id) ? current.filter((itemId) => itemId !== id) : [...current, id],
    );
  };

  return (
    <Paper
      dir="rtl"
      variant="outlined"
      sx={{
        p: 1.5,
        textAlign: 'start',
        borderColor: 'divider',
        bgcolor: 'background.paper',
        height: { xs: 'auto', xl: desktopHeight ?? 'auto' },
        minHeight: 0,
        overflow: 'hidden',
        display: 'flex',
        flexDirection: 'column',
      }}
    >
      <Stack spacing={1.25} sx={{ minWidth: 0, minHeight: 0, flex: 1 }}>
        <Stack spacing={1}>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            alignItems={{ xs: 'stretch', sm: 'center' }}
            sx={{ flexWrap: { sm: 'wrap' }, gap: 1 }}
          >
            <Stack direction="row" spacing={0.75} alignItems="center" sx={{ flexShrink: 0 }}>
              <Typography variant="subtitle2">{label}</Typography>
              {totalCount > 0 && (
                <Typography variant="caption" color="text.secondary">
                  {query.trim() ? `${visibleCount} / ${totalCount}` : `${totalCount} مورد`}
                </Typography>
              )}
            </Stack>

            <Box
              sx={{
                flex: 1,
                minWidth: { xs: 0, sm: 240 },
                minHeight: 40,
                px: 1.25,
                py: 0.75,
                display: 'flex',
                alignItems: 'center',
                border: 1,
                borderColor: 'divider',
                borderRadius: 1,
                bgcolor: alpha(theme.palette.primary.main, 0.03),
                overflow: 'hidden',
              }}
            >
              {selectedPath ? (
                <Breadcrumbs
                  dir="rtl"
                  separator="/"
                  sx={{
                    minWidth: 0,
                    width: '100%',
                    '& .MuiBreadcrumbs-ol': { flexWrap: 'wrap' },
                    '& .MuiBreadcrumbs-li': { minWidth: 0, maxWidth: '100%' },
                  }}
                >
                  {selectedPath.split(' / ').map((part, index, items) => (
                    <Typography
                      key={`${part}-${index}`}
                      variant="body2"
                      color={index === items.length - 1 ? 'primary' : 'text.secondary'}
                      sx={{
                        fontWeight: index === items.length - 1 ? 700 : 500,
                        overflowWrap: 'anywhere',
                      }}
                    >
                      {part}
                    </Typography>
                  ))}
                </Breadcrumbs>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  دسته‌ای انتخاب نشده است.
                </Typography>
              )}
            </Box>
          </Stack>

          {showSearch && categories.length > 0 && (
            <Stack direction="row" spacing={0.75} alignItems="center">
              <TextField
                dir="rtl"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
                placeholder={searchPlaceholder}
                fullWidth
                size="small"
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchIcon fontSize="small" />
                    </InputAdornment>
                  ),
                }}
                inputProps={{
                  dir: 'rtl',
                  style: { textAlign: 'right' },
                }}
              />
              <Tooltip title="باز کردن همه">
                <IconButton size="small" onClick={expandAll} sx={{ flexShrink: 0 }}>
                  <UnfoldMoreIcon fontSize="small" />
                </IconButton>
              </Tooltip>
              <Tooltip title="بستن همه">
                <IconButton size="small" onClick={collapseAll} sx={{ flexShrink: 0 }}>
                  <UnfoldLessIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            </Stack>
          )}
        </Stack>

        {categories.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            {emptyText}
          </Typography>
        ) : visibleCategories.length === 0 ? (
          <Box
            dir="rtl"
            sx={{
              minHeight,
              flex: desktopHeight ? { xl: 1 } : undefined,
              display: 'grid',
              placeItems: 'center',
              border: 1,
              borderColor: 'divider',
              borderRadius: 1,
              bgcolor: (currentTheme) => alpha(currentTheme.palette.text.primary, 0.02),
            }}
          >
            <Typography variant="body2" color="text.secondary">
              {noResultText}
            </Typography>
          </Box>
        ) : (
          <Box
            dir="rtl"
            sx={{
              maxHeight: desktopHeight ? { xs: maxHeight, xl: 'none' } : maxHeight,
              minHeight,
              flex: desktopHeight ? { xl: 1 } : undefined,
              overflow: 'auto',
              border: 1,
              borderColor: 'divider',
              borderRadius: 1,
              p: 0.5,
              scrollbarGutter: 'stable',
              bgcolor: (currentTheme) =>
                currentTheme.palette.mode === 'dark'
                  ? alpha(currentTheme.palette.common.white, 0.02)
                  : alpha(currentTheme.palette.primary.main, 0.025),
              '&::-webkit-scrollbar': {
                width: 8,
                height: 8,
              },
              '&::-webkit-scrollbar-thumb': {
                borderRadius: 999,
                bgcolor: alpha(theme.palette.text.primary, 0.22),
              },
              '&::-webkit-scrollbar-thumb:hover': {
                bgcolor: alpha(theme.palette.text.primary, 0.32),
              },
              '&::-webkit-scrollbar-track': {
                bgcolor: alpha(theme.palette.text.primary, 0.04),
              },
            }}
          >
            <Box
              component="ul"
              role="tree"
              aria-label={label}
              dir="rtl"
              sx={{
                m: 0,
                p: 0,
                textAlign: 'start',
                minWidth: { xs: 360, sm: '100%' },
              }}
            >
              {visibleCategories.map((category) => (
                <RecursiveTreeItem
                  key={category.id}
                  category={category}
                  depth={0}
                  selectedId={value}
                  onSelect={handleSelect}
                  expandedIds={expandedIdSet}
                  onToggle={toggleExpanded}
                  disabledIds={disabledIdSet}
                  parentIds={parentIds}
                  disableParentSelection={disableParentSelection}
                />
              ))}
            </Box>
          </Box>
        )}
      </Stack>
    </Paper>
  );
};
