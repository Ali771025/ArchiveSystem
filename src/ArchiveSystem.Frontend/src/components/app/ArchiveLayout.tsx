import AccountCircleIcon from '@mui/icons-material/AccountCircle';
import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';
import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings';
import AssessmentIcon from '@mui/icons-material/Assessment';
import AssignmentReturnIcon from '@mui/icons-material/AssignmentReturn';
import AutoAwesomeMotionIcon from '@mui/icons-material/AutoAwesomeMotion';
import BadgeIcon from '@mui/icons-material/Badge';
import DarkModeIcon from '@mui/icons-material/DarkMode';
import ExitToAppIcon from '@mui/icons-material/ExitToApp';
import LightModeIcon from '@mui/icons-material/LightMode';
import MenuIcon from '@mui/icons-material/Menu';
import MoreVertIcon from '@mui/icons-material/MoreVert';
import SearchIcon from '@mui/icons-material/Search';
import {
  AppBar,
  Avatar,
  Box,
  Button,
  Container,
  Divider,
  Drawer,
  IconButton,
  Menu,
  MenuItem,
  Paper,
  Stack,
  Switch,
  Toolbar,
  Tooltip,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material';
"use client";

import React, { useEffect, useRef, useState } from 'react';
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { ClassificationExplorer } from '@/components/classification/ClassificationExplorer';
import {
  useClassificationTree,
  useCreateClassification,
  useDeleteClassification,
  useMoveClassification,
  useRenameClassification,
} from '@/services/archiveQueries';
import { useArchiveStore } from '@/stores/useArchiveStore';
import { useOwnerModalStore } from '@/stores/useOwnerModalStore';
import { useMovementLogStore } from '@/stores/useMovementLogStore';
import { useSessionStore } from '@/stores/useSessionStore';
import { useToastStore } from '@/stores/useToastStore';
import { useUIStore } from '@/stores/useUIStore';
import { useConfirm } from '@/components/common/useConfirm';
import { buildDeleteConfirmOptions } from '@/components/common/confirmPresets';
import { GlobalCaseSearchDialog } from '@/components/search/GlobalCaseSearchDialog';
import type { PermissionKey } from '@/types/archive';
import { formatFooterDate, formatFooterTime } from '@/utils/footerDateTime';
import { CASE_LIST_QUERY_KEYS, setOptionalQueryParam } from '@/utils/caseListUrlState';
import packageJson from '../../../package.json';

const DESKTOP_DRAWER_WIDTH = 320;
const DESKTOP_DRAWER_MIN_WIDTH = 280;
const DESKTOP_DRAWER_MAX_WIDTH = 640;
const MOBILE_DRAWER_WIDTH = 'min(22rem, 88vw)';
const DESKTOP_DRAWER_WIDTH_STORAGE_KEY = 'archive:classification-panel-width';
const LOGO_SRC = '/Logo/Logo.png';
const normalizeVersion = (version: string) => version.startsWith('v') ? version : `v${version}`;
const isPortfolioDemo = import.meta.env.MODE === 'showcase';
const appVersion = normalizeVersion(
  import.meta.env.VITE_APP_VERSION ??
  import.meta.env.VITE_VERSION ??
  packageJson.version,
);

interface NavigationItem {
  to: string;
  label: string;
  icon: React.ReactNode;
  allowedRoles?: string[];
  requiredPermission?: PermissionKey;
  requiredTabPermission?: PermissionKey;
  accessDeniedMessage?: string;
}

const navigationItems: NavigationItem[] = [
  { to: '/', label: 'پرونده‌ها', icon: <AutoAwesomeMotionIcon fontSize="small" />, requiredTabPermission: 'tabs.archive.view' },
  // { to: '/search/documents', label: 'جستجوی پیشرفته اسناد', icon: <SearchIcon fontSize="small" /> },
  // { to: '/dossiers/advanced-search', label: 'جستجوی پویای پرونده', icon: <SearchIcon fontSize="small" /> },
  {
    to: '/admin/dynamic-fields',
    label: 'اطلاعات تکمیلی',
    icon: <AutoAwesomeMotionIcon fontSize="small" />,
    requiredPermission: 'dynamicfields.manage',
    requiredTabPermission: 'tabs.settings.view',
  },
  {
    to: '/admin/access-control',
    label: 'کنترل دسترسی',
    icon: <AdminPanelSettingsIcon fontSize="small" />,
    requiredPermission: 'roles.manage',
    requiredTabPermission: 'tabs.accesscontrol.view',
  },
  {
    to: '/admin/case-loans',
    label: 'امانات پرونده فیزیکی',
    icon: <AssignmentReturnIcon fontSize="small" />,
    requiredPermission: 'caseloans.manage',
    requiredTabPermission: 'tabs.caseloans.view',
  },
  {
    to: '/admin/taxpayers',
    label: 'مدیریت مودیان',
    icon: <BadgeIcon fontSize="small" />,
    requiredPermission: 'taxpayers.manage',
    requiredTabPermission: 'tabs.taxpayers.view',
  },
  {
    to: '/admin/users',
    label: 'مدیریت کاربران',
    icon: <AccountCircleIcon fontSize="small" />,
    requiredPermission: 'users.manage',
    requiredTabPermission: 'tabs.users.view',
  },
  {
    to: '/reports',
    label: 'گزارش‌ها',
    icon: <AssessmentIcon fontSize="small" />,
    allowedRoles: ['Admin', 'Auditor', 'Superuser'],
    requiredTabPermission: 'tabs.reports.view',
    accessDeniedMessage: 'شما مجوز مشاهده گزارش‌های امنیتی و ممیزی را ندارید.',
  },
];

export const ArchiveLayout = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const theme = useTheme();
  const isRtl = theme.direction === 'rtl';
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));
  const isDesktop = useMediaQuery(theme.breakpoints.up('lg'));

  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [mobileTreeOpen, setMobileTreeOpen] = useState(false);
  const [mobileNavAnchor, setMobileNavAnchor] = useState<null | HTMLElement>(null);
  const [globalSearchOpen, setGlobalSearchOpen] = useState(false);
  const [systemNow, setSystemNow] = useState(() => new Date());
  const [desktopDrawerWidth, setDesktopDrawerWidth] = useState(() => {
    if (typeof window === 'undefined') {
      return DESKTOP_DRAWER_WIDTH;
    }

    const storedWidth = Number(
      window.localStorage.getItem(DESKTOP_DRAWER_WIDTH_STORAGE_KEY),
    );
    return Number.isFinite(storedWidth)
      ? Math.min(
          Math.max(storedWidth, DESKTOP_DRAWER_MIN_WIDTH),
          DESKTOP_DRAWER_MAX_WIDTH,
        )
      : DESKTOP_DRAWER_WIDTH;
  });
  const desktopDrawerWidthRef = useRef(desktopDrawerWidth);
  const resizeCleanupRef = useRef<(() => void) | null>(null);

  const { data: classifications = [], isLoading } = useClassificationTree();
  const createMutation = useCreateClassification();
  const renameMutation = useRenameClassification();
  const moveMutation = useMoveClassification();
  const deleteMutation = useDeleteClassification();

  const selectedClassificationId = useArchiveStore((state) => state.selectedClassificationId);
  const setSelectedClassificationId = useArchiveStore((state) => state.setSelectedClassificationId);
  const setSelectedCaseId = useArchiveStore((state) => state.setSelectedCaseId);

  const clearSession = useSessionStore((state) => state.clearSession);
  const user = useSessionStore((state) => state.user);
  const hasPermission = useSessionStore((state) => state.hasPermission);
  const hasCreatePermission = useSessionStore((state) => state.hasPermission('categories.create'));
  const hasEditPermission = useSessionStore((state) => state.hasPermission('categories.edit'));
  const canManageClassifications = useSessionStore((state) => state.hasPermission('categories.manage'));
  const canManageDossiers = useSessionStore((state) => state.hasPermission('dossiers.manage'));
  const pushToast = useToastStore((state) => state.push);
  const openCreateModal = useOwnerModalStore((state) => state.openCreateModal);
  const openMovementModal = () => useMovementLogStore.getState().openModal();
  const darkMode = useUIStore((state) => state.darkMode);
  const toggleDarkMode = useUIStore((state) => state.toggleDarkMode);
  const themeToggleLabel = darkMode ? 'تغییر به حالت روشن' : 'تغییر به حالت تیره';
  const userFullName = [user?.firstName, user?.lastName].filter(Boolean).join(' ').trim() || 'کاربر';
  const confirm = useConfirm();
  const canAccessNavigationItem = (item: NavigationItem) => {
    const roleAllowed =
      !item.allowedRoles || item.allowedRoles.some((allowedRole) => user?.roles.some((role) => role.toLowerCase() === allowedRole.toLowerCase()));
    const permissionAllowed = !item.requiredPermission || hasPermission(item.requiredPermission);
    const tabAllowed = !item.requiredTabPermission || hasPermission(item.requiredTabPermission);
    return roleAllowed && permissionAllowed && tabAllowed;
  };

  const showAccessDeniedMessage = (message?: string) => {
    pushToast(message ?? 'شما مجوز دسترسی به این بخش را ندارید.', 'warning');
  };

  const openUserMenu = (event: React.MouseEvent<HTMLElement>) => setAnchorEl(event.currentTarget);
  const closeUserMenu = () => setAnchorEl(null);

  useEffect(() => {
    const handler = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault();
        setGlobalSearchOpen(true);
      }
    };

    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, []);

  useEffect(() => {
    const intervalId = window.setInterval(() => setSystemNow(new Date()), 30000);
    return () => window.clearInterval(intervalId);
  }, []);

  useEffect(() => {
    if (isDesktop) {
      setMobileTreeOpen(false);
    }
  }, [isDesktop]);

  useEffect(() => {
    setMobileTreeOpen(false);
    setMobileNavAnchor(null);
  }, [location.pathname]);

  useEffect(
    () => () => {
      resizeCleanupRef.current?.();
    },
    [],
  );

  const updateDesktopDrawerWidth = (width: number) => {
    const nextWidth = Math.min(
      Math.max(width, DESKTOP_DRAWER_MIN_WIDTH),
      DESKTOP_DRAWER_MAX_WIDTH,
    );
    desktopDrawerWidthRef.current = nextWidth;
    setDesktopDrawerWidth(nextWidth);
  };

  const persistDesktopDrawerWidth = () => {
    if (typeof window !== 'undefined') {
      window.localStorage.setItem(
        DESKTOP_DRAWER_WIDTH_STORAGE_KEY,
        String(desktopDrawerWidthRef.current),
      );
    }
  };

  const handleDesktopResizeStart = (
    event: React.PointerEvent<HTMLDivElement>,
  ) => {
    event.preventDefault();
    resizeCleanupRef.current?.();

    const startX = event.clientX;
    const startWidth = desktopDrawerWidthRef.current;
    const directionMultiplier = isRtl ? -1 : 1;
    const previousCursor = document.body.style.cursor;
    const previousUserSelect = document.body.style.userSelect;
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';

    const handlePointerMove = (pointerEvent: PointerEvent) => {
      updateDesktopDrawerWidth(
        startWidth +
          (pointerEvent.clientX - startX) * directionMultiplier,
      );
    };
    const stopResize = () => {
      window.removeEventListener('pointermove', handlePointerMove);
      window.removeEventListener('pointerup', stopResize);
      window.removeEventListener('pointercancel', stopResize);
      document.body.style.cursor = previousCursor;
      document.body.style.userSelect = previousUserSelect;
      persistDesktopDrawerWidth();
      resizeCleanupRef.current = null;
    };

    resizeCleanupRef.current = stopResize;
    window.addEventListener('pointermove', handlePointerMove);
    window.addEventListener('pointerup', stopResize);
    window.addEventListener('pointercancel', stopResize);
  };

  const handleDesktopResizeKeyDown = (
    event: React.KeyboardEvent<HTMLDivElement>,
  ) => {
    const step = event.shiftKey ? 40 : 16;
    const widthDelta =
      event.key === 'ArrowLeft'
        ? isRtl
          ? step
          : -step
        : event.key === 'ArrowRight'
          ? isRtl
            ? -step
            : step
          : 0;

    if (widthDelta !== 0) {
      event.preventDefault();
      updateDesktopDrawerWidth(desktopDrawerWidthRef.current + widthDelta);
      persistDesktopDrawerWidth();
      return;
    }

    if (event.key === 'Home' || event.key === 'End') {
      event.preventDefault();
      updateDesktopDrawerWidth(
        event.key === 'Home'
          ? DESKTOP_DRAWER_MIN_WIDTH
          : DESKTOP_DRAWER_MAX_WIDTH,
      );
      persistDesktopDrawerWidth();
    }
  };

  const classificationContent = (
    <Paper
      square
      elevation={0}
      sx={{
        height: '100%',
        width: '100%',
        display: 'flex',
        flexDirection: 'column',
        borderInlineEnd: '1px solid',
        borderColor: theme.palette.mode === 'dark' ? 'rgba(203, 213, 225, 0.18)' : 'rgba(15, 23, 42, 0.12)',
        bgcolor: theme.palette.mode === 'dark' ? '#111827' : '#FFFFFF',
        color: theme.palette.mode === 'dark' ? '#F9FAFB' : 'text.primary',
        p: { xs: 1, md: 1.25 },
        overflow: 'hidden',
      }}
    >
      <ClassificationExplorer
        nodes={classifications}
        loading={isLoading}
        selectedId={selectedClassificationId}
        onSelectionChange={(id) => {
          setSelectedClassificationId(id);
          setSelectedCaseId(null);
          const nextSearchParams =
            location.pathname === '/'
              ? new URLSearchParams(location.search)
              : new URLSearchParams();
          setOptionalQueryParam(
            nextSearchParams,
            CASE_LIST_QUERY_KEYS.classificationId,
            id,
          );
          nextSearchParams.delete(CASE_LIST_QUERY_KEYS.page);
          nextSearchParams.delete(CASE_LIST_QUERY_KEYS.dynamicFilters);
          navigate({
            pathname: '/',
            search: nextSearchParams.toString(),
          });
          if (!isDesktop) {
            setMobileTreeOpen(false);
          }
        }}
        onCreate={async (title, parentId) => {
          if (!hasCreatePermission) {
            pushToast('شما مجوز ایجاد طبقه‌بندی ندارید.', 'warning');
            return;
          }
          await createMutation.mutateAsync({ title, parentId });
        }}
        onRename={async (id, title) => {
          if (!hasEditPermission) {
            pushToast('شما مجوز ویرایش طبقه‌بندی ندارید.', 'warning');
            return;
          }
          await renameMutation.mutateAsync({ id, title });
        }}
        onDelete={async (id) => {
          if (!canManageClassifications) {
            pushToast('شما مجوز مدیریت طبقه‌بندی ندارید.', 'warning');
            return;
          }
          const shouldDelete = await confirm({
            ...buildDeleteConfirmOptions('این طبقه‌بندی'),
            title: 'حذف طبقه‌بندی',
          });
          if (!shouldDelete) {
            return;
          }
          await deleteMutation.mutateAsync(id);
        }}
        onMove={async (id, parentId) => {
          if (!canManageClassifications) {
            pushToast('شما مجوز مدیریت طبقه‌بندی ندارید.', 'warning');
            return;
          }
          await moveMutation.mutateAsync({ id, parentId });
        }}
        hasCreatePermission={hasCreatePermission}
        hasEditPermission={hasEditPermission}
        hasManagePermission={canManageClassifications}
      />
    </Paper>
  );

  return (
    <Box
      className="app-content"
      sx={{
        minHeight: '100dvh',
        background: theme.palette.mode === 'dark' ? '#1F2937' : '#F3F6FA',
      }}
    >
      <AppBar
        position="sticky"
        elevation={0}
        sx={{
          bgcolor: theme.palette.mode === 'dark' ? '#0B1320' : '#FFFFFF',
          color: theme.palette.mode === 'dark' ? '#F9FAFB' : '#0B1320',
          borderBottom: '1px solid',
          borderColor: theme.palette.mode === 'dark' ? 'rgba(203, 213, 225, 0.14)' : 'rgba(15, 23, 42, 0.12)',
          boxShadow: theme.palette.mode === 'dark' ? 'none' : '0 1px 3px rgba(15, 23, 42, 0.06)',
        }}
      >
        <Toolbar
          sx={{
            py: 1,
            px: { xs: 1, sm: 2 },
            alignItems: 'center',
            flexWrap: 'wrap',
            gap: 1,
          }}
        >
          <Stack direction="row" spacing={0.5} alignItems="center" sx={{ width: { xs: '100%', md: 'auto' }, minWidth: 0 }}>
            <IconButton aria-label="open-classification-tree" onClick={() => setMobileTreeOpen(true)} sx={{ display: { lg: 'none' }, color: 'inherit' }}>
              <MenuIcon />
            </IconButton>
            <Box
              sx={{
                width: { xs: 136, sm: 178 },
                height: { xs: 40, sm: 48 },
                display: 'grid',
                placeItems: 'center',
                flexShrink: 0,
              }}
            >
              <Box
                component="img"
                src={LOGO_SRC}
                alt="سامانه بایگانی الکترونیک - مدیریت آرشیو و پرونده‌ها"
                onError={(event) => {
                  event.currentTarget.hidden = true;
                }}
                sx={{
                  width: '100%',
                  height: '100%',
                  objectFit: 'contain',
                  objectPosition: 'right center',
                  filter: theme.palette.mode === 'dark' ? 'drop-shadow(0 8px 18px rgba(0, 0, 0, 0.24))' : 'drop-shadow(0 4px 10px rgba(15, 23, 42, 0.12))',
                }}
              />
            </Box>
          </Stack>

          <Button
            variant="outlined"
            startIcon={<SearchIcon fontSize="small" />}
            sx={{
              width: { xs: '100%', md: 'auto' },
              minWidth: { md: 248 },
              borderColor: theme.palette.mode === 'dark' ? 'rgba(249, 250, 251, 0.28)' : 'rgba(15, 23, 42, 0.20)',
              color: 'inherit',
              '&:hover': {
                borderColor: '#2DD4BF',
                bgcolor: 'rgba(45, 212, 191, 0.10)',
              },
            }}
            onClick={() => setGlobalSearchOpen(true)}
          >
            {isMobile ? 'جستجوی پیشرفته' : 'جستجوی پیشرفته پرونده (Ctrl+K)'}
          </Button>

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={0.75} sx={{ width: { xs: '100%', sm: 'auto' } }}>
            {/* <Button
              variant="outlined"
              size="small"
              startIcon={<AssignmentReturnIcon />}
              onClick={openMovementModal}
              sx={{ width: { xs: '100%', sm: 'auto' }, minWidth: { sm: 152 } }}
            >
              ثبت امانت پرونده
            </Button> */}
            {/* {canManageDossiers ? (
              <Button
                variant="contained"
                size="small"
                startIcon={<AddCircleOutlineIcon />}
                onClick={openCreateModal}
                sx={{ width: { xs: '100%', sm: 'auto' }, minWidth: { sm: 124 } }}
              >
                ایجاد مودی
              </Button>
            ) : null} */}
          </Stack>

          {/* <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={0.75}
            sx={{ width: { xs: '100%', sm: 'auto' }, justifyContent: { sm: 'flex-end' }, alignItems: { sm: 'center' } }}
          >
            <Button
              variant="outlined"
              size="small"
              fullWidth={!isTablet}
              startIcon={<AddCircleOutlineIcon />}
              onClick={() => {
                setSelectedCaseId(null);
                navigate('/');
              }}
            >
              پرونده جدید
            </Button>
            <Button
              variant="outlined"
              size="small"
              fullWidth={!isTablet}
              startIcon={<UploadFileIcon />}
              onClick={() => {
                if (!selectedClassificationId) {
                  pushToast('برای بارگذاری سریع، ابتدا یک پرونده انتخاب کنید.', 'info');
                } else {
                  pushToast('برای بارگذاری فایل، وارد تب فایل‌های پرونده شوید.', 'info');
                }
              }}
            >
              بارگذاری فایل
            </Button>
          </Stack> */}

          <Stack
            direction="row"
            spacing={0.5}
            sx={{
              mr: { md: 'auto' },
              display: { xs: 'none', md: 'flex' },
              flexWrap: 'wrap',
            }}
          >
            {navigationItems.map((item) => (
              <NavButton
                key={item.to}
                to={item.to}
                label={item.label}
                icon={item.icon}
                allowed={canAccessNavigationItem(item)}
                onDenied={() => showAccessDeniedMessage(item.accessDeniedMessage)}
              />
            ))}
          </Stack>

          <Stack
            direction="row"
            spacing={0.25}
            alignItems="center"
            sx={{
              mr: { xs: 'auto', md: 0 },
              width: { xs: '100%', md: 'auto' },
              justifyContent: 'flex-end',
            }}
          >
            <IconButton
              aria-label="open-nav-menu"
              onClick={(event) => setMobileNavAnchor(event.currentTarget)}
              sx={{
                display: { xs: 'inline-flex', md: 'none' },
                color: 'inherit',
              }}
            >
              <MoreVertIcon />
            </IconButton>
            <Tooltip title={themeToggleLabel}>
              <Stack
                component="label"
                direction="row"
                spacing={0.25}
                alignItems="center"
                sx={{
                  border: '1px solid',
                  borderColor: theme.palette.mode === 'dark' ? 'rgba(249, 250, 251, 0.22)' : 'rgba(15, 23, 42, 0.16)',
                  borderRadius: 999,
                  bgcolor: theme.palette.mode === 'dark' ? 'rgba(249, 250, 251, 0.08)' : 'rgba(15, 23, 42, 0.04)',
                  cursor: 'pointer',
                  px: 0.75,
                }}
              >
                {darkMode ? <DarkModeIcon color="primary" fontSize="small" /> : <LightModeIcon color="secondary" fontSize="small" />}
                <Switch
                  checked={darkMode}
                  color="primary"
                  inputProps={{ 'aria-label': themeToggleLabel }}
                  onChange={() => toggleDarkMode()}
                  size="small"
                />
              </Stack>
            </Tooltip>
            <Button
              onClick={openUserMenu}
              aria-label={`منوی کاربری ${userFullName}`}
              sx={{
                minWidth: 0,
                maxWidth: { xs: 230, sm: 280 },
                px: 0.75,
                py: 0.4,
                color: 'inherit',
                borderRadius: 1.5,
                justifyContent: 'flex-start',
                textAlign: 'right',
                '&:hover': {
                  bgcolor: theme.palette.mode === 'dark' ? 'rgba(249, 250, 251, 0.08)' : 'rgba(15, 23, 42, 0.05)',
                },
              }}
            >
              <Avatar sx={{ width: 32, height: 32, ml: 0.75, flexShrink: 0 }}>
                <AccountCircleIcon />
              </Avatar>
              <Stack spacing={0} sx={{ minWidth: 0, alignItems: 'flex-start' }}>
                <Typography variant="body2" fontWeight={700} noWrap sx={{ width: '100%' }}>
                  {userFullName}
                </Typography>
              </Stack>
            </Button>
          </Stack>

          <Menu open={Boolean(anchorEl)} anchorEl={anchorEl} onClose={closeUserMenu}>
            <MenuItem disabled>{userFullName}</MenuItem>
            <MenuItem
              onClick={() => {
                closeUserMenu();
                clearSession();
                navigate('/login');
              }}
            >
              <ExitToAppIcon fontSize="small" sx={{ ml: 1 }} />
              خروج
            </MenuItem>
          </Menu>

          <Menu open={Boolean(mobileNavAnchor)} anchorEl={mobileNavAnchor} onClose={() => setMobileNavAnchor(null)}>
            {navigationItems.map((item) => {
              const allowed = canAccessNavigationItem(item);
              return (
                <MenuItem
                  key={item.to}
                  selected={location.pathname === item.to}
                  onClick={() => {
                    setMobileNavAnchor(null);
                    if (allowed) {
                      navigate(item.to);
                    } else {
                      showAccessDeniedMessage(item.accessDeniedMessage);
                    }
                  }}
                >
                  <Stack direction="row" spacing={1} alignItems="center">
                    {item.icon}
                    <span>{item.label}</span>
                  </Stack>
                </MenuItem>
              );
            })}
          </Menu>
        </Toolbar>
      </AppBar>

      <Box
        sx={{
          display: 'flex',
          flexDirection: 'row',
          minHeight: 'calc(100dvh - 104px)',
          pb: 3.5,
        }}
      >
        <Drawer
          open={mobileTreeOpen}
          onClose={() => setMobileTreeOpen(false)}
          anchor={isRtl ? 'right' : 'left'}
          PaperProps={{
            sx: {
              width: MOBILE_DRAWER_WIDTH,
              maxWidth: '100vw',
            },
          }}
          sx={{ display: { xs: 'block', lg: 'none' } }}
        >
          <Box sx={{ p: 1 }}>
            <Typography variant="subtitle1" sx={{ px: 1, py: 0.5 }}>
              طبقه‌بندی
            </Typography>
            <Divider sx={{ mb: 1 }} />
            <Box sx={{ height: 'calc(100dvh - 130px)' }}>{classificationContent}</Box>
          </Box>
        </Drawer>

        <Box
          sx={{
            display: { xs: 'none', lg: 'block' },
            position: 'relative',
            width: desktopDrawerWidth,
            minWidth: DESKTOP_DRAWER_MIN_WIDTH,
            maxWidth: `min(${DESKTOP_DRAWER_MAX_WIDTH}px, calc(100vw - 480px))`,
            flexShrink: 0,
          }}
        >
          <Box sx={{ position: 'sticky', top: 74, height: 'calc(100dvh - 118px)' }}>{classificationContent}</Box>
          <Box
            role="separator"
            aria-label="تغییر عرض پنل طبقه‌بندی"
            aria-orientation="vertical"
            aria-valuemin={DESKTOP_DRAWER_MIN_WIDTH}
            aria-valuemax={DESKTOP_DRAWER_MAX_WIDTH}
            aria-valuenow={Math.round(desktopDrawerWidth)}
            tabIndex={0}
            onPointerDown={handleDesktopResizeStart}
            onKeyDown={handleDesktopResizeKeyDown}
            sx={{
              position: 'absolute',
              insetInlineEnd: -5,
              top: 0,
              bottom: 0,
              width: 10,
              zIndex: (muiTheme) => muiTheme.zIndex.drawer + 1,
              cursor: 'col-resize',
              touchAction: 'none',
              outline: 'none',
              '&::after': {
                content: '""',
                position: 'absolute',
                insetInlineStart: 4,
                top: 0,
                bottom: 0,
                width: 2,
                bgcolor: 'divider',
                transition: 'background-color 120ms ease, width 120ms ease',
              },
              '&:hover::after, &:focus-visible::after': {
                width: 3,
                bgcolor: 'primary.main',
              },
            }}
          />
        </Box>

        <Container
          maxWidth={false}
          sx={{
            py: { xs: 1.25, sm: 2 },
            px: { xs: 1, sm: 2, lg: 2.5 },
            flex: 1,
            minWidth: 0,
          }}
        >
          {isPortfolioDemo && (
                <Box role="status" sx={{ mb: 1, px: 1.5, py: 0.75, borderRadius: 1, bgcolor: '#ecfdf5', color: '#065f46', fontSize: 12, textAlign: 'center' }}>
                  نسخه نمایشی نمونه‌کار · تمام داده‌ها ساختگی هستند
                </Box>
              )}
              <Outlet />
          <GlobalCaseSearchDialog open={globalSearchOpen} onClose={() => setGlobalSearchOpen(false)} />
        </Container>
      </Box>

      <Box
        component="footer"
        sx={{
          position: 'fixed',
          right: 0,
          left: 0,
          bottom: 0,
          zIndex: (muiTheme) => muiTheme.zIndex.drawer + 1,
          minHeight: 30,
          px: { xs: 1, sm: 2 },
          py: 0.5,
          borderTop: '1px solid',
          borderColor: theme.palette.mode === 'dark' ? 'rgba(203, 213, 225, 0.18)' : 'rgba(15, 23, 42, 0.12)',
          bgcolor: theme.palette.mode === 'dark' ? '#0B1320' : '#F8FAFC',
          color: theme.palette.mode === 'dark' ? '#CBD5E1' : 'text.secondary',
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
        }}
      >
        <Typography variant="caption" noWrap sx={{ fontWeight: 600 }}>
          نسخه: {appVersion} | {formatFooterDate(systemNow)} | ساعت: {formatFooterTime(systemNow)}
        </Typography>
      </Box>
    </Box>
  );
};

const NavButton = ({ to, label, icon, allowed, onDenied }: { to: string; label: string; icon: React.ReactNode; allowed: boolean; onDenied: () => void }) => {
  return (
    <Button
      component={allowed ? NavLink : 'button'}
      to={allowed ? to : undefined}
      size="small"
      startIcon={icon}
      onClick={(event: React.MouseEvent<HTMLButtonElement>) => {
        if (!allowed) {
          event.preventDefault();
          onDenied();
        }
      }}
      sx={{
        opacity: allowed ? 1 : 0.62,
        '&.active': {
          bgcolor: 'primary.main',
          color: 'primary.contrastText',
          '&:hover': {
            bgcolor: 'primary.dark',
          },
        },
      }}
    >
      {label}
    </Button>
  );
};
