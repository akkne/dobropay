export interface ApiKey {
  id: string;
  name: string;
  prefix: string;
  apiKey?: string;
  createdAt: string;
  status: 'active' | 'revoked';
}
