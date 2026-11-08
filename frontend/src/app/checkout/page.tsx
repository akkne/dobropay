'use client';

import React, { Suspense } from 'react';
import CheckoutPage from './[token]/page';

export default function DefaultCheckout() {
  return (
    <Suspense fallback={<div className="p-8 text-center text-slate-500">Загрузка формы оплаты...</div>}>
      <CheckoutPage />
    </Suspense>
  );
}
