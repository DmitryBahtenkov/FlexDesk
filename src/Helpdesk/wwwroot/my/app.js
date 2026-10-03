const token = localStorage.getItem('hd_token');
if (!token) location.href = '/login.html';

function decodeUser() {
  try {
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    return payload.sub || 'guest';
  } catch {
    return 'guest';
  }
}

function relTime(iso) {
  if (!iso) return '—';
  const then = new Date(iso);
  if (Number.isNaN(then.getTime())) return String(iso);
  const sec = Math.max(0, (Date.now() - then.getTime()) / 1000);
  if (sec < 60) return 'just now';
  if (sec < 3600) return Math.floor(sec / 60) + ' min ago';
  if (sec < 86400) return Math.floor(sec / 3600) + ' hours ago';
  const days = Math.floor(sec / 86400);
  return days + (days === 1 ? ' day ago' : ' days ago');
}

function formatDate(iso) {
  if (!iso) return '—';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return String(iso);
  return d.toLocaleString();
}

function statusMeta(status) {
  const s = (status || 'open').toLowerCase();
  if (s === 'closed' || s === 'done' || s === 'completed') return { label: 'Completed', cls: 'status-done' };
  if (s === 'in_progress' || s === 'processing') return { label: "I'm working", cls: 'status-work' };
  return { label: 'Waiting for action', cls: 'status-wait' };
}

function setText(id, value) {
  const n = document.getElementById(id);
  if (n) n.textContent = value;
}

const username = decodeUser();
setText('header-username', username);
setText('sidebar-username', username);
const initial = (username[0] || 'G').toUpperCase();
setText('header-avatar', initial);
setText('sidebar-avatar', initial);


document.getElementById('logout')?.addEventListener('click', (e) => {
  e.preventDefault();
  localStorage.removeItem('hd_token');
  location.href = '/login.html';
});

document.getElementById('sidebar-toggle')?.addEventListener('click', () => {
  document.body.classList.toggle('sidebar-collapse');
});

document.getElementById('ticket-search')?.addEventListener('input', (e) => {
  const q = e.target.value.toLowerCase();
  document.querySelectorAll('#list tr').forEach((row) => {
    row.style.display = row.textContent.toLowerCase().includes(q) ? '' : 'none';
  });
});

fetch('/api/my-tickets', { headers: { Authorization: `Bearer ${token}` } })
  .then(async (r) => {
    if (r.status === 401) {
      location.href = '/login.html';
      return [];
    }
    return r.json();
  })
  .then((tickets) => {
    if (!Array.isArray(tickets)) return;

    tickets.forEach((t) => {
      t.body = t.body ?? t.Body;
    });

    const list = document.getElementById('list');
    if (!list) return;

    tickets.forEach((t) => {
      const id = t.id ?? t.Id;
      const subject = t.subject ?? t.Subject ?? '';
      const status = t.status ?? t.Status ?? 'open';
      const created = t.createdAt ?? t.CreatedAt;
      const meta = statusMeta(status);

      const tr = document.createElement('tr');

      const tdId = document.createElement('td');
      tdId.className = 'col-id';
      tdId.textContent = '#' + id;

      const tdSubject = document.createElement('td');
      tdSubject.className = 'ticket-subject';
      tdSubject.textContent = subject;

      const tdDate = document.createElement('td');
      const dateMain = document.createElement('div');
      dateMain.textContent = formatDate(created);
      const dateRel = document.createElement('div');
      dateRel.className = 'muted';
      dateRel.textContent = relTime(created);
      tdDate.append(dateMain, dateRel);

      const tdStatus = document.createElement('td');
      const badge = document.createElement('span');
      badge.className = 'status-pill ' + meta.cls;
      badge.textContent = meta.label;
      tdStatus.appendChild(badge);

      const tdBody = document.createElement('td');
      tdBody.className = 'col-body';
      const el = document.createElement('div');
      el.className = 'ticket-body';
      el.innerHTML = t.body;
      tdBody.appendChild(el);

      tr.append(tdId, tdSubject, tdDate, tdStatus, tdBody);
      list.appendChild(tr);
    });
  });
