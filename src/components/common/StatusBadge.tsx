interface StatusBadgeProps {
  status: string;
  label?: string;
  color?: 'blue' | 'green' | 'yellow' | 'red' | 'purple' | 'gray' | 'cyan' | 'teal' | 'orange' | 'emerald';
  size?: 'sm' | 'md';
}

const StatusBadge = ({
  status,
  label,
  color = 'blue',
  size = 'md',
}: StatusBadgeProps) => {
  const colors = {
    blue: 'bg-blue-100 text-blue-700 border-blue-200',
    green: 'bg-green-100 text-green-700 border-green-200',
    yellow: 'bg-yellow-100 text-yellow-700 border-yellow-200',
    red: 'bg-red-100 text-red-700 border-red-200',
    purple: 'bg-purple-100 text-purple-700 border-purple-200',
    gray: 'bg-gray-100 text-gray-700 border-gray-200',
    cyan: 'bg-cyan-100 text-cyan-700 border-cyan-200',
    teal: 'bg-teal-100 text-teal-700 border-teal-200',
    orange: 'bg-orange-100 text-orange-700 border-orange-200',
    emerald: 'bg-emerald-100 text-emerald-700 border-emerald-200',
  };

  const sizes = {
    sm: 'px-2 py-0.5 text-xs',
    md: 'px-2.5 py-1 text-xs',
  };

  return (
    <span
      className={`inline-flex items-center font-medium rounded-full border ${colors[color]} ${sizes[size]}`}
    >
      {label || status}
    </span>
  );
};

export default StatusBadge;
