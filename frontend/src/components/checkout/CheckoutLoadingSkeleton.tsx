import React from 'react';

export const CheckoutLoadingSkeleton: React.FC = () => {
  return (
    <div className="w-full max-w-[480px] mx-auto p-6 bg-white border border-dp-line rounded-card shadow-card animate-pulse space-y-6">
      <div className="flex items-center justify-between pb-2 border-b border-dp-line">
        <div className="h-6 w-24 bg-dp-tint rounded-pill" />
        <div className="h-6 w-16 bg-dp-tint rounded-pill" />
      </div>

      <div className="p-4 bg-dp-tint rounded-card flex items-center justify-between">
        <div className="space-y-2">
          <div className="h-3 w-20 bg-dp-line rounded" />
          <div className="h-4 w-36 bg-dp-line rounded" />
        </div>
        <div className="h-7 w-28 bg-dp-line rounded" />
      </div>

      <div className="h-10 w-full bg-dp-tint rounded-pill" />

      <div className="h-48 w-full bg-dp-tint rounded-card" />

      <div className="h-12 w-full bg-dp-line rounded-pill" />
    </div>
  );
};
