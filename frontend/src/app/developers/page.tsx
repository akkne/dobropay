'use client';

import React from 'react';
import Link from 'next/link';

type NavigationLink = { id: string; label: string };
type NavigationEntry = NavigationLink | { heading: string };
type NavigationGroup = { title: string; entries: NavigationEntry[] };

const navigation: NavigationGroup[] = [
  {
    title: 'Основы работы с API',
    entries: [
      { id: 'errors', label: 'Обработка ответов' },
      { id: 'openapi', label: 'Спецификация OpenAPI' },
      { id: 'webhooks', label: 'Входящие уведомления' },
      { id: 'changelog', label: 'История изменений' }
    ]
  },
  {
    title: 'Приём платежей в интернете',
    entries: [
      { id: 'online', label: 'О платежах в интернете' },
      { heading: 'С чего начать' },
      { id: 'quickstart', label: 'Быстрый старт' },
      { id: 'payment-flow', label: 'Процесс платежа' },
      { id: 'payment-methods', label: 'Способы оплаты' },
      { id: 'integration-choice', label: 'Выбор сценария интеграции' },
      { heading: 'Сценарии интеграции' },
      { id: 'manual-integration', label: 'Самостоятельная интеграция' }
    ]
  }
];

function CodeBlock({ children }: { children: React.ReactNode }) {
  return (
    <pre className="docs-code overflow-x-auto rounded-xl bg-[#10243d] p-5 text-sm leading-6 text-slate-100 shadow-sm">
      <code>{children}</code>
    </pre>
  );
}

function SectionTitle({ children }: { children: React.ReactNode }) {
  return <h2 className="docs-section-title mb-5 text-3xl font-semibold tracking-tight text-slate-950">{children}</h2>;
}

function NavigationContents() {
  return (
    <nav aria-label="Навигация по документации" className="space-y-1">
      <a className="docs-nav-home block rounded-lg px-3 py-2 text-sm font-semibold text-blue-600 hover:bg-blue-50" href="#overview">
        Главная
      </a>
      {navigation.map((group) => (
        <details key={group.title} className="docs-nav-group group" open>
          <summary className="docs-nav-summary flex cursor-pointer list-none items-center justify-between rounded-lg px-3 py-3 text-sm font-semibold text-slate-900 hover:bg-slate-50">
            {group.title}
            <span aria-hidden="true" className="text-slate-400 transition-transform group-open:rotate-180">⌄</span>
          </summary>
          <div className="docs-nav-items ml-3 border-l border-slate-200 pb-2 pl-3">
            {group.entries.map((entry, entryIndex) => (
              'heading' in entry ? (
                <p key={`${entry.heading}-${entryIndex}`} className="px-3 pb-1 pt-4 text-xs font-semibold text-slate-900">
                  {entry.heading}
                </p>
              ) : (
                <a key={entry.id} className="docs-nav-link block rounded-lg px-3 py-2 text-sm leading-5 text-slate-600 transition-colors hover:bg-blue-50 hover:text-blue-700" href={`#${entry.id}`}>
                  {entry.label}
                </a>
              )
            ))}
          </div>
        </details>
      ))}
    </nav>
  );
}

export default function DevelopersPage() {
  return (
    <div className="docs-page min-h-screen">
      <header className="docs-header sticky top-0 z-30 flex items-center justify-between">
        <Link href="/developers" className="docs-brand flex items-center">
          <span className="docs-brand-mark grid place-items-center"><span>D</span></span>
          <span>Dobropay <span className="docs-brand-subtitle font-normal">/ Документация API</span></span>
        </Link>
        <Link href="/" className="docs-header-action text-sm font-semibold text-blue-600 transition-colors hover:text-blue-700">В кабинет →</Link>
      </header>

      <div className="docs-shell mx-auto flex">
        <aside className="docs-sidebar sticky top-16 hidden h-[calc(100vh-4rem)] w-[280px] shrink-0 overflow-y-auto border-r border-slate-200 px-5 py-7 md:block">
          <NavigationContents />
        </aside>

        <main className="docs-content min-w-0 flex-1 px-5 py-8 md:px-10 lg:px-14 lg:py-12">
          <details className="docs-mobile-menu mb-8 rounded-xl border border-slate-200 md:hidden">
            <summary className="cursor-pointer list-none px-4 py-3 text-sm font-semibold text-slate-900">Разделы документации</summary>
            <div className="border-t border-slate-200 p-3"><NavigationContents /></div>
          </details>

          <div id="overview" className="scroll-mt-24">
            <p className="mb-3 text-sm font-semibold uppercase tracking-[0.14em] text-blue-600">Документация API</p>
            <h1 className="docs-title max-w-3xl text-4xl font-bold tracking-tight text-slate-950 md:text-5xl">API Dobropay</h1>
            <p className="docs-lead mt-5 max-w-3xl text-lg leading-8 text-slate-600">
              Единый API для создания и обработки платежей, возвратов и уведомлений о смене статусов.
            </p>
          </div>

          <section id="online" className="docs-section mt-16 scroll-mt-24 border-t border-slate-200 pt-10">
            <SectionTitle>О платежах в интернете</SectionTitle>
            <p className="max-w-3xl leading-7">Магазин создаёт платёж через REST API и получает ссылку Checkout для покупателя. Dobropay обрабатывает результат платежа, передаёт обновления через webhook и позволяет запросить актуальный статус через API.</p>
            <div className="mt-7 grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
              {[
                ['Приём платежей', 'Создайте платёж и передайте покупателю ссылку на Checkout.', 'quickstart'],
                ['Статусы и уведомления', 'Получайте события о результате и сверяйте статус платежа.', 'webhooks'],
                ['Возвраты', 'Создавайте полный или частичный возврат через API.', 'manual-integration']
              ].map(([title, body, id]) => (
                <a key={title} href={`#${id}`} className="docs-card rounded-xl border border-slate-200 p-5 transition-colors hover:border-blue-300 hover:bg-blue-50/40">
                  <h3 className="font-semibold text-slate-950">{title}</h3>
                  <p className="mt-2 text-sm leading-6 text-slate-600">{body}</p>
                </a>
              ))}
            </div>
          </section>

          <section id="quickstart" className="docs-section mt-16 scroll-mt-24 border-t border-slate-200 pt-10">
            <SectionTitle>Быстрый старт</SectionTitle>
            <p className="mb-5 max-w-3xl leading-7">Отправляйте запросы на <code>https://dobroplay.online</code> с API-ключом магазина в заголовке.</p>
            <CodeBlock>{`curl -X POST https://dobroplay.online/v1/payments \\
  -H 'X-Api-Key: <api-key>' \\
  -H 'Idempotency-Key: order-42' \\
  -H 'Content-Type: application/json' \\
  -d '{"amount":129900,"currency":"RUB","externalOrderId":"order-42"}'`}</CodeBlock>
            <p className="mt-4 text-sm leading-6 text-slate-600">Сумма передаётся в копейках: <code className="rounded bg-slate-100 px-1.5 py-0.5">129900</code> означает 1 299 ₽. Успешный запрос возвращает идентификатор, статус и ссылку Checkout.</p>
          </section>

          <section id="payment-flow" className="docs-section mt-16 scroll-mt-24 border-t border-slate-200 pt-10">
            <SectionTitle>Процесс платежа</SectionTitle>
            <ol className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
              {[
                ['01', 'Создайте платёж', 'Передайте сумму, валюту, ключ идемпотентности и параметры заказа.'],
                ['02', 'Откройте Checkout', 'Перенаправьте покупателя по checkoutUrl из ответа API.'],
                ['03', 'Получите результат', 'Обновите статус по webhook или запросите платёж через API.']
              ].map(([number, title, body]) => (
                <li key={number} className="docs-card docs-step rounded-xl border border-slate-200 p-5">
                  <span className="text-sm font-bold text-blue-600">{number}</span>
                  <h3 className="mt-3 font-semibold text-slate-950">{title}</h3>
                  <p className="mt-2 text-sm leading-6 text-slate-600">{body}</p>
                </li>
              ))}
            </ol>
          </section>

          <section id="payment-methods" className="docs-section mt-16 scroll-mt-24 border-t border-slate-200 pt-10">
            <SectionTitle>Способы оплаты</SectionTitle>
            <p className="mb-6 max-w-3xl leading-7">Доступны банковские карты и Система быстрых платежей. Способ можно оставить на выбор покупателя в Checkout или передать при создании платежа.</p>
            <div className="grid gap-4 md:grid-cols-2">
              <div className="docs-card rounded-xl border border-slate-200 p-5"><h3 className="font-semibold text-slate-950">Банковская карта</h3><p className="mt-2 text-sm leading-6 text-slate-600">Используйте значение <code>bank_card</code> в параметре <code>paymentMethodType</code>.</p></div>
              <div className="docs-card rounded-xl border border-slate-200 p-5"><h3 className="font-semibold text-slate-950">СБП</h3><p className="mt-2 text-sm leading-6 text-slate-600">Используйте значение <code>sbp</code> в параметре <code>paymentMethodType</code>.</p></div>
            </div>
          </section>

          <section id="integration-choice" className="docs-section mt-16 scroll-mt-24 border-t border-slate-200 pt-10">
            <SectionTitle>Выбор сценария интеграции</SectionTitle>
            <p className="mb-6 max-w-3xl leading-7">Текущая версия Dobropay принимает запросы магазина через REST API. Для страницы оплаты используйте Checkout, а интеграцию со своей системой стройте на API и webhook.</p>
            <div className="grid gap-4 md:grid-cols-2">
              <a href="#quickstart" className="docs-card rounded-xl border border-slate-200 p-5 transition-colors hover:border-blue-300 hover:bg-blue-50/40"><h3 className="font-semibold text-slate-950">Checkout для покупателя</h3><p className="mt-2 text-sm leading-6 text-slate-600">Быстрый способ встроить оплату: создайте платёж и перенаправьте покупателя по возвращённой ссылке.</p></a>
              <a href="#manual-integration" className="docs-card rounded-xl border border-slate-200 p-5 transition-colors hover:border-blue-300 hover:bg-blue-50/40"><h3 className="font-semibold text-slate-950">Своя серверная интеграция</h3><p className="mt-2 text-sm leading-6 text-slate-600">Управляйте запросами, статусами и возвратами из backend магазина; события получайте через webhook.</p></a>
            </div>
          </section>

          <section id="errors" className="docs-section mt-16 scroll-mt-24 border-t border-slate-200 pt-10">
            <SectionTitle>Обработка ответов</SectionTitle>
            <p className="mb-5 max-w-3xl leading-7">Успешный ответ содержит данные операции. При ошибке проверьте HTTP-код и поле <code>error</code>, исправьте запрос и повторите его с тем же ключом идемпотентности, если операция могла быть принята.</p>
            <div className="overflow-x-auto rounded-xl border border-slate-200">
              <table className="w-full min-w-[520px] text-left text-sm">
                <thead className="bg-slate-50 text-slate-900"><tr><th className="px-4 py-3">HTTP</th><th className="px-4 py-3">Что означает</th></tr></thead>
                <tbody className="divide-y divide-slate-200">
                  <tr><td className="px-4 py-3 font-mono">400</td><td className="px-4 py-3">Некорректные параметры или отсутствует обязательный заголовок.</td></tr>
                  <tr><td className="px-4 py-3 font-mono">401</td><td className="px-4 py-3">API-ключ не передан или недействителен.</td></tr>
                  <tr><td className="px-4 py-3 font-mono">404</td><td className="px-4 py-3">Объект не найден для этого магазина.</td></tr>
                  <tr><td className="px-4 py-3 font-mono">409</td><td className="px-4 py-3">Конфликт идемпотентности или состояния операции.</td></tr>
                </tbody>
              </table>
            </div>
            <p className="mt-5 max-w-3xl leading-7">Статус платежа находится в поле <code>status</code>. В зависимости от сценария он может быть <code>pending</code>, <code>waiting_for_capture</code>, <code>succeeded</code> или <code>canceled</code>.</p>
          </section>

          <section id="openapi" className="docs-section mt-16 scroll-mt-24 border-t border-slate-200 pt-10">
            <SectionTitle>Спецификация OpenAPI</SectionTitle>
            <p className="mb-5 max-w-3xl leading-7">Схема описывает доступные методы API, параметры запросов и форматы ответов локального контура.</p>
            <div className="flex flex-wrap gap-3">
              <a className="docs-action docs-action-primary rounded-lg bg-blue-600 px-5 py-3 text-sm font-semibold text-white transition-colors hover:bg-blue-700" href="https://dobroplay.online/swagger" target="_blank" rel="noreferrer">Открыть Swagger UI →</a>
              <a className="docs-action docs-action-secondary rounded-lg border border-slate-300 px-5 py-3 text-sm font-semibold text-slate-700 transition-colors hover:bg-slate-50" href="https://dobroplay.online/swagger/v1/swagger.json" target="_blank" rel="noreferrer">Скачать JSON-схему</a>
            </div>
          </section>

          <section id="webhooks" className="docs-section mt-16 scroll-mt-24 border-t border-slate-200 pt-10">
            <SectionTitle>Входящие уведомления</SectionTitle>
            <p className="mb-5 max-w-3xl leading-7">Настройте URL приёмника и секрет подписи в кабинете. Dobropay отправит событие методом POST с JSON-телом и HMAC-SHA256 подписью. Проверяйте подпись до обработки события и используйте <code>event_id</code> для защиты от повторов.</p>
            <CodeBlock>{`X-Dobropay-Timestamp: <unix-seconds>
X-Dobropay-Signature: sha256=<hex>

{
  "event_id": "<uuid>",
  "event_type": "payment.succeeded",
  "occurred_at": "2026-11-09T10:00:00Z",
  "payment": { "id": "<uuid>", "status": "succeeded", "amount": 129900, "currency": "RUB" }
}`}</CodeBlock>
            <p className="mt-5 max-w-3xl leading-7">Подпись вычисляется по исходным байтам тела: <code>HMAC-SHA256(secret, UTF8(timestamp + ".") + raw_body)</code>. После приёма события при необходимости сверьте состояние через API.</p>
          </section>

          <section id="changelog" className="docs-section mt-16 scroll-mt-24 border-t border-slate-200 pt-10">
            <SectionTitle>История изменений</SectionTitle>
            <div className="docs-card rounded-xl border border-slate-200 p-5">
              <p className="text-sm font-semibold text-slate-950">Текущая версия API</p>
              <p className="mt-2 text-sm leading-6 text-slate-600">Доступны создание и просмотр платежей, подтверждение или отмена списания, возвраты, сохранённые способы оплаты и подписанные webhook-события. Полный контракт опубликован в спецификации OpenAPI.</p>
            </div>
          </section>

          <section id="manual-integration" className="docs-section mt-16 scroll-mt-24 border-t border-slate-200 pt-10">
            <SectionTitle>Самостоятельная интеграция</SectionTitle>
            <p className="mb-8 max-w-3xl leading-7">Отправляйте запросы к API из backend магазина. Храните API-ключ на сервере, задавайте уникальный <code>Idempotency-Key</code> для каждой операции и обрабатывайте обновления статусов через webhook.</p>

            <div id="requests" className="scroll-mt-24">
              <h3 className="mb-4 text-xl font-semibold text-slate-950">Формат взаимодействия</h3>
              <ul className="list-disc space-y-2 pl-5 leading-7">
                <li>Базовый адрес API: <code>https://dobroplay.online</code>.</li>
                <li>Тело запроса и ответа — JSON; для запросов с телом указывайте <code>Content-Type: application/json</code>.</li>
                <li>Сумма — положительное целое число в копейках. Поддерживается валюта <code>RUB</code>.</li>
                <li>Идентификаторы платежей и возвратов передаются в формате UUID.</li>
              </ul>
              <p className="mt-5 leading-7">Для операций магазина передавайте ключ в заголовке <code>X-Api-Key</code>. Запросы создания платежей и возвратов защищайте заголовком <code>Idempotency-Key</code>. Повтор с тем же ключом и параметрами вернёт прежний результат.</p>
            </div>

            <div className="mt-10">
              <h3 className="mb-4 text-xl font-semibold text-slate-950">Создание и просмотр платежа</h3>
              <p className="mb-5 leading-7">Создайте платёж, затем передайте покупателю значение <code>checkoutUrl</code> из ответа.</p>
              <CodeBlock>{`POST /v1/payments
X-Api-Key: <api-key>
Idempotency-Key: order-42
Content-Type: application/json

{
  "amount": 129900,
  "currency": "RUB",
  "externalOrderId": "order-42",
  "capture": true,
  "paymentMethodType": "sbp"
}

GET /v1/payments/{id}
X-Api-Key: <api-key>`}</CodeBlock>
              <p className="mt-4 leading-7">Для карточного платежа укажите <code>bank_card</code> в <code>paymentMethodType</code>. Если способ оплаты не задан, доступные варианты выбираются в Checkout.</p>
            </div>

            <div className="mt-10">
              <h3 className="mb-4 text-xl font-semibold text-slate-950">Возвраты</h3>
              <p className="mb-5 leading-7">Создайте полный или частичный возврат на исходный платёж, передав сумму в копейках и отдельный ключ идемпотентности.</p>
              <CodeBlock>{`POST /v1/refunds
X-Api-Key: <api-key>
Idempotency-Key: refund-order-42
Content-Type: application/json

{
  "paymentId": "<payment-uuid>",
  "amount": 129900,
  "currency": "RUB"
}`}</CodeBlock>
              <p className="mt-4 leading-7">Проверить возврат можно через <code>GET /v1/refunds/{'{id}'}</code>, список возвратов платежа — через <code>GET /v1/payments/{'{id}'}/refunds</code>.</p>
            </div>

            <div className="mt-10 overflow-x-auto rounded-xl border border-slate-200">
              <table className="w-full min-w-[680px] text-left text-sm">
                <thead className="bg-slate-50 text-slate-900"><tr><th className="px-4 py-3">Метод</th><th className="px-4 py-3">Путь</th><th className="px-4 py-3">Назначение</th></tr></thead>
                <tbody className="divide-y divide-slate-200">
                  {[
                    ['POST', '/v1/payments', 'Создать платёж'],
                    ['GET', '/v1/payments/{id}', 'Получить платёж'],
                    ['POST', '/v1/payments/{id}/capture', 'Подтвердить списание'],
                    ['POST', '/v1/payments/{id}/cancel', 'Отменить платёж'],
                    ['GET', '/v1/payments/{id}/refunds', 'Список возвратов платежа'],
                    ['POST', '/v1/refunds', 'Создать возврат'],
                    ['GET', '/v1/refunds/{id}', 'Получить возврат'],
                    ['GET', '/v1/payment-methods', 'Список сохранённых способов оплаты']
                  ].map(([method, path, description]) => (
                    <tr key={`${method}-${path}`}><td className="px-4 py-3 font-mono font-semibold text-blue-700">{method}</td><td className="px-4 py-3 font-mono text-slate-800">{path}</td><td className="px-4 py-3">{description}</td></tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </main>
      </div>
    </div>
  );
}
