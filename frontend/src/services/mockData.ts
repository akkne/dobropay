import { Shop } from '../types/Shop';
import { Payment } from '../types/Payment';
import { WebhookDelivery } from '../types/WebhookDelivery';
import { FiscalReceipt } from '../types/FiscalReceipt';
import { ApiKey } from '../types/ApiKey';

export const initialMockShop: Shop = {
  id: '11111111-1111-4111-8111-111111111111',
  name: 'Магазин мерча Dobropay',
  status: 'active',
  inn: '7707083893',
  ogrn: '1027700132195',
  legalName: 'ООО «Добропей Диджитал»',
  checkingAccount: '40702810938000012345',
  bik: '044525225',
  website: 'https://dobropay-merch.ru',
  adminEmail: 'demo@dobropay.local'
};

export const initialMockPayments: Payment[] = [
  {
    id: 'd9b3a5b0-8fc2-43bb-a5a1-778899aabbcc',
    externalOrderId: 'order-1042',
    amount: 129900,
    currency: 'RUB',
    status: 'succeeded',
    paymentMethodType: 'sbp',
    createdAt: new Date(Date.now() - 1000 * 60 * 18).toISOString(),
    updatedAt: new Date(Date.now() - 1000 * 60 * 16).toISOString(),
    capturedAmount: 129900,
    refundedAmount: 0,
    refunds: []
  },
  {
    id: 'e1c4a5b0-8fc2-43bb-b5a1-998877aabbdd',
    externalOrderId: 'order-1043',
    amount: 450000,
    currency: 'RUB',
    status: 'waiting_for_capture',
    paymentMethodType: 'bank_card',
    createdAt: new Date(Date.now() - 1000 * 60 * 35).toISOString(),
    updatedAt: new Date(Date.now() - 1000 * 60 * 34).toISOString(),
    capturedAmount: 0,
    refundedAmount: 0,
    cardLast4: '4242',
    cardBrand: 'mir',
    refunds: []
  },
  {
    id: 'f2d5a5b0-8fc2-43bb-c5a1-112233aabbcc',
    externalOrderId: 'order-1044',
    amount: 89000,
    currency: 'RUB',
    status: 'pending',
    paymentMethodType: 'crypto',
    cryptoNetwork: 'USDT_TRC20',
    cryptoAmount: 9.14,
    depositAddress: 'TXjS51QoP8iBhykLw9mYfR79r42XN8yFkM',
    confirmations: 1,
    requiredConfirmations: 3,
    createdAt: new Date(Date.now() - 1000 * 60 * 5).toISOString(),
    updatedAt: new Date(Date.now() - 1000 * 60 * 4).toISOString(),
    capturedAmount: 0,
    refundedAmount: 0,
    refunds: []
  },
  {
    id: 'a3e6a5b0-8fc2-43bb-d5a1-445566aabbcc',
    externalOrderId: 'order-1040',
    amount: 320000,
    currency: 'RUB',
    status: 'succeeded',
    paymentMethodType: 'bank_card',
    createdAt: new Date(Date.now() - 1000 * 60 * 180).toISOString(),
    updatedAt: new Date(Date.now() - 1000 * 60 * 179).toISOString(),
    capturedAmount: 320000,
    refundedAmount: 100000,
    cardLast4: '5555',
    cardBrand: 'mastercard',
    refunds: [
      {
        id: 'ref-001',
        amount: 100000,
        status: 'succeeded',
        reason: 'Частичный возврат (клиент вернул 1 позицию)',
        createdAt: new Date(Date.now() - 1000 * 60 * 60).toISOString()
      }
    ]
  },
  {
    id: 'b4f7a5b0-8fc2-43bb-e5a1-778899aabbcc',
    externalOrderId: 'order-1039',
    amount: 150000,
    currency: 'RUB',
    status: 'canceled',
    cancellationReason: 'insufficient_funds',
    paymentMethodType: 'bank_card',
    createdAt: new Date(Date.now() - 1000 * 60 * 300).toISOString(),
    updatedAt: new Date(Date.now() - 1000 * 60 * 299).toISOString(),
    refunds: []
  }
];

export const initialMockApiKeys: ApiKey[] = [
  {
    id: 'k1-uuid',
    name: 'Основной рабочий сервер',
    prefix: 'dp_live_8f3a...',
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 14).toISOString(),
    status: 'active'
  },
  {
    id: 'k2-uuid',
    name: 'Тестовый ключ песочницы',
    prefix: 'dp_test_shop_a_...',
    createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 2).toISOString(),
    status: 'active'
  }
];

export const initialMockDeliveries: WebhookDelivery[] = [
  {
    id: 'del-001',
    eventType: 'payment.succeeded',
    paymentId: 'd9b3a5b0-8fc2-43bb-a5a1-778899aabbcc',
    url: 'https://demo-merchant.local/api/dobropay-webhook',
    status: 'delivered',
    statusCode: 200,
    attempts: 1,
    createdAt: new Date(Date.now() - 1000 * 60 * 16).toISOString(),
    payload: JSON.stringify({
      event_id: 'evt-901',
      event_type: 'payment.succeeded',
      occurred_at: new Date(Date.now() - 1000 * 60 * 16).toISOString(),
      payment: { id: 'd9b3a5b0-8fc2-43bb-a5a1-778899aabbcc', status: 'succeeded', amount: 129900, currency: 'RUB' }
    }, null, 2),
    signature: 'sha256=48fbc8a1c90192e...'
  },
  {
    id: 'del-002',
    eventType: 'payment.waiting_for_capture',
    paymentId: 'e1c4a5b0-8fc2-43bb-b5a1-998877aabbdd',
    url: 'https://demo-merchant.local/api/dobropay-webhook',
    status: 'delivered',
    statusCode: 200,
    attempts: 1,
    createdAt: new Date(Date.now() - 1000 * 60 * 34).toISOString(),
    payload: JSON.stringify({
      event_id: 'evt-902',
      event_type: 'payment.waiting_for_capture',
      occurred_at: new Date(Date.now() - 1000 * 60 * 34).toISOString(),
      payment: { id: 'e1c4a5b0-8fc2-43bb-b5a1-998877aabbdd', status: 'waiting_for_capture', amount: 450000, currency: 'RUB' }
    }, null, 2),
    signature: 'sha256=10f2c4a980...'
  },
  {
    id: 'del-003',
    eventType: 'payment.canceled',
    paymentId: 'b4f7a5b0-8fc2-43bb-e5a1-778899aabbcc',
    url: 'https://demo-merchant.local/api/dobropay-webhook',
    status: 'retrying',
    statusCode: 503,
    attempts: 2,
    nextRetryAt: new Date(Date.now() + 1000 * 60 * 4).toISOString(),
    createdAt: new Date(Date.now() - 1000 * 60 * 15).toISOString(),
    payload: JSON.stringify({
      event_id: 'evt-903',
      event_type: 'payment.canceled',
      payment: { id: 'b4f7a5b0-8fc2-43bb-e5a1-778899aabbcc', status: 'canceled', amount: 150000, currency: 'RUB' }
    }, null, 2),
    signature: 'sha256=99f8e7d6c...'
  }
];

export const initialMockReceipts: FiscalReceipt[] = [
  {
    id: 'rec-001',
    paymentId: 'd9b3a5b0-8fc2-43bb-a5a1-778899aabbcc',
    orderNumber: '1042',
    amount: 129900,
    fiscalStatus: 'succeeded',
    fnNumber: '9999078900012345',
    fdNumber: '4021',
    fpdNumber: '3948572019',
    customerEmail: 'customer@client.ru',
    emailStatus: 'sent',
    createdAt: new Date(Date.now() - 1000 * 60 * 16).toISOString(),
    items: [
      { name: 'Керамическая кружка Dobropay (ручная работа)', price: 129900, quantity: 1, vat: 'vat20' }
    ]
  },
  {
    id: 'rec-002',
    paymentId: 'a3e6a5b0-8fc2-43bb-d5a1-445566aabbcc',
    orderNumber: '1040',
    amount: 320000,
    fiscalStatus: 'succeeded',
    fnNumber: '9999078900012345',
    fdNumber: '4019',
    fpdNumber: '1092837465',
    customerEmail: 'artur@buyer.ru',
    emailStatus: 'sent',
    createdAt: new Date(Date.now() - 1000 * 60 * 179).toISOString(),
    items: [
      { name: 'Худи Dobropay Oversize Navy L', price: 320000, quantity: 1, vat: 'vat20' }
    ]
  }
];
