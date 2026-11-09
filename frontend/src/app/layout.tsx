import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  metadataBase: new URL('https://dobroplay.online'),
  title: 'Dobropay — Платёжный шлюз и личный кабинет мерчанта',
  description: 'Приём платежей через СБП, банковские карты и криптовалюту. Hosted Checkout и портал разработчика.',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="ru">
      <head>
        <meta charSet="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
      </head>
      <body>{children}</body>
    </html>
  );
}
