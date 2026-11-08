import React from 'react';

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'primary-blue' | 'outline' | 'outline-light' | 'ghost';
  size?: 'sm' | 'md' | 'lg';
  full?: boolean;
  children: React.ReactNode;
}

export const Button: React.FC<ButtonProps> = ({
  variant = 'primary',
  size = 'md',
  full = false,
  className = '',
  children,
  ...props
}) => {
  const variantStyles = {
    'primary': 'bg-dp-pink text-dp-blue shadow-sm hover:brightness-105',
    'primary-blue': 'bg-dp-blue text-white shadow-sm hover:bg-[#07246b]',
    'outline': 'bg-transparent border border-dp-line text-dp-blue hover:bg-dp-tint',
    'outline-light': 'bg-transparent border border-white/30 text-white hover:bg-white/10',
    'ghost': 'bg-transparent border-transparent text-dp-blue hover:bg-dp-tint'
  }[variant];

  const sizeStyles = {
    sm: 'min-h-[30px] px-3 py-1 text-xs',
    md: 'min-h-[36px] px-4.5 py-2 text-sm',
    lg: 'min-h-[46px] px-6 py-3 text-base font-semibold'
  }[size];

  const widthStyle = full ? 'w-full' : '';

  return (
    <button
      className={`inline-flex items-center justify-center gap-2 rounded-pill font-sans leading-none whitespace-nowrap cursor-pointer transition-all active:scale-[0.98] select-none disabled:opacity-50 disabled:pointer-events-none ${variantStyles} ${sizeStyles} ${widthStyle} ${className}`.trim()}
      {...props}
    >
      {children}
    </button>
  );
};
