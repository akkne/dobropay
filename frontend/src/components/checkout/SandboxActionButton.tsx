import React from 'react';

export interface SandboxActionButtonProps {
  label: string;
  onClick: () => void;
  variant?: 'success' | 'warning' | 'danger' | 'neutral';
}

export const SandboxActionButton: React.FC<SandboxActionButtonProps> = ({
  label,
  onClick,
  variant = 'neutral'
}) => {
  const variantStyles = {
    success: 'bg-dp-green-blush text-dp-green hover:bg-emerald-100',
    warning: 'bg-dp-amber-blush text-dp-amber hover:bg-amber-100',
    danger: 'bg-dp-red-blush text-dp-red hover:bg-red-100',
    neutral: 'bg-dp-tint text-dp-blue hover:bg-dp-blush/40'
  }[variant];

  return (
    <button
      type="button"
      onClick={onClick}
      className={`px-3 py-1.5 rounded-pill text-[11px] font-bold tracking-tight transition-all cursor-pointer border border-transparent select-none active:scale-95 ${variantStyles}`}
    >
      {label}
    </button>
  );
};
