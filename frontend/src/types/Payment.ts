import { PaymentStatus } from './PaymentStatus';
import { PaymentMethodType } from './PaymentMethodType';
import { Refund } from './Refund';

export interface Payment {
  id: string;
  shopId?: string;
  externalOrderId: string;
  amount: number;
  currency: string;
  status: PaymentStatus;
  paymentMethodType: PaymentMethodType;
  createdAt: string;
  updatedAt: string;
  capturedAmount?: number;
  refundedAmount?: number;
  cancellationReason?: string;
  cardLast4?: string;
  cardBrand?: string;
  cryptoNetwork?: string;
  cryptoAmount?: number;
  depositAddress?: string;
  confirmations?: number;
  requiredConfirmations?: number;
  refunds: Refund[];
}
