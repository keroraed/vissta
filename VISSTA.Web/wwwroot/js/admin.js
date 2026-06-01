(function () {
  const sidebar = document.querySelector('[data-admin-sidebar]');
  const openButton = document.querySelector('[data-admin-menu-open]');
  const closeButton = document.querySelector('[data-admin-menu-close]');
  const overlay = document.querySelector('[data-admin-menu-overlay]');

  if (!sidebar || !openButton || !overlay) return;

  const syncDesktopState = () => {
    if (window.innerWidth > 1100) {
      sidebar.setAttribute('aria-hidden', 'false');
      openButton.setAttribute('aria-expanded', 'false');
      return true;
    }

    return false;
  };

  const setOpen = (isOpen) => {
    if (syncDesktopState()) {
      document.body.classList.remove('is-admin-menu-open');
      sidebar.classList.remove('is-open');
      return;
    }

    document.body.classList.toggle('is-admin-menu-open', isOpen);
    sidebar.classList.toggle('is-open', isOpen);
    sidebar.setAttribute('aria-hidden', String(!isOpen));
    openButton.setAttribute('aria-expanded', String(isOpen));
  };

  openButton.addEventListener('click', () => setOpen(true));
  closeButton?.addEventListener('click', () => setOpen(false));
  overlay.addEventListener('click', () => setOpen(false));

  sidebar.querySelectorAll('a').forEach((link) => {
    link.addEventListener('click', () => setOpen(false));
  });

  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      setOpen(false);
    }
  });

  window.addEventListener('resize', () => setOpen(false));

  if (!syncDesktopState()) {
    sidebar.setAttribute('aria-hidden', 'true');
  }
})();
