import React, { useEffect } from 'react';

export interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  maxWidth?: string;
  children: React.ReactNode;
}

export const Modal: React.FC<ModalProps> = ({
  isOpen,
  onClose,
  maxWidth = '520px',
  children
}) => {
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 backdrop-blur-sm"
      onClick={onClose}
    >
      <div
        className="relative w-full bg-white rounded-card shadow-popover border border-dp-line p-6 md:p-8 max-h-[92vh] overflow-y-auto"
        style={{ maxWidth }}
        onClick={(e) => e.stopPropagation()}
      >
        <button
          type="button"
          className="absolute top-4 right-4 w-8 h-8 rounded-full flex items-center justify-center text-2xl leading-none text-dp-blue/60 hover:text-dp-blue hover:bg-dp-tint transition-colors cursor-pointer"
          onClick={onClose}
          aria-label="Закрыть"
        >
          &times;
        </button>
        {children}
      </div>
    </div>
  );
};
