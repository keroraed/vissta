(function () {
  document.body.classList.remove('is-mobile-menu-open', 'is-cart-open');
  document.querySelector('[data-mobile-menu]')?.classList.remove('is-open');
  document.querySelector('[data-mobile-menu]')?.setAttribute('aria-hidden', 'true');

  const nav = document.querySelector('[data-nav]');
  const onScroll = () => {
    if (nav) nav.classList.toggle('is-scrolled', window.scrollY > 50);
  };
  onScroll();
  window.addEventListener('scroll', onScroll, { passive: true });

  const menu = document.querySelector('[data-mobile-menu]');
  document.querySelector('[data-mobile-open]')?.addEventListener('click', () => {
    menu?.classList.add('is-open');
    menu?.setAttribute('aria-hidden', 'false');
    document.body.classList.add('is-mobile-menu-open');
  });
  document.querySelector('[data-mobile-close]')?.addEventListener('click', () => {
    menu?.classList.remove('is-open');
    menu?.setAttribute('aria-hidden', 'true');
    document.body.classList.remove('is-mobile-menu-open');
  });
  document.getElementById('mobile-menu-overlay')?.addEventListener('click', () => {
    document.body.classList.remove('is-mobile-menu-open');
    menu?.classList.remove('is-open');
    menu?.setAttribute('aria-hidden', 'true');
  });

  const observer = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting) {
        entry.target.classList.add('is-visible');
        observer.unobserve(entry.target);
      }
    });
  }, { threshold: 0.18, rootMargin: '0px 0px -80px 0px' });

  document.querySelectorAll('.reveal').forEach((el) => observer.observe(el));
})();
