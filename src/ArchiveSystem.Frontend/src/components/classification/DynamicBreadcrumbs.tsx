import NavigateNextIcon from '@mui/icons-material/NavigateNext';
import FolderOutlinedIcon from '@mui/icons-material/FolderOutlined';
import { Breadcrumbs, ButtonBase, Stack, Typography } from '@mui/material';

export interface BreadcrumbPathItem {
  id: number;
  label: string;
}

interface DynamicBreadcrumbsProps {
  items: BreadcrumbPathItem[];
  onNavigate: (id: number) => void;
  emptyLabel?: string;
}

export const DynamicBreadcrumbs = ({
  items,
  onNavigate,
  emptyLabel = 'Select a folder to see its full path',
}: DynamicBreadcrumbsProps) => {
  if (items.length === 0) {
    return (
      <Stack
        direction="row"
        alignItems="center"
        spacing={1}
        sx={{
          minHeight: 38,
          px: 1.25,
          borderRadius: 1.5,
          border: '1px solid',
          borderColor: 'divider',
          backgroundColor: 'background.paper',
        }}
      >
        <FolderOutlinedIcon fontSize="small" color="action" />
        <Typography variant="body2" color="text.secondary" noWrap>
          {emptyLabel}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack
      direction="row"
      alignItems="center"
      sx={{
        minHeight: 38,
        px: 1.25,
        borderRadius: 1.5,
        border: '1px solid',
        borderColor: 'divider',
        backgroundColor: 'background.paper',
        overflowX: 'auto',
        overflowY: 'hidden',
      }}
    >
      <Breadcrumbs separator={<NavigateNextIcon fontSize="small" />} aria-label="classification-breadcrumbs">
        {items.map((item, index) => {
          const isCurrent = index === items.length - 1;

          return (
            <ButtonBase
              key={item.id}
              onClick={() => onNavigate(item.id)}
              sx={{
                borderRadius: 1,
                px: 0.75,
                py: 0.25,
                color: isCurrent ? 'primary.main' : 'text.secondary',
                fontWeight: isCurrent ? 700 : 500,
                whiteSpace: 'nowrap',
                '&:hover': {
                  backgroundColor: 'action.hover',
                },
              }}
            >
              <Typography variant="body2" component="span" sx={{ lineHeight: 1.2 }}>
                {item.label}
              </Typography>
            </ButtonBase>
          );
        })}
      </Breadcrumbs>
    </Stack>
  );
};

export { DynamicBreadcrumbs as BreadcrumbsAuto };
export { DynamicBreadcrumbs as Breadcrumbs };
