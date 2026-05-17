(function () {
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
  });
  document.querySelector('[data-mobile-close]')?.addEventListener('click', () => {
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
