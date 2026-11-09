export interface WebhookDelivery {
  id: string;
  state: string;
  attempts: number;
  lastResult: string | null;
}
