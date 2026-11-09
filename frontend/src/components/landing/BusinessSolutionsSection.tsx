import React from 'react';

const solutions = [
  {
    title: 'Приём оплаты на сайте',
    text: 'Создайте платёж в своём сервисе и перенаправьте покупателя на защищённую страницу оплаты.'
  },
  {
    title: 'Ссылка для клиента',
    text: 'Выставьте счёт по заказу и отправьте покупателю ссылку в сообщении или по электронной почте.'
  },
  {
    title: 'Для небольшого бизнеса',
    text: 'Принимайте оплату за заказ, даже если у вас пока нет собственного интернет-магазина.'
  },
  {
    title: 'Статусы и чеки',
    text: 'Проверяйте платежи в кабинете, а обновления получайте через webhook.'
  }
];

export function BusinessSolutionsSection() {
  return (
    <section className="page inside" id="solutions" aria-labelledby="solutions-title">
      <div className="content">
        <h1 className="page-title" id="solutions-title">Решения для бизнеса</h1>
        <p className="lead">Принимайте платежи на сайте или отправляйте клиентам отдельные ссылки на оплату.</p>
        <div className="solutions-grid">
          {solutions.map((solution) => (
            <article className="solution-card" key={solution.title}>
              <h2>{solution.title}</h2>
              <p>{solution.text}</p>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
}
