import type { Metadata } from 'next';
import Link from 'next/link';
import './landing.css';

export const metadata: Metadata = {
  title: 'dobropay — приём платежей через СБП',
  description: 'Приём платежей на сайте и по ссылке, чеки и подключение магазина.'
};

function Brand() {
  return <Link className="brand" href="/" aria-label="dobropay — главная">dobropay</Link>;
}

function WindowBar() {
  return <div className="window-bar" aria-hidden="true"><i /><i /><i /></div>;
}

export default function LandingPage() {
  return (
    <div className="landing">
      <header className="header">
        <div className="wrap topbar">
          <Brand />
          <nav className="nav" aria-label="Основное меню">
            <Link className="nav-home" href="/">Главная</Link>
            <a className="nav-receipts" href="/#receipts">Чеки</a>
            <a className="nav-connect" href="/#connect">Как подключить</a>
            <Link href="/developers">Разработчикам</Link>
          </nav>
          <div className="header-actions">
            <a className="button primary home-action" href="/#connect">Подключить dobropay</a>
            <a className="button outline home-action" href="/#demo">Посмотреть демо</a>
            <Link className="button login" href="/managing">Войти</Link>
          </div>
        </div>
      </header>

      <main>
        <section className="page inside" id="connect" aria-labelledby="connect-title">
          <div className="content">
            <h1 className="page-title" id="connect-title">Как подключить dobropay</h1>
            <p className="lead">От регистрации до проверки заявки — три шага.</p>
            <Link className="button primary connect-action" href="/managing">Перейти в кабинет</Link>

            <ol className="steps">
              <li className="step">
                <div>
                  <span className="step-number">Шаг 1</span>
                  <h2>Зарегистрируйтесь в dobropay</h2>
                  <p>Подготовьте ИНН. Укажите имя, номер телефона, рабочую почту и форму бизнеса. Придумайте пароль для личного кабинета.</p>
                  <p>Если у вас уже есть аккаунт, войдите в него.</p>
                </div>
                <div className="window" aria-hidden="true">
                  <WindowBar />
                  <div className="registration">
                    <div className="registration-side"><span className="brand">dobropay</span></div>
                    <div className="window-body">
                      <h3>Регистрация</h3>
                      <div className="field">Ваше имя<span>Анна</span></div>
                      <div className="field">Форма бизнеса<span>ИП</span></div>
                    </div>
                  </div>
                </div>
              </li>

              <li className="step">
                <div>
                  <span className="step-number">Шаг 2</span>
                  <h2>Заполните данные для договора</h2>
                  <p>Откройте анкету в личном кабинете. Для организации подготовьте:</p>
                  <ul>
                    <li>реквизиты организации;</li>
                    <li>сведения о государственной регистрации;</li>
                    <li>цветной снимок паспорта;</li>
                    <li>паспортные данные других владельцев, если они есть.</li>
                  </ul>
                </div>
                <div className="window" aria-hidden="true">
                  <WindowBar />
                  <div className="window-body">
                    <h3>Анкета организации</h3>
                    <div className="window-list">
                      <div><b>✓</b>Реквизиты</div>
                      <div><b>✓</b>Сведения о регистрации</div>
                      <div><b>✓</b>Документы владельцев</div>
                    </div>
                  </div>
                </div>
              </li>

              <li className="step">
                <div>
                  <span className="step-number">Шаг 3</span>
                  <h2>Отправьте данные на проверку</h2>
                  <p>Проверьте анкету и отправьте заявку. Ответ придёт на рабочую почту и появится в личном кабинете.</p>
                  <p>Если понадобятся уточнения, в заявке будет указано, какие сведения исправить или какой документ приложить.</p>
                </div>
                <div className="window" aria-hidden="true">
                  <WindowBar />
                  <div className="window-body review">
                    <svg viewBox="0 0 40 40" fill="none" aria-hidden="true">
                      <circle cx="20" cy="20" r="18" fill="var(--pink)" />
                      <circle cx="20" cy="20" r="12" stroke="currentColor" strokeWidth="2" />
                      <path d="M20 12v9l6 3" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                    </svg>
                    <h3>Проверяем данные</h3>
                    <small>Заявка отправлена</small>
                  </div>
                </div>
              </li>
            </ol>
          </div>
        </section>

        <section className="page inside" id="receipts" aria-labelledby="receipts-title">
          <div className="content">
            <div className="receipt-layout">
              <div>
                <h1 className="page-title" id="receipts-title">Чеки</h1>
                <p className="lead">После оплаты через СБП покупатель увидит чек. Вот как выглядит заказ из керамической мастерской.</p>
                <a className="button primary" href="/#connect">Подключить dobropay</a>
              </div>
              <article className="receipt" aria-label="Пример чека dobropay">
                <div className="row">
                  <span className="brand">dobropay</span>
                  <span className="status">✓ Оплачено</span>
                </div>
                <h2>Чек · Заказ №1042</h2>
                <dl>
                  <div className="row"><dt>Керамическая кружка</dt><dd>1 800 ₽</dd></div>
                  <div className="row"><dt>Количество</dt><dd>1 шт.</dd></div>
                  <div className="row"><dt>Способ оплаты</dt><dd>СБП</dd></div>
                </dl>
                <div className="row total"><span>Итого</span><strong>1 800 ₽</strong></div>
                <small className="sample">Демонстрационный образец</small>
              </article>
            </div>
            <article className="receipt-note">
              <h2>Как найти покупку</h2>
              <p>Если возникнет вопрос о доставке, покупатель сможет назвать номер заказа из чека.</p>
            </article>
          </div>
        </section>

        <section className="page" id="home" aria-labelledby="home-title">
          <div className="hero">
            <div className="wrap intro">
              <h1 id="home-title">Принимайте платежи<br />на сайте и по ссылке</h1>
              <p>Покупатель платит через СБП в приложении банка. Вы видите, какой заказ оплачен.</p>
            </div>
            <figure className="preview" id="demo" aria-label="Пример покупки в мастерской">
              <ol className="papers">
                <li className="paper order">
                  <div className="row paper-head"><span>Мастерская</span><span>№1042</span></div>
                  <div className="product">
                    <svg className="mug" viewBox="0 0 48 48" fill="none" aria-hidden="true">
                      <path d="M31 15h4a6 6 0 0 1 0 12h-4" stroke="var(--blue)" strokeWidth="2.4" />
                      <path d="M9 13h23v14a10 10 0 0 1-10 10h-3A10 10 0 0 1 9 27Z" fill="var(--pink)" stroke="var(--blue)" strokeWidth="2.4" />
                      <ellipse cx="20.5" cy="13" rx="11.5" ry="3" fill="var(--white)" stroke="var(--blue)" strokeWidth="2" />
                    </svg>
                    <div><strong>Керамическая кружка</strong><small>1 шт.</small></div>
                  </div>
                  <div className="row paper-foot"><span>Итого</span><b>1 800 ₽</b></div>
                </li>
                <li className="paper payment">
                  <h2>К оплате</h2>
                  <strong className="amount">1 800 ₽</strong>
                  <span className="pay-action">Оплатить через СБП</span>
                </li>
                <li className="paper notice">
                  <div className="notice-head"><span className="check" aria-hidden="true">✓</span><strong>Заказ оплачен</strong></div>
                  <p>Заказ №1042<br />Керамическая кружка</p>
                  <div className="row paper-foot"><span>СБП</span><b>1 800 ₽</b></div>
                </li>
              </ol>
              <figcaption>Демонстрационные данные</figcaption>
            </figure>
          </div>
        </section>
      </main>

      <footer>
        <div className="wrap footer-inner">
          <Brand />
          <small>Оплата через СБП</small>
        </div>
      </footer>
    </div>
  );
}
