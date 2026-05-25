(() => {
  const fileInput = document.querySelector('[data-image-upload]');
  const preview = document.querySelector('[data-image-preview]');
  if (!fileInput || !preview) return;

  fileInput.addEventListener('change', () => {
    preview.replaceChildren();
    Array.from(fileInput.files || []).forEach((file) => {
      if (!file.type.startsWith('image/')) return;
      const img = document.createElement('img');
      img.className = 'admin-image-thumb';
      img.alt = 'Selected image preview';
      img.src = URL.createObjectURL(file);
      img.onload = () => URL.revokeObjectURL(img.src);
      preview.appendChild(img);
    });
  });
})();

document.addEventListener('click', (event) => {
  const toggle = event.target.closest('[data-customer-toggle]');
  if (!toggle) return;

  const card = toggle.closest('.admin-review-card');
  const panel = card?.querySelector('[data-customer-panel]');
  if (!panel) return;

  const isVisible = panel.classList.toggle('is-visible');
  toggle.setAttribute('aria-expanded', String(isVisible));
});
