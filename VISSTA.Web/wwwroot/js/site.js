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
