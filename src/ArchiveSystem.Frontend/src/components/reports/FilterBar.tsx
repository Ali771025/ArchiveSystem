import { JalaliDatePicker } from '@/components/reports/JalaliDatePicker';

interface FilterBarFilters {
  fromDate: string;
  toDate: string;
  user: string;
  action: string;
}

interface FilterBarProps {
  filters: FilterBarFilters;
  onChange: (patch: Partial<FilterBarFilters>) => void;
  onSubmit: () => void;
  onReset: () => void;
}

const baseInputClass =
  'w-full h-10 rounded-md border border-gray-300 px-3 text-sm outline-none transition focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20';

export const FilterBar = ({ filters, onChange, onSubmit, onReset }: FilterBarProps) => {
  return (
    <section dir="rtl" className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <div className="mb-3 flex items-center justify-between">
        <h2 className="text-sm font-semibold text-slate-800">فیلتر گزارش‌ها</h2>
        <button type="button" onClick={onReset} className="text-sm font-medium text-blue-600 hover:text-blue-700">
          پاک‌سازی
        </button>
      </div>

      <div className="grid items-end gap-3 md:grid-cols-5">
        <JalaliDatePicker
          label="از تاریخ"
          value={filters.fromDate}
          onChange={(value) => onChange({ fromDate: value })}
          inputClassName={baseInputClass}
        />
        <JalaliDatePicker
          label="تا تاریخ"
          value={filters.toDate}
          onChange={(value) => onChange({ toDate: value })}
          inputClassName={baseInputClass}
        />

        <div>
          <label htmlFor="filter-user" className="mb-1 block text-xs font-medium text-slate-600">
            کاربر
          </label>
          <input
            id="filter-user"
            value={filters.user}
            onChange={(event) => onChange({ user: event.target.value })}
            className={baseInputClass}
          />
        </div>

        <div>
          <label htmlFor="filter-action" className="mb-1 block text-xs font-medium text-slate-600">
            نوع عملیات
          </label>
          <select
            id="filter-action"
            value={filters.action}
            onChange={(event) => onChange({ action: event.target.value })}
            className={baseInputClass}
          >
            <option value="">همه عملیات</option>
            <option value="Create">ایجاد</option>
            <option value="Update">ویرایش</option>
            <option value="Delete">حذف</option>
          </select>
        </div>

        <button
          type="button"
          onClick={onSubmit}
          className="h-10 rounded-md bg-blue-600 px-4 text-sm font-semibold text-white transition hover:bg-blue-700"
        >
          اعمال فیلتر
        </button>
      </div>
    </section>
  );
};
