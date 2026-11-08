export interface FiscalReceiptItem {
  name: string;
  price: number;
  quantity: number;
  vat: string;
}

export interface FiscalReceipt {
  id: string;
  paymentId: string;
  orderNumber: string;
  amount: number;
  fiscalStatus: 'succeeded' | 'pending';
  fnNumber: string;
  fdNumber: string;
  fpdNumber: string;
  customerEmail: string;
  emailStatus: 'sent' | 'sending';
  createdAt: string;
  items: FiscalReceiptItem[];
}
