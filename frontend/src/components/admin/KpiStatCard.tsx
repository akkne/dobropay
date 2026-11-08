import React from 'react';

export interface KpiStatCardProps {
  title: string;
  value: string;
  subtitle: string;
  trend?: string;
  variant?: 'blue' | 'pink' | 'green' | 'amber';
}

export const KpiStatCard: React.FC<KpiStatCardProps> = ({
  title,
  value,
  subtitle,
  trend,
  variant = 'blue'
}) => {
  const borderStyles = {
    blue: 'border-l-4 border-l-dp-blue',
    pink: 'border-l-4 border-l-dp-pink',
    green: 'border-l-4 border-l-dp-green',
    amber: 'border-l-4 border-l-dp-amber'
  }[variant];

  return (
    <div className={`bg-white border border-dp-line rounded-card p-5 shadow-sm space-y-1.5 transition-transform hover:-translate-y-0.5 ${borderStyles}`}>
      <div className="flex items-center justify-between">
        <span className="text-xs font-semibold text-dp-blue/70">{title}</span>
        {trend && (
          <span className="text-[11px] font-bold text-dp-green bg-dp-green-blush px-2 py-0.5 rounded-full">
            {trend}
          </span>
        )}
      </div>
      <div className="text-2xl font-bold font-mono tracking-tight text-dp-blue">
        {value}
      </div>
      <div className="text-[11px] text-dp-blue/50 font-medium">
        {subtitle}
      </div>
    </div>
  );
};
