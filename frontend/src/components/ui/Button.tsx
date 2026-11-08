import React from 'react';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
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
  const sizeClass = size === 'sm' ? 'sm' : size === 'lg' ? 'lg' : '';
  const fullClass = full ? 'full' : '';

  return (
    <button
      className={`button ${variant} ${sizeClass} ${fullClass} ${className}`.trim()}
      {...props}
    >
      {children}
    </button>
  );
};
