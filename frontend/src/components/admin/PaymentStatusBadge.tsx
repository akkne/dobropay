import React from 'react';
import { PaymentStatus } from '../../types/PaymentStatus';
import { Badge } from '../ui/Badge';

export interface PaymentStatusBadgeProps {
  status: PaymentStatus;
}

export const PaymentStatusBadge: React.FC<PaymentStatusBadgeProps> = ({ status }) => {
  switch (status) {
    case 'succeeded':
      return <Badge variant="succeeded">Успешно</Badge>;
    case 'waiting_for_capture':
      return <Badge variant="waiting">В холде</Badge>;
    case 'pending':
      return <Badge variant="pending">Ожидает</Badge>;
    case 'canceled':
      return <Badge variant="canceled">Отменен</Badge>;
    default:
      return <Badge variant="neutral">{status}</Badge>;
  }
};
