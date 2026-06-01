(function () {
  document.body.classList.remove('is-mobile-menu-open', 'is-cart-open');
  document.querySelector('[data-mobile-menu]')?.classList.remove('is-open');
  document.querySelector('[data-mobile-menu]')?.setAttribute('aria-hidden', 'true');
})();

const accountMenu = document.querySelector('[data-account-menu]');
const accountToggle = document.querySelector('[data-account-toggle]');

if (accountMenu && accountToggle) {
  accountToggle.addEventListener('click', () => {
    const isOpen = accountMenu.classList.toggle('is-open');
    accountToggle.setAttribute('aria-expanded', String(isOpen));
  });

  document.addEventListener('click', (event) => {
    if (!accountMenu.contains(event.target)) {
      accountMenu.classList.remove('is-open');
      accountToggle.setAttribute('aria-expanded', 'false');
    }
  });

  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      accountMenu.classList.remove('is-open');
      accountToggle.setAttribute('aria-expanded', 'false');
      accountToggle.focus();
    }
  });
}

document.querySelectorAll('[data-pdp-gallery]').forEach((gallery) => {
  const images = Array.from(gallery.querySelectorAll('[data-pdp-image]'));
  const previous = gallery.querySelector('[data-pdp-prev]');
  const next = gallery.querySelector('[data-pdp-next]');
  const count = gallery.querySelector('[data-pdp-count]');

  if (images.length <= 1) return;

  let activeIndex = 0;

  const applyState = () => {
    images.forEach((image, index) => {
      image.classList.remove('is-active', 'is-next', 'is-after', 'is-prev');

      const offset = (index - activeIndex + images.length) % images.length;
      if (offset === 0) {
        image.classList.add('is-active');
      } else if (offset === 1) {
        image.classList.add('is-next');
      } else if (offset === 2) {
        image.classList.add('is-after');
      } else if (offset === images.length - 1) {
        image.classList.add('is-prev');
      }
    });

    if (count) {
      count.textContent = `${activeIndex + 1} / ${images.length}`;
    }
  };

  previous?.addEventListener('click', () => {
    activeIndex = (activeIndex - 1 + images.length) % images.length;
    applyState();
  });

  next?.addEventListener('click', () => {
    activeIndex = (activeIndex + 1) % images.length;
    applyState();
  });

  gallery.addEventListener('keydown', (event) => {
    if (event.key === 'ArrowLeft') {
      previous?.click();
    }

    if (event.key === 'ArrowRight') {
      next?.click();
    }
  });

  applyState();
});

document.querySelectorAll('[data-review-carousel]').forEach((carousel) => {
  const section = carousel.closest('.reviews');
  const previous = section?.querySelector('[data-review-prev]');
  const next = section?.querySelector('[data-review-next]');

  const scrollByCard = (direction) => {
    const card = carousel.querySelector('.review-card');
    const distance = card ? card.getBoundingClientRect().width + 16 : carousel.clientWidth * 0.85;
    carousel.scrollBy({ left: direction * distance, behavior: 'smooth' });
  };

  previous?.addEventListener('click', () => scrollByCard(-1));
  next?.addEventListener('click', () => scrollByCard(1));
});

document.querySelectorAll('[data-buy-box]').forEach((box) => {
  const quantity = box.querySelector('[data-buy-quantity]');
  const note = box.querySelector('[data-size-stock-note]');
  const minus = box.querySelector('[data-qty-minus]');
  const plus = box.querySelector('[data-qty-plus]');

  const clampQuantity = () => {
    const max = Number(quantity?.max || 1);
    const current = Number(quantity?.value || 1);
    if (quantity) {
      quantity.value = String(Math.min(Math.max(current, 1), max));
    }
  };

  box.querySelectorAll('[data-size-option]').forEach((option) => {
    option.addEventListener('change', () => {
      const stock = Number(option.dataset.stock || 0);
      box.classList.add('is-size-selected');
      box.querySelector('[data-quantity-control]')?.setAttribute('aria-hidden', 'false');
      if (quantity) {
        quantity.max = String(stock);
        quantity.value = '1';
      }
      if (note) {
        note.textContent = `${stock} available in size ${option.value}.`;
      }
    });
  });

  minus?.addEventListener('click', () => {
    if (quantity) {
      quantity.value = String(Number(quantity.value || 1) - 1);
      clampQuantity();
    }
  });

  plus?.addEventListener('click', () => {
    if (quantity) {
      quantity.value = String(Number(quantity.value || 1) + 1);
      clampQuantity();
    }
  });
});

document.querySelectorAll('[data-edit-saved-address]').forEach((button) => {
  button.addEventListener('click', (event) => {
    event.preventDefault();
    event.stopPropagation();

    const form = button.closest('form');
    if (!form) return;

    const useSaved = form.querySelector('#UseSavedAddressTrue');
    const useNew = form.querySelector('#UseSavedAddressFalse');
    const fields = form.querySelector('[data-address-fields]');

    if (useNew) {
      useNew.checked = true;
      useNew.dispatchEvent(new Event('change', { bubbles: true }));
    } else if (useSaved) {
      useSaved.checked = false;
    }

    if (fields) {
      fields.dataset.prefill = 'true';
      fields.classList.add('is-visible');
    }

    const setValue = (name, value) => {
      const input = form.querySelector(`[name="ShippingAddress.${name}"]`);
      if (input) input.value = value || '';
    };

    setValue('Street', button.dataset.street);
    setValue('City', button.dataset.city);
    setValue('Governorate', button.dataset.governorate);
    setValue('PostalCode', button.dataset.postal);
    setValue('Country', button.dataset.country);
  });
});

document.querySelectorAll('[name="UseSavedAddress"]').forEach((radio) => {
  radio.addEventListener('change', () => {
    const form = radio.closest('form');
    if (!form) return;
    const fields = form.querySelector('[data-address-fields]');
    if (radio.id === 'UseSavedAddressTrue') {
      if (fields) {
        fields.classList.remove('is-visible');
        delete fields.dataset.prefill;
      }
      return;
    }

    if (!fields) return;

    fields.classList.add('is-visible');
    if (fields.dataset.prefill === 'true') {
      delete fields.dataset.prefill;
      return;
    }

    const setValue = (name, value) => {
      const input = form.querySelector(`[name="ShippingAddress.${name}"]`);
      if (input) input.value = value;
    };

    setValue('Street', '');
    setValue('City', '');
    setValue('Governorate', '');
    setValue('PostalCode', '');
    setValue('Country', 'Egypt');
  });
});

const checkoutForm = document.querySelector('[data-checkout-form]');
if (checkoutForm) {
  const couponInput = checkoutForm.querySelector('[data-coupon-input]');
  const token = checkoutForm.querySelector('input[name="__RequestVerificationToken"]')?.value;
  const subtotalEl = document.querySelector('[data-summary-subtotal]');
  const discountRow = document.querySelector('[data-summary-discount-row]');
  const discountEl = document.querySelector('[data-summary-discount]');
  const totalEl = document.querySelector('[data-summary-total]');
  const couponStatus = document.querySelector('[data-summary-coupon-status]');
  const paymentProof = checkoutForm.querySelector('[data-payment-proof]');
  const paymentMethods = checkoutForm.querySelectorAll('[data-payment-method]');

  const formatMoney = (value, currency) => `${Number(value).toLocaleString()} ${currency}`;

  const resetSummary = () => {
    if (!subtotalEl || !totalEl) return;
    const subtotal = Number(subtotalEl.dataset.subtotal || 0);
    const currency = subtotalEl.dataset.currency || '';
    totalEl.textContent = formatMoney(subtotal, currency);
    if (discountRow) {
      discountRow.classList.add('is-hidden');
    }
    couponStatus?.classList.remove('is-visible');
    if (discountEl) {
      discountEl.textContent = `-${formatMoney(0, currency)}`;
    }
    couponStatus?.classList.remove('is-visible');
  };

  const applySummary = (data) => {
    if (!subtotalEl || !totalEl) return;
    const currency = data.currency || subtotalEl.dataset.currency || '';
    const discount = Number(data.discountAmount || 0);
    const total = Number(data.totalAmount || subtotalEl.dataset.subtotal || 0);

    totalEl.textContent = formatMoney(total, currency);

    if (discount > 0 && discountRow && discountEl) {
      discountRow.classList.remove('is-hidden');
      discountEl.textContent = `-${formatMoney(discount, currency)}`;
      couponStatus?.classList.toggle('is-visible', Boolean(data.valid));
      return;
    }

    resetSummary();
  };

  let debounceTimer;
  let requestId = 0;

  const validateCoupon = async (code) => {
    if (!code) {
      resetSummary();
      return;
    }

    if (!token) {
      return;
    }

    const currentRequest = ++requestId;
    const response = await fetch('/Checkout/ValidateCoupon', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
        RequestVerificationToken: token
      },
      body: new URLSearchParams({ couponCode: code })
    });

    if (!response.ok || currentRequest !== requestId) {
      return;
    }

    const data = await response.json();
    applySummary(data || {});
  };

  const scheduleValidation = () => {
    if (!couponInput) return;
    window.clearTimeout(debounceTimer);
    debounceTimer = window.setTimeout(() => {
      validateCoupon(couponInput.value.trim());
    }, 350);
  };

  couponInput?.addEventListener('input', scheduleValidation);
  couponInput?.addEventListener('blur', scheduleValidation);

  if (couponInput?.value) {
    scheduleValidation();
  }

  const togglePaymentProof = () => {
    if (!paymentProof) return;
    const selected = checkoutForm.querySelector('[data-payment-method]:checked');
    const needsProof = selected?.value === 'InstaPayWallet';
    paymentProof.classList.toggle('is-hidden', !needsProof);
  };

  paymentMethods.forEach((input) => {
    input.addEventListener('change', togglePaymentProof);
  });
  togglePaymentProof();
}

/* ─── COLLECTION MEGA-DROPDOWN ──────────────────────────────────── */
(function () {
  const wrap = document.querySelector('[data-collection-nav]');
  const toggle = document.querySelector('[data-collection-toggle]');
  const panel = document.querySelector('[data-collection-panel]');

  if (!wrap || !toggle || !panel) return;

  const open = () => {
    wrap.classList.add('is-open');
    toggle.setAttribute('aria-expanded', 'true');
    panel.removeAttribute('aria-hidden');
  };

  const close = () => {
    wrap.classList.remove('is-open');
    toggle.setAttribute('aria-expanded', 'false');
    panel.setAttribute('aria-hidden', 'true');
  };

  toggle.addEventListener('click', (e) => {
    e.stopPropagation();
    wrap.classList.contains('is-open') ? close() : open();
  });

  // Close when clicking outside
  document.addEventListener('click', (e) => {
    if (!wrap.contains(e.target)) close();
  });

  // Close on Escape
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') { close(); toggle.focus(); }
  });

  // Keep open while hovering (desktop UX polish)
  wrap.addEventListener('mouseenter', open);
  wrap.addEventListener('mouseleave', close);
})();

/* ─── MOBILE COLLECTION TOGGLE ─────────────────────────────────── */
(function () {
  const group = document.querySelector('[data-mobile-collection]');
  const toggle = document.querySelector('[data-mobile-collection-toggle]');
  if (!group || !toggle) return;

  toggle.addEventListener('click', () => {
    if (group.classList.contains('is-open')) {
      // Already open → navigate to /collection
      window.location.href = '/collection';
    } else {
      // Closed → expand
      group.classList.add('is-open');
    }
  });

  // Collapse when mobile menu closes
  const mobileMenu = document.querySelector('[data-mobile-menu]');
  const mobileClose = document.querySelector('[data-mobile-close]');
  const mobileOpen = document.querySelector('[data-mobile-open]');

  if (mobileClose) {
    mobileClose.addEventListener('click', () => {
      group.classList.remove('is-open');
    });
  }
  if (mobileOpen) {
    mobileOpen.addEventListener('click', () => {
      // Reset to closed every time menu opens
      group.classList.remove('is-open');
    });
  }
})();

/* ─── RELATED PIECES CAROUSEL ──────────────────────────────────── */
(function () {
  const section = document.querySelector('.related');
  if (!section) return;

  const carousel = section.querySelector('.catalog-grid');
  const prevBtn = section.querySelector('[data-related-prev]');
  const nextBtn = section.querySelector('[data-related-next]');

  if (!carousel) return;

  const scrollByCard = (direction) => {
    const card = carousel.querySelector('.product-card');
    const distance = card ? card.getBoundingClientRect().width + 20 : carousel.clientWidth * 0.8;
    carousel.scrollBy({ left: direction * distance, behavior: 'smooth' });
  };

  prevBtn?.addEventListener('click', () => scrollByCard(-1));
  nextBtn?.addEventListener('click', () => scrollByCard(1));
})();
