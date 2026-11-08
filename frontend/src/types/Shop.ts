export interface Shop {
  id: string;
  name: string;
  status: 'active' | 'under_review' | 'blocked';
  inn?: string;
  ogrn?: string;
  legalName?: string;
  checkingAccount?: string;
  bik?: string;
  website?: string;
  adminEmail?: string;
  createdAt?: string;
}
