onReady(() =>
{
	if (!$id('tab-smtp'))
		return;

	initTabs();
	refreshAll();
	setInterval(refreshAll, 5000);
});

// ── Tab navigation ─────────────────────────────────────────

function initTabs()
{
	$$('.tab-btn').on('click', function ()
	{
		$$('.tab-btn').removeClass('active');
		$$('.tab-panel').removeClass('active');
		this.addClass('active');
		$id(this.dataset.panel).addClass('active');
	});
}

// ── Refresh ────────────────────────────────────────────────

async function refreshAll()
{
	await Promise.all([refreshServices(), refreshSmtp(), refreshImap(), refreshPop3()]);
	pulse();
}

async function refreshServices()
{
	try
	{
		const d = await netproxyasync('/api/status/all');
		setSvcTab('tab-smtp', d.smtp);
		setSvcTab('tab-imap', d.imap);
		setSvcTab('tab-pop3', d.pop3);

		const running = [d.smtp, d.imap, d.pop3].filter(Boolean).length;
		$id('header-status').textContent = `${running}/3 running`;
	}
	catch
	{
		$id('header-status').textContent = 'unreachable';
	}
}

async function refreshSmtp()
{
	try
	{
		const d = await netproxyasync('/api/status/smtp');

		set('smtp-total-conn', d.connections.total);
		set('smtp-active',     d.connections.active);
		set('smtp-recv',       d.messages.received);
		set('smtp-last-msg',   d.messages.lastReceivedAt ? timeSince(d.messages.lastReceivedAt) : 'never');
		set('smtp-relayed',    d.messages.relayed);
		set('smtp-bytes',      formatBytes(d.messages.bytesReceived));
		set('smtp-uptime',     formatUptime(d.uptimeSeconds));
		set('smtp-started',    d.startTime ? 'since ' + fmtDate(d.startTime) : '');
		set('smtp-auth-ok',    d.auth.success);
		set('smtp-auth-fail',  d.auth.failed);

		const total = Math.max(d.rejections.total, 1);
		set('smtp-rej-spf',   d.rejections.spf);
		set('smtp-rej-dkim',  d.rejections.dkim);
		set('smtp-rej-dmarc', d.rejections.dmarc);
		setBar('bar-spf',    d.rejections.spf,  total);
		setBar('bar-dkim',   d.rejections.dkim,  total);
		setBar('bar-dmarc',  d.rejections.dmarc, total);

		renderUserTable('smtp-tbl-senders',    d.topSenders,    'domain');
		renderUserTable('smtp-tbl-recipients', d.topRecipients, 'domain');
	}
	catch (e) { console.error('SMTP refresh failed', e); }
}

async function refreshImap()
{
	try
	{
		const d = await netproxyasync('/api/status/imap');

		set('imap-total-conn',     d.connections.total);
		set('imap-active',         d.connections.active);
		set('imap-fetched',        d.messages.fetched);
		set('imap-last-activity',  d.messages.lastActivityAt ? timeSince(d.messages.lastActivityAt) : 'never');
		set('imap-bytes',          formatBytes(d.messages.bytesSent));
		set('imap-commands',       d.commands);
		set('imap-folder-selects', d.folderSelects);
		set('imap-uptime',         formatUptime(d.uptimeSeconds));
		set('imap-started',        d.startTime ? 'since ' + fmtDate(d.startTime) : '');
		set('imap-auth-ok',        d.auth.success);
		set('imap-auth-fail',      d.auth.failed);

		renderUserTable('imap-tbl-users', d.topUsers, 'user');
	}
	catch (e) { console.error('IMAP refresh failed', e); }
}

async function refreshPop3()
{
	try
	{
		const d = await netproxyasync('/api/status/pop3');

		set('pop3-total-conn',    d.connections.total);
		set('pop3-active',        d.connections.active);
		set('pop3-retrieved',     d.messages.retrieved);
		set('pop3-last-activity', d.messages.lastActivityAt ? timeSince(d.messages.lastActivityAt) : 'never');
		set('pop3-deleted',       d.messages.deleted);
		set('pop3-bytes',         formatBytes(d.messages.bytesSent));
		set('pop3-uptime',        formatUptime(d.uptimeSeconds));
		set('pop3-started',       d.startTime ? 'since ' + fmtDate(d.startTime) : '');
		set('pop3-auth-ok',       d.auth.success);
		set('pop3-auth-fail',     d.auth.failed);

		renderUserTable('pop3-tbl-users', d.topUsers, 'user');
	}
	catch (e) { console.error('POP3 refresh failed', e); }
}

// ── Helpers ────────────────────────────────────────────────

function setSvcTab(tabId, up)
{
	const el = $id(tabId);
	if (!el) return;
	el.classList.remove('svc-up', 'svc-down');
	el.classList.add(up ? 'svc-up' : 'svc-down');
}

function set(id, value)
{
	const el = $id(id);
	if (el) el.textContent = (value !== null && value !== undefined) ? value : '—';
}

function setBar(id, n, total)
{
	const el = $id(id);
	if (el) el.style.width = Math.min(100, Math.round((n / total) * 100)) + '%';
}

function renderUserTable(id, rows, key)
{
	const el = $id(id);
	if (!el) return;
	if (!rows || rows.length === 0)
	{
		el.innerHTML = '<div class="domain-card__empty">No data yet</div>';
		return;
	}
	let html = '<table>';
	for (const r of rows)
		html += `<tr><td>${esc(r[key])}</td><td>${r.count}</td></tr>`;
	html += '</table>';
	el.innerHTML = html;
}

function pulse()
{
	const dot = $id('refresh-dot');
	if (!dot) return;
	dot.classList.remove('pulse');
	void dot.offsetWidth;
	dot.classList.add('pulse');
	$id('refresh-time').textContent = new Date().toLocaleTimeString();
}

function formatBytes(b)
{
	if (b == null) return '—';
	if (b < 1024)         return b + ' B';
	if (b < 1048576)      return (b / 1024).toFixed(1) + ' KB';
	return (b / 1048576).toFixed(2) + ' MB';
}

function formatUptime(s)
{
	if (s == null) return '—';
	const d = Math.floor(s / 86400);
	const h = Math.floor((s % 86400) / 3600);
	const m = Math.floor((s % 3600) / 60);
	if (d > 0) return `${d}d ${h}h ${m}m`;
	if (h > 0) return `${h}h ${m}m`;
	return `${m}m ${s % 60}s`;
}

function timeSince(iso)
{
	const diff = Math.floor((Date.now() - new Date(iso)) / 1000);
	if (diff < 60)   return diff + 's ago';
	if (diff < 3600) return Math.floor(diff / 60) + 'm ago';
	return Math.floor(diff / 3600) + 'h ago';
}

function fmtDate(iso)
{
	return new Date(iso).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' });
}

function esc(str)
{
	return String(str ?? '').replace(/[&<>"']/g, c =>
		({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}
