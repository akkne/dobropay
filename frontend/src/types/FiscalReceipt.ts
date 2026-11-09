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
  fiscalStatus: string;
  fnNumber: string;
  fdNumber: string;
  fpdNumber: string;
  customerEmail: string;
  createdAt: string;
  items: FiscalReceiptItem[];
}
