import React from 'react';

interface BrandProps {
  className?: string;
  size?: number;
  light?: boolean;
}

export const Brand: React.FC<BrandProps> = ({ className = '', size = 27, light = false }) => {
  return (
    <div
      className={`brand ${light ? 'light' : ''} ${className}`}
      style={{ fontSize: `${size}px` }}
    >
      <span className="brand-mark" />
      <span>dobropay</span>
    </div>
  );
};
