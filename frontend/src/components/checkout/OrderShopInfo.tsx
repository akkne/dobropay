import React from 'react';

export interface OrderShopInfoProps {
  shopName: string;
}

export const OrderShopInfo: React.FC<OrderShopInfoProps> = ({ shopName }) => {
  return (
    <div className="flex items-center gap-3">
      <div className="w-10 h-10 rounded-full bg-dp-blush flex items-center justify-center text-dp-blue font-bold text-sm flex-shrink-0">
        {shopName ? shopName.charAt(0).toUpperCase() : 'M'}
      </div>
      <div>
        <div className="text-xs text-dp-blue/60 font-medium">Получатель платежа</div>
        <div className="text-sm font-bold text-dp-blue flex items-center gap-1.5">
          <span>{shopName}</span>
          <svg className="w-4 h-4 text-dp-green inline-block flex-shrink-0" viewBox="0 0 24 24" fill="currentColor">
            <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z" />
          </svg>
        </div>
      </div>
    </div>
  );
};
