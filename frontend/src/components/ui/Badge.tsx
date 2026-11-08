import React from 'react';

export interface BadgeProps {
  variant?: 'succeeded' | 'paid' | 'pending' | 'waiting' | 'canceled' | 'failed' | 'neutral';
  className?: string;
  style?: React.CSSProperties;
  children: React.ReactNode;
}

export const Badge: React.FC<BadgeProps> = ({ variant = 'neutral', className = '', style, children }) => {
  const variantStyles = {
    succeeded: 'bg-dp-green-blush text-dp-green border-dp-green/20',
    paid: 'bg-dp-green-blush text-dp-green border-dp-green/20',
    pending: 'bg-dp-tint text-dp-blue border-dp-line',
    waiting: 'bg-dp-amber-blush text-dp-amber border-amber-500/30',
    canceled: 'bg-dp-red-blush text-dp-red border-red-500/30',
    failed: 'bg-dp-red-blush text-dp-red border-red-500/30',
    neutral: 'bg-dp-tint text-dp-blue border-dp-line'
  }[variant];

  return (
    <span
      className={`inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-pill text-xs font-semibold whitespace-nowrap border ${variantStyles} ${className}`.trim()}
      style={style}
    >
      {children}
    </span>
  );
};
