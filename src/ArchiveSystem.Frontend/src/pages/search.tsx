// src/pages/search.tsx
import React, { useState } from 'react';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/common/Input';
import { Button } from '@/components/common/Button';
import { useFileStore } from '@/stores/useFileStore';
import { Search as SearchIcon, Filter, FileText, Calendar, Tag } from 'lucide-react';

const SearchPage = () => {
  const [query, setQuery] = useState('');
  const [filters, setFilters] = useState({
    dateRange: '',
    fileType: '',
    tags: ''
  });
  const { searchFiles, searchResults, loading } = useFileStore();

  const handleSearch = async () => {
    await searchFiles(query, filters);
  };

  const filterOptions = [
    { label: 'PDF', value: 'pdf' },
    { label: 'Documents', value: 'docx' },
    { label: 'Images', value: 'jpg,png' },
    { label: 'Spreadsheets', value: 'xlsx' },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight sm:text-3xl">Advanced Search</h1>
        <p className="text-muted-foreground">Find documents across your entire organization</p>
      </div>

      <Card>
        <CardContent className="p-4 sm:p-6">
          <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:gap-4">
            <div className="flex-1 relative">
              <SearchIcon className="absolute right-3 top-3 h-4 w-4 text-muted-foreground" />
              <Input
                type="text"
                placeholder="Enter search terms..."
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                className="h-11 pr-10 text-base sm:h-12 sm:text-lg"
                onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
              />
            </div>
            <Button onClick={handleSearch} className="h-11 w-full px-6 sm:h-12 sm:w-auto" disabled={loading}>
              {loading ? 'Searching...' : 'Search'}
            </Button>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
            {/* Filters Sidebar */}
            <div className="lg:col-span-1 space-y-6">
              <div>
                <h3 className="font-semibold mb-3 flex items-center gap-2">
                  <Filter className="h-4 w-4" />
                  Filters
                </h3>
                
                <div className="space-y-4">
                  <div>
                    <label htmlFor="date-range-select" className="block text-sm font-medium mb-1">Date Range</label>
                    <select
                      id="date-range-select"
                      className="w-full p-2 border rounded text-sm"
                      value={filters.dateRange}
                      onChange={(e) => setFilters({...filters, dateRange: e.target.value})}
                    >
                      <option value="">Any time</option>
                      <option value="last_week">Last week</option>
                      <option value="last_month">Last month</option>
                      <option value="last_year">Last year</option>
                    </select>
                  </div>

                  <div>
                    <label htmlFor="file-type-select" className="block text-sm font-medium mb-1">File Type</label>
                    <select
                      id="file-type-select"
                      className="w-full p-2 border rounded text-sm"
                      value={filters.fileType}
                      onChange={(e) => setFilters({...filters, fileType: e.target.value})}
                    >
                      <option value="">All types</option>
                      {filterOptions.map(option => (
                        <option key={option.value} value={option.value}>{option.label}</option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="block text-sm font-medium mb-1">Tags</label>
                    <Input
                      type="text"
                      placeholder="Enter tags..."
                      value={filters.tags}
                      onChange={(e) => setFilters({...filters, tags: e.target.value})}
                      className="text-sm"
                    />
                  </div>
                </div>
              </div>
            </div>

            {/* Results */}
            <div className="lg:col-span-3">
              <div className="flex items-center justify-between mb-4">
                <h3 className="font-semibold">Search Results</h3>
                <span className="text-sm text-muted-foreground">
                  {searchResults.length} results found
                </span>
              </div>

              <div className="space-y-4">
                {loading ? (
                  <div className="text-center py-8">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto"></div>
                    <p className="mt-2 text-muted-foreground">Searching...</p>
                  </div>
                ) : searchResults.length === 0 ? (
                  <div className="text-center py-8">
                    <FileText className="h-12 w-12 text-muted-foreground mx-auto mb-2" />
                    <p className="text-muted-foreground">No results found</p>
                  </div>
                ) : (
                  searchResults.map((result, index) => (
                    <Card key={index} className="hover:shadow-md transition-shadow">
                      <CardContent className="p-4">
                        <div className="flex flex-col gap-3 sm:flex-row sm:items-start">
                          <div className="p-2 bg-blue-100 rounded">
                            <FileText className="h-4 w-4 text-blue-600" />
                          </div>
                          <div className="flex-1">
                            <h4 className="font-medium mb-1">{result.title}</h4>
                            <p className="text-sm text-muted-foreground mb-2 line-clamp-2">
                              {result.snippet}
                            </p>
                            <div className="flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
                              <span className="flex items-center gap-1">
                                <Calendar className="h-3 w-3" />
                                {result.date}
                              </span>
                              <span className="flex items-center gap-1">
                                <Tag className="h-3 w-3" />
                                {result.type}
                              </span>
                              <span>{(result.size / 1024 / 1024).toFixed(2)} MB</span>
                            </div>
                          </div>
                        </div>
                      </CardContent>
                    </Card>
                  ))
                )}
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

export default SearchPage;
