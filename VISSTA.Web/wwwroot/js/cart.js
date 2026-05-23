(function () {
  const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
  let quantityTimer;

  function formatMoney(value, currency) {
    return `${Number(value).toLocaleString()} ${currency}`;
  }

  function refreshCartPage(cart) {
    document.querySelector('[data-cart-subtotal]')?.replaceChildren(document.createTextNode(formatMoney(cart.subtotal, cart.currency)));
    document.querySelector('[data-summary-count]')?.replaceChildren(document.createTextNode(`${cart.count} item(s)`));

    cart.items?.forEach((item) => {
      const line = document.querySelector(`[data-cart-line="${item.id}"]`);
      line?.querySelector('[data-line-total]')?.replaceChildren(document.createTextNode(formatMoney(item.lineTotal, item.currency)));
      const quantity = line?.querySelector('[data-cart-quantity]');
      if (quantity && document.activeElement !== quantity) {
        quantity.value = item.quantity;
      }
    });
  }

  async function submitCart(form) {
    const body = new FormData(form);
    const response = await fetch(form.action, {
      method: form.method || 'POST',
      body,
      headers: { 'X-Requested-With': 'XMLHttpRequest', 'RequestVerificationToken': token || '' }
    });
    if (!response.ok) return;
    const cart = await response.json();
    document.querySelectorAll('[data-cart-count]').forEach((el) => { el.textContent = cart.count; });
    refreshCartPage(cart);
  }

  document.addEventListener('submit', (event) => {
    const form = event.target.closest('[data-cart-form]');
    if (!form) return;
    event.preventDefault();
    submitCart(form);
  });

  document.addEventListener('input', (event) => {
    const input = event.target.closest('[data-cart-quantity]');
    if (!input) return;

    const form = input.closest('[data-auto-cart-form]');
    if (!form) return;

    const quantity = Number(input.value);
    if (!Number.isFinite(quantity) || quantity < 1) return;

    clearTimeout(quantityTimer);
    quantityTimer = setTimeout(() => submitCart(form), 350);
  });

  document.addEventListener('change', (event) => {
    const input = event.target.closest('[data-cart-quantity]');
    if (!input) return;

    if (Number(input.value) < 1) {
      input.value = 1;
    }

    clearTimeout(quantityTimer);
    const form = input.closest('[data-auto-cart-form]');
    if (form) submitCart(form);
  });

  document.querySelector('[data-newsletter-form]')?.addEventListener('submit', async (event) => {
    event.preventDefault();
    const form = event.currentTarget;
    await fetch('/api/newsletter', {
      method: 'POST',
      body: new FormData(form),
      headers: { 'RequestVerificationToken': token || '' }
    });
    form.reset();
  });
})();
