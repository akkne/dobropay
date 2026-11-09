'use client';

import React from 'react';
import Link from 'next/link';

const sections = [
  {
    title: 'Начало работы',
    links: [
      { id: 'overview', label: 'Главная' },
      { id: 'quickstart', label: 'Быстрый старт' },
      { id: 'payment-flow', label: 'Процесс платежа' }
    ]
  },
  {
    title: 'Основы API',
    links: [
      { id: 'authentication', label: 'Аутентификация' },
      { id: 'requests', label: 'Формат запросов' },
      { id: 'idempotency', label: 'Идемпотентность' },
      { id: 'errors', label: 'Ошибки и статусы' }
    ]
  },
  {
    title: 'Платежи',
    links: [
      { id: 'create-payment', label: 'Создание платежа' },
      { id: 'get-payment', label: 'Получение платежа' },
      { id: 'refunds', label: 'Возвраты' },
      { id: 'webhooks', label: 'Входящие уведомления' }
    ]
  },
  {
    title: 'Справочник',
    links: [
      { id: 'methods', label: 'Методы API' },
      { id: 'sandbox', label: 'Тестирование' }
    ]
  }
];

function CodeBlock({ children }: { children: React.ReactNode }) {
  return (
    <pre className="overflow-x-auto rounded-2xl bg-[#10243d] p-5 text-sm leading-6 text-slate-100 shadow-sm">
      <code>{children}</code>
    </pre>
  );
}

function SectionTitle({ children }: { children: React.ReactNode }) {
  return <h2 className="mb-4 text-2xl font-bold tracking-tight text-slate-900">{children}</h2>;
}

export default function DevelopersPage() {
  return (
    <div className="min-h-screen bg-white text-slate-700">
      <header className="sticky top-0 z-30 flex h-16 items-center justify-between border-b border-slate-200 bg-white/95 px-5 backdrop-blur md:px-10">
        <Link href="/" className="flex items-center gap-3 font-semibold text-slate-900">
          <span className="grid h-9 w-9 place-items-center rounded-xl bg-blue-600 text-sm font-bold text-white">D</span>
          <span>Dobropay <span className="font-normal text-slate-500">/ Разработчикам</span></span>
        </Link>
        <Link href="/" className="text-sm font-semibold text-blue-600 hover:text-blue-700">В кабинет →</Link>
      </header>

      <div className="mx-auto flex max-w-[1440px]">
        <aside className="sticky top-16 hidden h-[calc(100vh-4rem)] w-[290px] shrink-0 overflow-y-auto border-r border-slate-200 px-6 py-8 lg:block">
          <nav aria-label="Навигация по документации" className="space-y-8">
            {sections.map((section) => (
              <section key={section.title}>
                <h2 className="mb-3 text-sm font-bold text-slate-900">{section.title}</h2>
                <ul className="space-y-2 border-l border-slate-200 pl-4">
                  {section.links.map((item) => (
                    <li key={item.id}>
                      <a className="text-sm text-slate-600 transition-colors hover:text-blue-600" href={`#${item.id}`}>
                        {item.label}
                      </a>
                    </li>
                  ))}
                </ul>
              </section>
            ))}
          </nav>
        </aside>

        <main className="min-w-0 flex-1 px-5 py-10 md:px-10 lg:px-14">
          <div id="overview" className="scroll-mt-24">
            <p className="mb-3 text-sm font-semibold uppercase tracking-[0.14em] text-blue-600">Документация API</p>
            <h1 className="max-w-3xl text-4xl font-bold tracking-tight text-slate-950 md:text-5xl">Интеграция с Dobropay</h1>
            <p className="mt-5 max-w-3xl text-lg leading-8 text-slate-600">
              Создавайте платежи, перенаправляйте покупателей на Checkout и получайте события об изменении статуса.
              Здесь описан API текущей тестовой версии Dobropay.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <a href="#quickstart" className="rounded-xl bg-blue-600 px-5 py-3 text-sm font-semibold text-white hover:bg-blue-700">Быстрый старт →</a>
              <a href="#methods" className="rounded-xl border border-slate-300 px-5 py-3 text-sm font-semibold text-slate-700 hover:bg-slate-50">Методы API</a>
            </div>
            <div className="mt-10 rounded-2xl border border-blue-100 bg-blue-50 p-5 text-sm leading-6 text-blue-950">
              <strong>Тестовый контур.</strong> Платёжные сценарии в этой версии предназначены для разработки и проверки интеграции.
              Не используйте тестовые ключи и данные для реальных расчётов.
            </div>
          </div>

          <div className="mt-16 grid gap-4 md:grid-cols-3">
            {[
              ['Платежи', 'Создание, проверка статуса и отмена платежа через REST API.'],
              ['Checkout', 'Готовая страница оплаты для покупателя по ссылке из ответа API.'],
              ['Webhook', 'Подписанные события помогают синхронизировать статусы с вашим сервисом.']
            ].map(([title, body]) => (
              <a key={title} href={title === 'Checkout' ? '#payment-flow' : title === 'Webhook' ? '#webhooks' : '#create-payment'} className="rounded-2xl bg-slate-100 p-5 transition-colors hover:bg-slate-200">
                <h2 className="text-lg font-bold text-slate-900">{title}</h2>
                <p className="mt-2 text-sm leading-6 text-slate-600">{body}</p>
              </a>
            ))}
          </div>

          <section id="quickstart" className="mt-20 scroll-mt-24">
            <SectionTitle>Быстрый старт</SectionTitle>
            <p className="mb-5 max-w-3xl leading-7">Для запросов к API используйте адрес своего контура и серверный API-ключ магазина. В примере ниже указан локальный адрес и демонстрационный ключ.</p>
            <CodeBlock>{`curl -X POST http://localhost:8080/v1/payments \\
  -H 'X-Api-Key: dp_test_shop_a_123456789' \\
  -H 'Idempotency-Key: order-42' \\
  -H 'Content-Type: application/json' \\
  -d '{"amount":129900,"currency":"RUB","externalOrderId":"order-42"}'`}</CodeBlock>
            <p className="mt-4 text-sm leading-6 text-slate-600">Сумма передаётся в копейках: <code className="rounded bg-slate-100 px-1.5 py-0.5">129900</code> означает 1 299 ₽. Успешное создание возвращает идентификатор платежа, статус и ссылку Checkout.</p>
          </section>

          <section id="payment-flow" className="mt-20 scroll-mt-24">
            <SectionTitle>Процесс платежа</SectionTitle>
            <ol className="grid gap-4 md:grid-cols-3">
              {[
                ['01', 'Создайте платёж', 'Передайте сумму, валюту, ключ идемпотентности и дополнительные параметры заказа.'],
                ['02', 'Откройте Checkout', 'Перенаправьте покупателя по checkoutUrl из ответа API.'],
                ['03', 'Получите результат', 'Обновите статус по webhook или запросите платёж через API.']
              ].map(([number, title, body]) => (
                <li key={number} className="rounded-2xl border border-slate-200 p-5">
                  <span className="text-sm font-bold text-blue-600">{number}</span>
                  <h3 className="mt-3 font-bold text-slate-900">{title}</h3>
                  <p className="mt-2 text-sm leading-6 text-slate-600">{body}</p>
                </li>
              ))}
            </ol>
          </section>

          <section id="authentication" className="mt-20 scroll-mt-24">
            <SectionTitle>Аутентификация</SectionTitle>
            <p className="max-w-3xl leading-7">Для операций магазина передавайте API-ключ в заголовке <code className="rounded bg-slate-100 px-1.5 py-0.5">X-Api-Key</code>. Выпускайте и отзывайте ключи в кабинете. Ключ храните только на сервере и не включайте в код браузера или мобильного приложения.</p>
          </section>

          <section id="requests" className="mt-16 scroll-mt-24">
            <SectionTitle>Формат запросов</SectionTitle>
            <ul className="list-disc space-y-2 pl-5 leading-7">
              <li>Базовый локальный адрес API: <code>http://localhost:8080</code>.</li>
              <li>Тело запроса и ответа — JSON; для запросов с телом указывайте <code>Content-Type: application/json</code>.</li>
              <li>Сумма — положительное целое число в копейках. В текущей версии поддерживается валюта <code>RUB</code>.</li>
              <li>Идентификаторы платежей и возвратов передаются в формате UUID.</li>
            </ul>
          </section>

          <section id="idempotency" className="mt-16 scroll-mt-24">
            <SectionTitle>Идемпотентность</SectionTitle>
            <p className="max-w-3xl leading-7">Для создания платежа и возврата передавайте уникальный для операции заголовок <code className="rounded bg-slate-100 px-1.5 py-0.5">Idempotency-Key</code> длиной от 1 до 200 символов. Повторите запрос с тем же ключом и теми же параметрами, чтобы безопасно получить прежний результат. Если параметры отличаются, API вернёт <code>409 Conflict</code>.</p>
          </section>

          <section id="errors" className="mt-16 scroll-mt-24">
            <SectionTitle>Ошибки и статусы</SectionTitle>
            <div className="overflow-x-auto rounded-2xl border border-slate-200">
              <table className="w-full min-w-[520px] text-left text-sm">
                <thead className="bg-slate-50 text-slate-900"><tr><th className="px-4 py-3">HTTP</th><th className="px-4 py-3">Что означает</th></tr></thead>
                <tbody className="divide-y divide-slate-200">
                  <tr><td className="px-4 py-3 font-mono">400</td><td className="px-4 py-3">Некорректные параметры или отсутствует обязательный заголовок.</td></tr>
                  <tr><td className="px-4 py-3 font-mono">401</td><td className="px-4 py-3">Ключ API не передан или недействителен.</td></tr>
                  <tr><td className="px-4 py-3 font-mono">404</td><td className="px-4 py-3">Объект не найден для этого магазина.</td></tr>
                  <tr><td className="px-4 py-3 font-mono">409</td><td className="px-4 py-3">Конфликт идемпотентности или состояния операции.</td></tr>
                </tbody>
              </table>
            </div>
            <p className="mt-4 leading-7">Платёж проходит состояния <code>pending</code>, <code>waiting_for_capture</code>, <code>succeeded</code> или <code>canceled</code> в зависимости от сценария. Ориентируйтесь на поле <code>status</code> ответа и актуальную схему OpenAPI вашего контура.</p>
          </section>

          <section id="create-payment" className="mt-20 scroll-mt-24">
            <SectionTitle>Создание платежа</SectionTitle>
            <p className="mb-5 leading-7">Метод создаёт платёж и ссылку на Checkout. Поддерживаются параметры сценария подтверждения, метода оплаты, сохранения способа оплаты и фискальных данных чека.</p>
            <CodeBlock>{`POST /v1/payments
X-Api-Key: <api-key>
Idempotency-Key: <unique-operation-key>
Content-Type: application/json

{
  "amount": 129900,
  "currency": "RUB",
  "externalOrderId": "order-42",
  "capture": true
}`}</CodeBlock>
            <p className="mt-4 leading-7">Ответ содержит <code>id</code>, <code>status</code>, <code>amount</code>, <code>currency</code> и <code>checkoutUrl</code>. Запись в журнале магазина связывайте по <code>externalOrderId</code>.</p>
          </section>

          <section id="get-payment" className="mt-16 scroll-mt-24">
            <SectionTitle>Получение платежа</SectionTitle>
            <CodeBlock>{`GET /v1/payments/{id}
X-Api-Key: <api-key>`}</CodeBlock>
            <p className="mt-4 leading-7">Ответ возвращает статус и сумму платежа. Идентификатор другого магазина не раскрывается: API отвечает так же, как для отсутствующего платежа.</p>
          </section>

          <section id="refunds" className="mt-16 scroll-mt-24">
            <SectionTitle>Возвраты</SectionTitle>
            <p className="mb-5 leading-7">Создайте полный или частичный возврат на исходный платёж. Передайте сумму в копейках и отдельный ключ идемпотентности.</p>
            <CodeBlock>{`POST /v1/refunds
X-Api-Key: <api-key>
Idempotency-Key: refund-order-42
Content-Type: application/json

{
  "paymentId": "<payment-uuid>",
  "amount": 129900,
  "currency": "RUB"
}`}</CodeBlock>
            <p className="mt-4 leading-7">Проверить возврат можно через <code>GET /v1/refunds/{'{id}'}</code>, а список возвратов платежа — через <code>GET /v1/payments/{'{id}'}/refunds</code>.</p>
          </section>

          <section id="webhooks" className="mt-20 scroll-mt-24">
            <SectionTitle>Входящие уведомления</SectionTitle>
            <p className="mb-5 max-w-3xl leading-7">Настройте URL приёмника и секрет подписи в кабинете. Dobropay отправит событие методом POST с JSON-телом и HMAC-SHA256 подписью. Принимайте уведомление только после проверки подписи и затем сверяйте статус платежа через API.</p>
            <CodeBlock>{`X-Dobropay-Timestamp: <unix-seconds>
X-Dobropay-Signature: sha256=<hex>

{
  "event_id": "<uuid>",
  "event_type": "payment.succeeded",
  "occurred_at": "2026-11-09T10:00:00Z",
  "payment": { "id": "<uuid>", "status": "succeeded", "amount": 129900, "currency": "RUB" }
}`}</CodeBlock>
            <p className="mt-4 leading-7">Подпись вычисляется по исходным байтам тела: <code>HMAC-SHA256(secret, UTF8(timestamp + ".") + raw_body)</code>. События могут повторяться — используйте <code>event_id</code> для дедупликации.</p>
          </section>

          <section id="methods" className="mt-20 scroll-mt-24">
            <SectionTitle>Методы API</SectionTitle>
            <div className="overflow-x-auto rounded-2xl border border-slate-200">
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

          <section id="sandbox" className="mt-20 scroll-mt-24">
            <SectionTitle>Тестирование</SectionTitle>
            <p className="max-w-3xl leading-7">Запустите локальный контур командой <code className="rounded bg-slate-100 px-1.5 py-0.5">docker compose up -d --build</code> из корня проекта. Swagger доступен по адресу <code>http://localhost:8080/swagger</code>. Тестовый Checkout и локальный приёмник webhook позволяют проверить интеграцию без реальной оплаты.</p>
          </section>

          <footer className="mt-24 border-t border-slate-200 py-8 text-sm text-slate-500">
            Документация отражает текущий контракт тестовой версии Dobropay. Схема OpenAPI локального контура: <a className="text-blue-600 hover:underline" href="http://localhost:8080/swagger/v1/swagger.json">swagger/v1/swagger.json</a>.
          </footer>
        </main>
      </div>
    </div>
  );
}
