export interface WebhookDelivery {
  id: string;
  eventType: string;
  paymentId: string;
  url: string;
  status: 'delivered' | 'retrying' | 'failed';
  statusCode: number;
  attempts: number;
  createdAt: string;
  nextRetryAt?: string;
  payload: string;
  signature: string;
}
