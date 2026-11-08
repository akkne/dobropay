import React from 'react';

export interface BrandProps {
  className?: string;
  size?: number;
  light?: boolean;
}

export const Brand: React.FC<BrandProps> = ({ className = '', size = 27, light = false }) => {
  return (
    <div
      className={`inline-flex items-center gap-[0.34em] font-[730] leading-none tracking-[-0.065em] whitespace-nowrap select-none ${
        light ? 'text-white' : 'text-dp-blue'
      } ${className}`}
      style={{ fontSize: `${size}px` }}
    >
      <span
        className="inline-block w-[0.68em] height-[0.68em] h-[0.68em] bg-dp-pink rounded-tl-[55%] rounded-tr-[55%] rounded-br-[55%] rounded-bl-[12%] -rotate-12 flex-shrink-0"
      />
      <span>dobropay</span>
    </div>
  );
};
