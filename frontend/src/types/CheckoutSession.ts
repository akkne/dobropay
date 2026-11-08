import { PaymentMethodType } from './PaymentMethodType';
import { PaymentStatus } from './PaymentStatus';

export interface CheckoutSession {
  token: string;
  paymentId: string;
  shopId: string;
  shopName: string;
  orderId: string;
  amount: number;
  currency: string;
  status: PaymentStatus;
  paymentMethodType: PaymentMethodType;
  capture: boolean;
  expiresAt: string;
  createdAt: string;
  description?: string;
}
