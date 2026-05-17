(function () {
  const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

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
  }

  document.addEventListener('submit', (event) => {
    const form = event.target.closest('[data-cart-form]');
    if (!form) return;
    event.preventDefault();
    submitCart(form);
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
