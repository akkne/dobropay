import React from 'react';
import { Brand } from '../ui/Brand';

export const CheckoutBrand: React.FC = () => {
  return (
    <div className="flex items-center gap-2">
      <Brand size={22} />
    </div>
  );
};
