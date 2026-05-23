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
