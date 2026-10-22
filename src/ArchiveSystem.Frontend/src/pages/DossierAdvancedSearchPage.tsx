import SearchIcon from '@mui/icons-material/Search';
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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useState } from 'react';
import { AdvancedSmartFilter } from '@/components/dossiers/AdvancedSmartFilter';
import { dossierSearchApi } from '@/services/dossierSearchApi';
import type {
  DossierAdvancedSearchResult,
  DossierConditionGroup,
  DossierSearchFieldDescriptor,
} from '@/types/dossierSearch';
import { formatOwnerNames } from '@/utils/ownerDisplay';

const usageTypeLabel: Record<number, string> = {
  0: 'Unknown',
  1: 'Residential',
  2: 'Commercial',
  3: 'Industrial',
  4: 'Administrative',
  5: 'Agricultural',
  6: 'Mixed Use',
  7: 'Other',
};

export const DossierAdvancedSearchPage = () => {
  const [classificationScope, setClassificationScope] = useState('');
  const [fields, setFields] = useState<DossierSearchFieldDescriptor[]>([]);
  const [fieldsLoading, setFieldsLoading] = useState(false);
  const [fieldsError, setFieldsError] = useState('');
  const [searching, setSearching] = useState(false);
  const [searchError, setSearchError] = useState('');
  const [result, setResult] = useState<DossierAdvancedSearchResult | null>(null);
  const [root, setRoot] = useState<DossierConditionGroup | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [sortBy, setSortBy] = useState('modifiedAt');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('desc');
  const [includeDeleted, setIncludeDeleted] = useState(false);

  useEffect(() => {
    void loadFields();
  }, [classificationScope]);

  const loadFields = async () => {
    setFieldsLoading(true);
    setFieldsError('');
    try {
      const parsedScope = Number.parseInt(classificationScope, 10);
      const scope = Number.isNaN(parsedScope) ? undefined : parsedScope;
      const response = await dossierSearchApi.getAdvancedSearchFields(scope);
      setFields(response.fields ?? []);
    } catch (error) {
      console.error(error);
      setFields([]);
      setFieldsError('Failed to load searchable fields.');
    } finally {
      setFieldsLoading(false);
    }
  };

  const runSearch = async (inputRoot: DossierConditionGroup, nextPage = 1) => {
    setSearching(true);
    setSearchError('');
    setRoot(inputRoot);
    setPage(nextPage);

    try {
      const response = await dossierSearchApi.search({
        root: inputRoot,
        page: nextPage,
        pageSize,
        sortBy,
        sortDirection,
        includeDeleted,
      });

      setResult(response);
    } catch (error) {
      console.error(error);
      setSearchError('Advanced search request failed.');
      setResult(null);
    } finally {
      setSearching(false);
    }
  };

  const canGoPrevious = page > 1;
  const canGoNext = result ? page * pageSize < result.totalCount : false;

  return (
    <Stack spacing={1.5}>
      <Stack spacing={0.25}>
        <Typography variant="h5">جستجوی پیشرفته پرونده</Typography>
        <Typography variant="body2" color="text.secondary">
          Build nested AND/OR queries across dossier, owner, and dynamic fields.
        </Typography>
      </Stack>

      <Paper variant="outlined" sx={{ p: 1.25 }}>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1}>
          <TextField
            size="small"
            label="Classification Scope (Optional)"
            value={classificationScope}
            onChange={(event) => setClassificationScope(event.target.value)}
            placeholder="e.g. 12"
          />

          <Select size="small" value={sortBy} onChange={(event) => setSortBy(event.target.value)}>
            <MenuItem value="modifiedAt">Modified At</MenuItem>
            <MenuItem value="createdAt">Created At</MenuItem>
            <MenuItem value="dossierNumber">Dossier Number</MenuItem>
            <MenuItem value="externalCaseNumber">External Case Number</MenuItem>
            <MenuItem value="address">Address</MenuItem>
            <MenuItem value="usageType">Usage Type</MenuItem>
          </Select>

          <Select size="small" value={sortDirection} onChange={(event) => setSortDirection(event.target.value as 'asc' | 'desc')}>
            <MenuItem value="desc">Descending</MenuItem>
            <MenuItem value="asc">Ascending</MenuItem>
          </Select>

          <Select
            size="small"
            value={String(pageSize)}
            onChange={(event) => setPageSize(Number(event.target.value))}
          >
            <MenuItem value="10">10</MenuItem>
            <MenuItem value="20">20</MenuItem>
            <MenuItem value="50">50</MenuItem>
            <MenuItem value="100">100</MenuItem>
          </Select>

          <FormControlLabel
            control={
              <Checkbox
                checked={includeDeleted}
                onChange={(event) => setIncludeDeleted(event.target.checked)}
              />
            }
            label="Include Soft Deleted"
          />
        </Stack>
      </Paper>

      {fieldsError && <Alert severity="error">{fieldsError}</Alert>}

      <AdvancedSmartFilter
        fields={fields}
        loading={fieldsLoading || searching}
        onApply={(nextRoot) => void runSearch(nextRoot, 1)}
      />

      {searchError && <Alert severity="error">{searchError}</Alert>}

      <Paper variant="outlined" sx={{ p: 1.25 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} justifyContent="space-between" alignItems={{ sm: 'center' }}>
          <Typography variant="subtitle1">
            Results: {result?.totalCount ?? 0}
          </Typography>
          <Stack direction="row" spacing={1}>
            <Button
              size="small"
              variant="outlined"
              disabled={!root || !canGoPrevious || searching}
              onClick={() => root && void runSearch(root, page - 1)}
            >
              Previous
            </Button>
            <Button
              size="small"
              variant="outlined"
              disabled={!root || !canGoNext || searching}
              onClick={() => root && void runSearch(root, page + 1)}
            >
              Next
            </Button>
            <Button
              size="small"
              variant="contained"
              startIcon={<SearchIcon />}
              disabled={!root || searching}
              onClick={() => root && void runSearch(root, page)}
            >
              Refresh
            </Button>
          </Stack>
        </Stack>

        <Box sx={{ overflowX: 'auto', mt: 1 }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>ID</TableCell>
                <TableCell>Dossier Number</TableCell>
                <TableCell>External Number</TableCell>
                <TableCell>Address</TableCell>
                <TableCell>Usage</TableCell>
                <TableCell>Owners</TableCell>
                <TableCell>Classification</TableCell>
                <TableCell>Modified</TableCell>
                <TableCell>Deleted</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(result?.items ?? []).map((item) => (
                <TableRow key={item.id} hover>
                  <TableCell>{item.id}</TableCell>
                  <TableCell>{item.dossierNumber}</TableCell>
                  <TableCell>{item.externalCaseNumber}</TableCell>
                  <TableCell>{item.address}</TableCell>
                  <TableCell>{usageTypeLabel[item.usageType] ?? item.usageType}</TableCell>
                  <TableCell>{formatOwnerNames(item.ownerNames)}</TableCell>
                  <TableCell>{item.classificationId}</TableCell>
                  <TableCell>{new Date(item.modifiedAt ?? item.createdAt).toLocaleString('fa-IR')}</TableCell>
                  <TableCell>{item.isDeleted ? 'Yes' : 'No'}</TableCell>
                </TableRow>
              ))}

              {(result?.items.length ?? 0) === 0 && (
                <TableRow>
                  <TableCell colSpan={9}>
                    <Typography variant="body2" color="text.secondary">
                      No dossiers found for the selected query.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </Box>
      </Paper>
    </Stack>
  );
};

export default DossierAdvancedSearchPage;
