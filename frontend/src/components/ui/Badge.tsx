import React from 'react';

interface BadgeProps {
  variant?: 'succeeded' | 'paid' | 'pending' | 'waiting' | 'canceled' | 'failed' | 'neutral';
  className?: string;
  style?: React.CSSProperties;
  children: React.ReactNode;
}

export const Badge: React.FC<BadgeProps> = ({ variant = 'neutral', className = '', style, children }) => {
  return (
    <span className={`badge ${variant} ${className}`.trim()} style={style}>
      {children}
    </span>
  );
};
