import React, { useEffect, useMemo, useState } from 'react';
import { JalaliDateTimeInput } from '@/components/common/JalaliDateInputs';
import { reportsApi, type ActivityFilter, type ActivityReportItem, type ExportJob, type FilesZipExportRequest, type ReportSummary } from '@/services/reportsApi';
import { toIsoUtcString } from '@/utils/jalaliDate';

const ACTIVE_JOB_STATUSES = new Set(['pending', 'running']);

const normalizeJobStatus = (status: string | number): string => {
  const raw = String(status).toLowerCase();
  switch (raw) {
    case '1':
      return 'pending';
    case '2':
      return 'running';
    case '3':
      return 'completed';
    case '4':
      return 'failed';
    case '5':
      return 'expired';
    default:
      return raw;
  }
};

const normalizeJobType = (type: string | number): string => {
  const raw = String(type).toLowerCase();
  switch (raw) {
    case '1':
      return 'activityExcel';
    case '2':
      return 'filesZip';
    default:
      return String(type);
  }
};

const toNumber = (value: string): number | undefined => {
  if (!value.trim()) {
    return undefined;
  }

  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : undefined;
};

const formatBytes = (bytes: number): string => {
  if (bytes <= 0) {
    return '0 B';
  }

  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  let size = bytes;
  let index = 0;
  while (size >= 1024 && index < units.length - 1) {
    size /= 1024;
    index++;
  }

  return `${size.toFixed(index === 0 ? 0 : 2)} ${units[index]}`;
};

export const ActivityReports: React.FC = () => {
  const [fromUtc, setFromUtc] = useState('');
  const [toUtc, setToUtc] = useState('');
  const [actorUserId, setActorUserId] = useState('');
  const [actionType, setActionType] = useState('');
  const [classificationId, setClassificationId] = useState('');
  const [caseId, setCaseId] = useState('');
  const [fileId, setFileId] = useState('');
  const [tagId, setTagId] = useState('');
  const [term, setTerm] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const [items, setItems] = useState<ActivityReportItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [summary, setSummary] = useState<ReportSummary | null>(null);
  const [jobs, setJobs] = useState<ExportJob[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const filter = useMemo<ActivityFilter>(
    () => ({
      fromUtc: toIsoUtcString(fromUtc),
      toUtc: toIsoUtcString(toUtc),
      actorUserId: toNumber(actorUserId),
      actionType: actionType || undefined,
      classificationId: toNumber(classificationId),
      caseId: toNumber(caseId),
      fileId: toNumber(fileId),
      tagId: toNumber(tagId),
      page,
      pageSize,
      sortBy: 'timestamp',
      sortDescending: true
    }),
    [fromUtc, toUtc, actorUserId, actionType, classificationId, caseId, fileId, tagId, page, pageSize]
  );

  const zipRequest = useMemo<FilesZipExportRequest>(
    () => ({
      term: term || undefined,
      fromUtc: filter.fromUtc,
      toUtc: filter.toUtc,
      classificationId: filter.classificationId,
      caseId: filter.caseId,
      fileId: filter.fileId,
      tagId: filter.tagId
    }),
    [term, filter]
  );

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  const loadReport = async () => {
    setLoading(true);
    setError('');
    try {
      const [activity, summaryResult] = await Promise.all([
        reportsApi.getActivity(filter),
        reportsApi.getSummary(filter)
      ]);
      setItems(activity.items);
      setTotalCount(activity.totalCount);
      setSummary(summaryResult);
    } catch (reportError) {
      console.error(reportError);
      setError('Failed to load activity report.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadReport().catch((err) => {
      console.error(err);
      setError('Failed to load activity report.');
    });  }, [filter]);

  useEffect(() => {
    const activeJobs = jobs.filter((job) => ACTIVE_JOB_STATUSES.has(normalizeJobStatus(job.status)));
    if (activeJobs.length === 0) {
      return undefined;
    }

    const timer = setInterval(() => {
      Promise.all(activeJobs.map((job) => reportsApi.getJobStatus(job.id)))
        .then((updated) => {
          setJobs((current) => current.map((job) => updated.find((x) => x.id === job.id) ?? job));
        })
        .catch((pollError) => {
          console.error('Failed to refresh job status', pollError);
        });
    }, 3000);

    return () => clearInterval(timer);
  }, [jobs]);

  const createActivityExport = async () => {
    try {
      const job = await reportsApi.createActivityExcelExport(filter);
      setJobs((current) => [job, ...current.filter((x) => x.id !== job.id)]);
    } catch (exportError) {
      console.error(exportError);
      setError('Failed to create activity Excel export job.');
    }
  };

  const createZipExport = async () => {
    try {
      const job = await reportsApi.createFilesZipExport(zipRequest);
      setJobs((current) => [job, ...current.filter((x) => x.id !== job.id)]);
    } catch (exportError) {
      console.error(exportError);
      setError('Failed to create files ZIP export job.');
    }
  };

  const downloadJob = async (jobId: string) => {
    try {
      const result = await reportsApi.downloadJob(jobId);
      const anchor = document.createElement('a');
      anchor.href = result.url;
      anchor.download = result.fileName;
      document.body.append(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(result.url);
    } catch (downloadError) {
      console.error(downloadError);
      setError('Failed to download export result.');
    }
  };

  return (
    <section className="rounded border p-4 space-y-4">
      <h2 className="text-xl font-semibold">Activity Report and Exports</h2>

      <div className="grid gap-2 md:grid-cols-5">
        <JalaliDateTimeInput value={fromUtc} onChange={setFromUtc} inputClassName="border rounded px-2 py-1 w-full text-sm" />
        <JalaliDateTimeInput value={toUtc} onChange={setToUtc} inputClassName="border rounded px-2 py-1 w-full text-sm" />
        <input type="text" className="border rounded px-2 py-1" placeholder="Actor user id" value={actorUserId} onChange={(event) => setActorUserId(event.target.value)} />
        <input type="text" className="border rounded px-2 py-1" placeholder="Action type" value={actionType} onChange={(event) => setActionType(event.target.value)} />
        <input type="text" className="border rounded px-2 py-1" placeholder="Term for ZIP export" value={term} onChange={(event) => setTerm(event.target.value)} />
        <input type="text" className="border rounded px-2 py-1" placeholder="Classification id" value={classificationId} onChange={(event) => setClassificationId(event.target.value)} />
        <input type="text" className="border rounded px-2 py-1" placeholder="Case id" value={caseId} onChange={(event) => setCaseId(event.target.value)} />
        <input type="text" className="border rounded px-2 py-1" placeholder="File id" value={fileId} onChange={(event) => setFileId(event.target.value)} />
        <input type="text" className="border rounded px-2 py-1" placeholder="Tag id" value={tagId} onChange={(event) => setTagId(event.target.value)} />
        <div className="flex gap-2">
          <button className="px-3 py-2 border rounded w-full" onClick={createActivityExport}>
            Export Excel
          </button>
          <button className="px-3 py-2 border rounded w-full" onClick={createZipExport}>
            Export Zip
          </button>
        </div>
      </div>

      {summary && (
        <div className="grid gap-2 md:grid-cols-3 text-sm">
          <div className="rounded border p-3">
            <p className="font-medium">Total Files</p>
            <p>{summary.totalFiles}</p>
          </div>
          <div className="rounded border p-3">
            <p className="font-medium">Storage</p>
            <p>{formatBytes(summary.totalStorageBytes)}</p>
          </div>
          <div className="rounded border p-3">
            <p className="font-medium">Top Action</p>
            <p>{summary.countsByActionType[0]?.actionType ?? 'N/A'}</p>
          </div>
        </div>
      )}

      {error && <p className="text-sm text-red-700">{error}</p>}
      {loading && <p className="text-sm text-gray-600">Loading report...</p>}

      <div className="overflow-x-auto">
        <table className="min-w-full border text-sm">
          <thead>
            <tr>
              <th className="border px-2 py-1 text-right">Timestamp</th>
              <th className="border px-2 py-1 text-right">Actor</th>
              <th className="border px-2 py-1 text-right">Action</th>
              <th className="border px-2 py-1 text-right">Entity</th>
              <th className="border px-2 py-1 text-right">Classification</th>
              <th className="border px-2 py-1 text-right">Case</th>
              <th className="border px-2 py-1 text-right">File</th>
              <th className="border px-2 py-1 text-right">Tag</th>
            </tr>
          </thead>
          <tbody>
            {items.map((item) => (
              <tr key={item.id}>
                <td className="border px-2 py-1">{new Date(item.timestamp).toLocaleString()}</td>
                <td className="border px-2 py-1">{item.actorName ?? item.actorUserId}</td>
                <td className="border px-2 py-1">{item.actionType}</td>
                <td className="border px-2 py-1">
                  {item.entityType}:{item.entityId}
                </td>
                <td className="border px-2 py-1">{item.classificationId ?? '-'}</td>
                <td className="border px-2 py-1">{item.caseId ?? '-'}</td>
                <td className="border px-2 py-1">{item.fileId ?? '-'}</td>
                <td className="border px-2 py-1">{item.tagId ?? '-'}</td>
              </tr>
            ))}
            {items.length === 0 && (
              <tr>
                <td className="border px-2 py-3 text-center text-gray-500" colSpan={8}>
                  No activity found for the selected filters.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="flex items-center justify-between text-sm">
        <div>
          Page {page} of {totalPages} ({totalCount} rows)
        </div>
        <div className="flex items-center gap-2">
          <select
            className="border rounded px-2 py-1"
            value={pageSize}
            onChange={(event) => {
              setPage(1);
              setPageSize(Number(event.target.value));
            }}
          >
            {[20, 50, 100].map((size) => (
              <option key={size} value={size}>
                {size} / page
              </option>
            ))}
          </select>
          <button className="border rounded px-2 py-1" disabled={page <= 1} onClick={() => setPage((current) => Math.max(1, current - 1))}>
            Prev
          </button>
          <button className="border rounded px-2 py-1" disabled={page >= totalPages} onClick={() => setPage((current) => Math.min(totalPages, current + 1))}>
            Next
          </button>
        </div>
      </div>

      <div className="space-y-2">
        <h3 className="font-semibold">Export Jobs</h3>
        <div className="overflow-x-auto">
          <table className="min-w-full border text-sm">
            <thead>
              <tr>
                <th className="border px-2 py-1 text-right">Job Id</th>
                <th className="border px-2 py-1 text-right">Type</th>
                <th className="border px-2 py-1 text-right">Status</th>
                <th className="border px-2 py-1 text-right">Progress</th>
                <th className="border px-2 py-1 text-right">Action</th>
              </tr>
            </thead>
            <tbody>
              {jobs.map((job) => (
                <tr key={job.id}>
                  <td className="border px-2 py-1">{job.id}</td>
                  <td className="border px-2 py-1">{normalizeJobType(job.type)}</td>
                  <td className="border px-2 py-1">{normalizeJobStatus(job.status)}</td>
                  <td className="border px-2 py-1">{job.progressPercent ?? '-'}</td>
                  <td className="border px-2 py-1">
                    {normalizeJobStatus(job.status) === 'completed' ? (
                      <button className="border rounded px-2 py-1" onClick={() => downloadJob(job.id)}>
                        Download
                      </button>
                    ) : (
                      <span className="text-gray-500">{job.errorMessage ?? '-'}</span>
                    )}
                  </td>
                </tr>
              ))}
              {jobs.length === 0 && (
                <tr>
                  <td className="border px-2 py-3 text-center text-gray-500" colSpan={5}>
                    No export jobs yet.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
};

export default ActivityReports;

