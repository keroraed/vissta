(() => {
  const toggles = document.querySelectorAll('[data-password-toggle]');

  toggles.forEach((toggle) => {
    const field = toggle.closest('.password-field');
    const input = field?.querySelector('[data-password-input]');

    if (!input || !field) {
      return;
    }

    const setState = (visible) => {
      input.type = visible ? 'text' : 'password';
      field.classList.toggle('is-visible', visible);
      toggle.setAttribute('aria-pressed', visible ? 'true' : 'false');
      toggle.setAttribute('aria-label', visible ? 'Hide password' : 'Show password');
    };

    toggle.addEventListener('click', () => {
      const show = input.type === 'password';
      setState(show);
    });
  });
})();
