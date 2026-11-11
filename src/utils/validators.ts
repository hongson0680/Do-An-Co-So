// Form validation utilities

export const validators = {
  // Required field
  required: (message = 'Trường này là bắt buộc') => ({
    required: message,
  }),

  // Email validation
  email: (message = 'Email không hợp lệ') => ({
    pattern: {
      value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
      message,
    },
  }),

  // Phone number (Vietnamese)
  phone: (message = 'Số điện thoại không hợp lệ') => ({
    pattern: {
      value: /^(0[0-9]{9})$/,
      message,
    },
  }),

  // CCCD (9 or 12 digits)
  cccd: (message = 'Số CCCD không hợp lệ') => ({
    pattern: {
      value: /^[0-9]{9}$|^[0-9]{12}$/,
      message,
    },
  }),

  // Positive number
  positiveNumber: (message = 'Giá trị phải lớn hơn 0') => ({
    min: {
      value: 0.01,
      message,
    },
  }),

  // Min length
  minLength: (min: number, message?: string) => ({
    minLength: {
      value: min,
      message: message || `Tối thiểu ${min} ký tự`,
    },
  }),

  // Max length
  maxLength: (max: number, message?: string) => ({
    maxLength: {
      value: max,
      message: message || `Tối đa ${max} ký tự`,
    },
  }),
};

// Validation helper
export const validateForm = (
  values: Record<string, unknown>,
  rules: Record<string, unknown>
): Record<string, string> => {
  const errors: Record<string, string> = {};

  Object.keys(rules).forEach((field) => {
    const value = values[field];
    const fieldRules = rules[field] as Record<string, { value?: unknown; message?: string }>;

    if (fieldRules.required && (!value || (typeof value === 'string' && !value.trim()))) {
      errors[field] = fieldRules.required.message || 'Trường này là bắt buộc';
    }

    if (fieldRules.pattern && value) {
      const pattern = fieldRules.pattern.value as RegExp;
      if (!pattern.test(String(value))) {
        errors[field] = fieldRules.pattern.message || 'Giá trị không hợp lệ';
      }
    }

    if (fieldRules.minLength && typeof value === 'string') {
      if (value.length < (fieldRules.minLength.value as number)) {
        errors[field] = fieldRules.minLength.message || `Tối thiểu ${fieldRules.minLength.value} ký tự`;
      }
    }

    if (fieldRules.maxLength && typeof value === 'string') {
      if (value.length > (fieldRules.maxLength.value as number)) {
        errors[field] = fieldRules.maxLength.message || `Tối đa ${fieldRules.maxLength.value} ký tự`;
      }
    }
  });

  return errors;
};
