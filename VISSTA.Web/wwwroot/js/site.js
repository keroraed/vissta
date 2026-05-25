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
