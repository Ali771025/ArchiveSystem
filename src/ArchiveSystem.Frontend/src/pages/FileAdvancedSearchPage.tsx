import { Stack } from '@mui/material';
import { useEffect } from 'react';
import { AdvancedSearchForm } from '@/components/search/AdvancedSearchForm';
import { SearchResults } from '@/components/search/SearchResults';
import { useAdvancedSearchStore } from '@/stores/useAdvancedSearchStore';

export const FileAdvancedSearchPage = () => {
  const syncStateFromUrl = useAdvancedSearchStore((state) => state.syncStateFromUrl);

  useEffect(() => {
    void syncStateFromUrl();
  }, [syncStateFromUrl]);

  return (
    <Stack spacing={1.5}>
      <AdvancedSearchForm />
      <SearchResults />
    </Stack>
  );
};

export default FileAdvancedSearchPage;
