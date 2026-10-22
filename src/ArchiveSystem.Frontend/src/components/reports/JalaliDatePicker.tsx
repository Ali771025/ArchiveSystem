import { JalaliDateInput } from '@/components/common/JalaliDateInputs';

interface JalaliDatePickerProps {
    label: string;
    value: string;
    onChange: (value: string) => void;
    inputClassName?: string;
    className?: string;
}

const defaultInputClassName =
    'jalali-date-real-input w-full h-10 rounded-md border border-gray-300 px-3 text-sm outline-none transition bg-white text-slate-700 text-right';

export const JalaliDatePicker = ({
    label,
    value,
    onChange,
    inputClassName,
    className,
}: JalaliDatePickerProps) => {
    return (
        <div className={`jalali-date-field w-full min-w-0 ${className ?? ''}`}>
            <label className="mb-1 block text-xs font-medium text-slate-600">{label}</label>
            <JalaliDateInput
                value={value}
                onChange={onChange}
                inputClassName={inputClassName ?? defaultInputClassName}
            />
        </div>
    );
};
