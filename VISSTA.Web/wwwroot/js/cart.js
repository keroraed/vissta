(function () {
  const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
  let quantityTimer;
  const sidebar = document.querySelector('[data-cart-sidebar]');
  const sidebarItems = sidebar?.querySelector('[data-cart-sidebar-items]');
  const sidebarSubtotal = sidebar?.querySelector('[data-cart-mini-subtotal]');
  const sidebarCount = sidebar?.querySelector('[data-cart-mini-count]');
  const sidebarClose = sidebar?.querySelector('[data-cart-close]');
  const overlay = document.querySelector('[data-cart-overlay]');

  function formatMoney(value, currency) {
    return `${Number(value).toLocaleString()} ${currency}`;
  }

  function buildCartEmptyState() {
    const empty = document.createElement('article');
    empty.className = 'empty-state glassmorphism cart-empty';

    const title = document.createElement('h2');
    title.textContent = 'Your cart is empty.';

    const copy = document.createElement('p');
    copy.textContent = 'Add a few essentials before checkout.';

    const link = document.createElement('a');
    link.className = 'button button-primary';
    link.href = '/Shop/Index';
    link.textContent = 'Continue Shopping';

    empty.append(title, copy, link);
    return empty;
  }

  function renderCartSummary(cart) {
    const summary = document.querySelector('.cart-summary');
    if (!summary) return;

    summary.replaceChildren();

    if (!cart.count) {
      const label = document.createElement('p');
      label.textContent = 'Cart';

      const status = document.createElement('strong');
      status.textContent = 'Empty';

      const link = document.createElement('a');
      link.className = 'button button-secondary';
      link.href = '/Shop/Index';
      link.textContent = 'Shop Now';

      summary.append(label, status, link);
      return;
    }

    const head = document.createElement('div');
    head.className = 'summary-head';

    const headTitle = document.createElement('p');
    headTitle.textContent = 'Order summary';

    const headCount = document.createElement('span');
    headCount.setAttribute('data-summary-count', '');
    headCount.textContent = `${cart.count} item(s)`;

    head.append(headTitle, headCount);

    const row = document.createElement('div');
    row.className = 'summary-row';

    const rowLabel = document.createElement('span');
    rowLabel.textContent = 'Subtotal';

    const rowValue = document.createElement('strong');
    rowValue.setAttribute('data-cart-subtotal', '');
    rowValue.textContent = formatMoney(cart.subtotal, cart.currency);

    row.append(rowLabel, rowValue);

    const note = document.createElement('div');
    note.className = 'summary-note';
    note.textContent = 'Shipping and payment are confirmed in checkout.';

    const checkout = document.createElement('a');
    checkout.className = 'button button-primary';
    checkout.href = '/Checkout/Index';
    checkout.textContent = 'Checkout';

    const continueLink = document.createElement('a');
    continueLink.className = 'summary-link';
    continueLink.href = '/Shop/Index';
    continueLink.textContent = 'Continue shopping';

    summary.append(head, row, note, checkout, continueLink);
  }

  function refreshCartPage(cart) {
    document.querySelector('[data-cart-subtotal]')?.replaceChildren(document.createTextNode(formatMoney(cart.subtotal, cart.currency)));
    document.querySelector('[data-summary-count]')?.replaceChildren(document.createTextNode(`${cart.count} item(s)`));

    const cartList = document.querySelector('.cart-list');
    if (cartList) {
      const itemIds = new Set((cart.items || []).map((item) => String(item.id)));
      cartList.querySelectorAll('[data-cart-line]').forEach((line) => {
        if (!itemIds.has(line.dataset.cartLine)) {
          line.remove();
        }
      });

      cart.items?.forEach((item) => {
        const line = cartList.querySelector(`[data-cart-line="${item.id}"]`);
        line?.querySelector('[data-line-total]')?.replaceChildren(document.createTextNode(formatMoney(item.lineTotal, item.currency)));
        const quantity = line?.querySelector('[data-cart-quantity]');
        if (quantity && document.activeElement !== quantity) {
          quantity.value = item.quantity;
        }
      });

      if (!cart.count) {
        cartList.replaceChildren(buildCartEmptyState());
      }
    }

    const summary = document.querySelector('.cart-summary');
    if (summary && (cart.count === 0 || !summary.querySelector('[data-summary-count]'))) {
      renderCartSummary(cart);
    }
  }

  function setSidebarOpen(isOpen) {
    if (!sidebar) return;
    sidebar.classList.toggle('is-open', isOpen);
    sidebar.setAttribute('aria-hidden', String(!isOpen));
    overlay?.classList.toggle('is-visible', isOpen);
    document.body.classList.toggle('is-cart-open', isOpen);
  }

  function renderMiniCart(cart) {
    if (!sidebarItems) return;

    sidebarItems.replaceChildren();

    if (!cart.items || cart.items.length === 0) {
      const empty = document.createElement('p');
      empty.className = 'cart-mini-empty';
      empty.textContent = 'Your cart is empty.';
      sidebarItems.appendChild(empty);
    } else {
      cart.items.forEach((item) => {
        const line = document.createElement('article');
        line.className = 'cart-mini-line';

        const image = document.createElement('img');
        image.src = item.imageUrl;
        image.alt = item.productName || 'Cart item';

        const info = document.createElement('div');
        info.className = 'cart-mini-info';
        const name = document.createElement('strong');
        name.textContent = item.productName;
        const meta = document.createElement('span');
        const sizeLabel = item.size ? `Size ${item.size}` : 'One size';
        meta.textContent = `${sizeLabel} - ${formatMoney(item.unitPrice, item.currency)}`;

        const actions = document.createElement('div');
        actions.className = 'cart-mini-actions';

        const qtyForm = document.createElement('form');
        qtyForm.className = 'mini-qty-form';
        qtyForm.method = 'post';
        qtyForm.action = '/Cart/Update';
        qtyForm.setAttribute('data-cart-form', '');
        qtyForm.setAttribute('data-auto-cart-form', '');

        const qtyId = document.createElement('input');
        qtyId.type = 'hidden';
        qtyId.name = 'cartItemId';
        qtyId.value = item.id;

        const stepper = document.createElement('div');
        stepper.className = 'mini-qty-stepper';
        stepper.setAttribute('role', 'group');
        stepper.setAttribute('aria-label', 'Adjust quantity');

        const minus = document.createElement('button');
        minus.type = 'button';
        minus.setAttribute('data-mini-qty-minus', '');
        minus.setAttribute('aria-label', 'Decrease quantity');
        minus.textContent = '-';

        const qtyInput = document.createElement('input');
        qtyInput.type = 'number';
        qtyInput.name = 'quantity';
        qtyInput.min = '1';
        qtyInput.value = item.quantity;
        qtyInput.setAttribute('data-cart-quantity', '');

        const plus = document.createElement('button');
        plus.type = 'button';
        plus.setAttribute('data-mini-qty-plus', '');
        plus.setAttribute('aria-label', 'Increase quantity');
        plus.textContent = '+';

        stepper.append(minus, qtyInput, plus);
        qtyForm.append(qtyId, stepper);

        const removeForm = document.createElement('form');
        removeForm.className = 'mini-remove-form';
        removeForm.method = 'post';
        removeForm.action = '/Cart/Remove';
        removeForm.setAttribute('data-cart-form', '');

        const removeId = document.createElement('input');
        removeId.type = 'hidden';
        removeId.name = 'cartItemId';
        removeId.value = item.id;

        const removeButton = document.createElement('button');
        removeButton.type = 'submit';
        removeButton.className = 'text-button mini-remove';
        removeButton.textContent = 'Remove';

        removeForm.append(removeId, removeButton);
        actions.append(qtyForm, removeForm);

        info.append(name, meta, actions);
        line.append(image, info);
        sidebarItems.appendChild(line);
      });
    }

    sidebarSubtotal?.replaceChildren(document.createTextNode(formatMoney(cart.subtotal, cart.currency)));
    sidebarCount?.replaceChildren(document.createTextNode(`${cart.count} item(s)`));
  }

  function showToast(message) {
    const toast = document.createElement('div');
    toast.className = 'toast is-transient';
    toast.textContent = message;
    toast.setAttribute('role', 'status');
    toast.setAttribute('aria-live', 'polite');
    document.body.appendChild(toast);
    requestAnimationFrame(() => toast.classList.add('is-visible'));
    window.setTimeout(() => {
      toast.classList.remove('is-visible');
      window.setTimeout(() => toast.remove(), 320);
    }, 2600);
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
    renderMiniCart(cart);

    if (form.hasAttribute('data-cart-add')) {
      showToast('Added to cart.');
      setSidebarOpen(true);
    }
  }

  sidebarClose?.addEventListener('click', () => setSidebarOpen(false));
  overlay?.addEventListener('click', () => setSidebarOpen(false));
  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      setSidebarOpen(false);
    }
  });

  document.addEventListener('submit', (event) => {
    const form = event.target.closest('[data-cart-form]');
    if (!form) return;
    event.preventDefault();
    submitCart(form);
  });

  document.addEventListener('click', (event) => {
    const minus = event.target.closest('[data-mini-qty-minus]');
    const plus = event.target.closest('[data-mini-qty-plus]');
    if (!minus && !plus) return;

    const stepper = (minus || plus).closest('.mini-qty-stepper');
    const input = stepper?.querySelector('[data-cart-quantity]');
    if (!input) return;

    const current = Number(input.value) || 1;
    const next = minus ? Math.max(1, current - 1) : current + 1;
    input.value = next;

    const form = input.closest('[data-auto-cart-form]');
    if (form) submitCart(form);
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
