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

(function () {
  const root = document.querySelector('[data-admin-notifications]')
    || document.querySelector('[data-notification-toggle]')?.closest('.admin-notifications');
  if (!root) return;

  const toggle = root.querySelector('[data-notification-toggle]');
  if (!toggle) return;

  let menu = root.querySelector('[data-notification-menu]');
  if (!menu) {
    menu = document.createElement('div');
    menu.className = 'admin-notification-menu';
    menu.setAttribute('data-notification-menu', '');
    menu.hidden = true;
    menu.innerHTML = `
      <div class="admin-notification-menu-head">
        <strong>Notifications</strong>
        <button type="button" data-notification-read-all>Mark all read</button>
      </div>
      <div class="admin-notification-list" data-notification-list>
        <p class="admin-notification-empty">No notifications yet.</p>
      </div>
    `;
    root.appendChild(menu);
  }

  const list = menu.querySelector('[data-notification-list]');
  const badge = root.querySelector('[data-notification-count]');
  const readAll = menu.querySelector('[data-notification-read-all]');

  let notifications = [];
  let unreadCount = 0;
  let audioContext;
  let audioUnlocked = false;

  const unlockAudio = () => {
    try {
      if (audioUnlocked) return;
      const AudioCtor = window.AudioContext || window.webkitAudioContext;
      if (!AudioCtor) return;
      audioContext = audioContext || new AudioCtor();
      if (audioContext.state === 'suspended') {
        audioContext.resume().catch(() => {});
      }
      audioUnlocked = true;
    } catch {
      audioContext = null;
    }
  };

  const ring = () => {
    toggle?.classList.add('is-ringing');
    window.setTimeout(() => toggle?.classList.remove('is-ringing'), 900);

    if (!audioUnlocked || !audioContext) return;
    try {
      const oscillator = audioContext.createOscillator();
      const gain = audioContext.createGain();
      oscillator.type = 'sine';
      oscillator.frequency.setValueAtTime(880, audioContext.currentTime);
      oscillator.frequency.exponentialRampToValueAtTime(660, audioContext.currentTime + 0.18);
      gain.gain.setValueAtTime(0.001, audioContext.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.16, audioContext.currentTime + 0.02);
      gain.gain.exponentialRampToValueAtTime(0.001, audioContext.currentTime + 0.28);
      oscillator.connect(gain).connect(audioContext.destination);
      oscillator.start();
      oscillator.stop(audioContext.currentTime + 0.3);
    } catch {
      audioContext = null;
      audioUnlocked = false;
    }
  };

  const updateBadge = () => {
    if (!badge) return;
    badge.hidden = unreadCount <= 0;
    badge.textContent = unreadCount > 99 ? '99+' : String(unreadCount);
  };

  const formatDate = (value) => {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    return date.toLocaleString([], { month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' });
  };

  const htmlEscapeMap = {
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;'
  };

  const escapeHtml = (value) => String(value || '').replace(/[&<>"']/g, (char) => htmlEscapeMap[char] || char);

  const iconFor = (type) => {
    if (type === 'order') return '#';
    if (type === 'review') return '*';
    return '@';
  };

  const render = () => {
    if (!list) return;
    if (notifications.length === 0) {
      list.innerHTML = '<p class="admin-notification-empty">No notifications yet.</p>';
      updateBadge();
      return;
    }

    list.innerHTML = notifications.map((item) => `
      <a class="admin-notification-item${item.isRead ? '' : ' is-unread'}" href="${escapeHtml(item.linkUrl)}" data-notification-id="${item.id}">
        <span class="admin-notification-type">${escapeHtml(iconFor(item.type))}</span>
        <span>
          <strong>${escapeHtml(item.title)}</strong>
          <small>${escapeHtml(item.body)}</small>
          <em>${escapeHtml(formatDate(item.createdAt))}</em>
        </span>
      </a>
    `).join('');

    updateBadge();
  };

  const addNotification = (item, shouldRing) => {
    const existing = notifications.find((current) => current.id === item.id);
    notifications = [item, ...notifications.filter((current) => current.id !== item.id)].slice(0, 12);
    if (!item.isRead && !existing) {
      unreadCount += 1;
    }
    render();
    if (shouldRing) ring();
  };

  const setMenuOpen = (isOpen) => {
    menu.hidden = !isOpen;
    menu.classList.toggle('is-open', isOpen);
    toggle.setAttribute('aria-expanded', String(isOpen));
  };

  const loadNotifications = async () => {
    try {
      const response = await fetch('/admin/notifications', { headers: { Accept: 'application/json' } });
      if (!response.ok || !response.headers.get('content-type')?.includes('application/json')) return;
      const data = await response.json();
      notifications = Array.isArray(data.items) ? data.items : [];
      unreadCount = Number(data.unreadCount || 0);
      render();
    } catch {
      // Keep the bell usable even if the network request fails.
    }
  };

  const connectStream = () => {
    if (!window.EventSource) return;
    try {
      const source = new EventSource('/admin/notifications/stream');
      source.addEventListener('notification', (event) => {
        try {
          addNotification(JSON.parse(event.data), true);
        } catch {
          // Ignore malformed stream payloads.
        }
      });
    } catch {
      // The initial list still works without the live stream.
    }
  };

  toggle.addEventListener('click', (event) => {
    event.preventDefault();
    event.stopPropagation();
    setMenuOpen(menu.hidden);
    unlockAudio();
  });

  document.addEventListener('click', (event) => {
    if (!root.contains(event.target)) {
      setMenuOpen(false);
    }
  });

  document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') {
      setMenuOpen(false);
    }
  });

  list?.addEventListener('click', (event) => {
    const link = event.target.closest('[data-notification-id]');
    if (!link) return;
    event.preventDefault();
    const id = link.dataset.notificationId;
    fetch(`/admin/notifications/${id}/read`, { method: 'POST' }).finally(() => {
      window.location.href = link.href;
    });
  });

  readAll?.addEventListener('click', () => {
    fetch('/admin/notifications/read-all', { method: 'POST' }).then(() => {
      unreadCount = 0;
      notifications = notifications.map((item) => ({ ...item, isRead: true }));
      render();
    });
  });

  document.addEventListener('pointerdown', unlockAudio, { once: true });
  document.addEventListener('keydown', unlockAudio, { once: true });

  loadNotifications();
  connectStream();
})();
