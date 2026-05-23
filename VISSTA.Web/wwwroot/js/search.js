(function () {
  const panel = document.querySelector('[data-search-panel]');
  const input = document.querySelector('[data-search-input]');
  const results = document.querySelector('[data-search-results]');
  const toggle = document.querySelector('[data-search-toggle]');
  let timer;

  toggle?.addEventListener('click', () => {
    panel?.classList.toggle('is-open');
    input?.focus();
  });

  input?.addEventListener('input', () => {
    clearTimeout(timer);
    const q = input.value.trim();
    if (q.length < 2) {
      if (results) results.innerHTML = '';
      return;
    }

    timer = setTimeout(async () => {
      const response = await fetch(`/api/search?q=${encodeURIComponent(q)}`);
      const items = await response.json();
      if (!results) return;
      results.innerHTML = items.map((item) => `
        <a class="search-result" href="/shop/${item.slug}">
          <img src="${item.imageUrl}" alt="">
          <span>${item.name}</span>
          <small>${Number(item.price).toLocaleString()} EGP</small>
        </a>`).join('');
    }, 220);
  });

  document.querySelector('[data-filter-form]')?.addEventListener('submit', async (event) => {
    event.preventDefault();
    const form = event.currentTarget;
    const target = document.querySelector('[data-catalog-target]');
    const action = form.getAttribute('action') || window.location.pathname;
    const params = new URLSearchParams();

    for (const [key, value] of new FormData(form)) {
      const trimmed = String(value ?? '').trim();
      if (trimmed.length > 0) {
        params.set(key, trimmed);
      }
    }

    const query = params.toString();
    const url = query ? `${action}?${query}` : action;
    const response = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
    if (target) target.innerHTML = await response.text();
    history.replaceState(null, '', url);
  });
})();
