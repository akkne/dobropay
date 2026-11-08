import { PaymentStatus } from './PaymentStatus';
import { PaymentMethodType } from './PaymentMethodType';
import { Refund } from './Refund';

export interface Payment {
  id: string;
  shopId?: string;
  orderId?: string;
  externalOrderId: string;
  amount: number;
  currency: string;
  status: PaymentStatus;
  paymentMethodType: PaymentMethodType;
  capture?: boolean;
  checkoutUrl?: string;
  description?: string;
  createdAt: string;
  updatedAt: string;
  capturedAmount?: number;
  refundedAmount?: number;
  cancellationReason?: string;
  cardLast4?: string;
  cardBrand?: string;
  refunds: Refund[];
}
