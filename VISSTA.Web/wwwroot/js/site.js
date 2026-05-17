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
