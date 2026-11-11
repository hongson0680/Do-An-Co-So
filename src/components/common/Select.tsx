import { SelectHTMLAttributes, forwardRef, useState, useRef, useEffect } from 'react';
import { ChevronDown, Check } from 'lucide-react';

interface SelectOption {
  value: string | number;
  label: string;
}

interface SelectProps extends Omit<SelectHTMLAttributes<HTMLSelectElement>, 'onChange'> {
  label?: string;
  error?: string;
  helperText?: string;
  options: SelectOption[];
  placeholder?: string;
  onChange?: (value: string | number) => void;
}

const Select = forwardRef<HTMLSelectElement, SelectProps>(
  (
    { label, error, helperText, options, placeholder, className = '', id, value, onChange },
    ref
  ) => {
    const [isOpen, setIsOpen] = useState(false);
    const selectId = id || label?.toLowerCase().replace(/\s+/g, '-');
    const dropdownRef = useRef<HTMLDivElement>(null);

    // Helper to check if values match (handles number vs string comparison)
    const isSelected = (optValue: string | number) => {
      if (typeof value === 'number') {
        return Number(optValue) === value;
      }
      return String(optValue) === String(value);
    };

    const selectedOption = options.find(opt => isSelected(opt.value));
    const currentValue = value ?? '';

    useEffect(() => {
      const handleClickOutside = (event: MouseEvent) => {
        if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
          setIsOpen(false);
        }
      };
      document.addEventListener('mousedown', handleClickOutside);
      return () => document.removeEventListener('mousedown', handleClickOutside);
    }, []);

    const handleSelect = (option: SelectOption) => {
      onChange?.(option.value);
      setIsOpen(false);
    };

    return (
      <div className="w-full" ref={dropdownRef}>
        {label && (
          <label
            htmlFor={selectId}
            className="block text-sm font-medium text-gray-700 mb-1"
          >
            {label}
          </label>
        )}
        <div className="relative">
          <button
            type="button"
            id={selectId}
            onClick={() => setIsOpen(!isOpen)}
            className={`
              flex items-center justify-between w-full rounded-lg border bg-white px-3 py-2 text-left
              focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
              disabled:bg-gray-100 disabled:cursor-not-allowed
              ${error ? 'border-red-500 focus:ring-red-500' : 'border-gray-300'}
              ${className}
            `}
          >
            <span className={selectedOption ? 'text-gray-900' : 'text-gray-400'}>
              {selectedOption?.label || placeholder || 'Chọn...'}
            </span>
            <ChevronDown
              className={`w-4 h-4 text-gray-400 transition-transform ${isOpen ? 'rotate-180' : ''}`}
            />
          </button>

          {isOpen && (
            <div className="absolute z-50 w-full mt-1 bg-white border border-gray-200 rounded-lg shadow-lg max-h-60 overflow-auto">
              {options.map((option) => (
                <button
                  key={option.value}
                  type="button"
                  onClick={() => handleSelect(option)}
                  className={`
                    flex items-center justify-between w-full px-3 py-2 text-left hover:bg-gray-50
                    ${isSelected(option.value) ? 'bg-blue-50 text-blue-700' : 'text-gray-900'}
                  `}
                >
                  <span>{option.label}</span>
                  {isSelected(option.value) && <Check className="w-4 h-4 text-blue-600" />}
                </button>
              ))}
            </div>
          )}
        </div>
        {error && <p className="mt-1 text-sm text-red-500">{error}</p>}
        {helperText && !error && (
          <p className="mt-1 text-sm text-gray-500">{helperText}</p>
        )}

        {/* Hidden native select for form submission */}
        <select
          ref={ref}
          value={currentValue}
          onChange={(e) => {
            const rawValue = e.target.value;
            const parsedValue = Number(rawValue);
            onChange?.(!Number.isNaN(parsedValue) && rawValue.trim() !== '' ? parsedValue : rawValue);
          }}
          className="hidden"
          aria-label={label}
        >
          {placeholder && (
            <option value="" disabled>
              {placeholder}
            </option>
          )}
          {options.map((option) => (
            <option key={`${option.value}-${option.label}`} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </div>
    );
  }
);

Select.displayName = 'Select';

export default Select;
