// ── Helpers ────────────────────────────────────────────────

function $id(id) { return document.getElementById(id); }
function $$(sel) { return Array.from(document.querySelectorAll(sel)); }

function esc(str)
{
	return String(str ?? '').replace(/[&<>"']/g, c =>
		({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

async function apiFetch(url, data)
{
	const opts = data
		? { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) }
		: { method: 'GET' };
	const res = await fetch(url, opts);
	if (!res.ok) throw new Error(`HTTP ${res.status}`);
	if (res.status === 204) return null;
	return res.json();
}

// ── Init ───────────────────────────────────────────────────

document.addEventListener('DOMContentLoaded', () =>
{
	initTheme();

	if (!$id('tab-smtp')) return;

	initTabs();
	refreshAll();
	setInterval(refreshAll, 5000);
});

// ── Theme ──────────────────────────────────────────────────

function initTheme()
{
	const dark = localStorage.getItem('theme') === 'dark';
	applyTheme(dark);
	const btn = $id('theme-toggle');
	if (btn) btn.addEventListener('click', () =>
	{
		const isDark = document.body.classList.toggle('dark');
		localStorage.setItem('theme', isDark ? 'dark' : 'light');
		btn.textContent = isDark ? '☾' : '☀';
	});
}

function applyTheme(dark)
{
	document.body.classList.toggle('dark', dark);
	const btn = $id('theme-toggle');
	if (btn) btn.textContent = dark ? '☾' : '☀';
}

// ── Tabs ───────────────────────────────────────────────────

function initTabs()
{
	$$('.tab-btn').forEach(btn => btn.addEventListener('click', function ()
	{
		$$('.tab-btn').forEach(b => b.classList.remove('active'));
		$$('.tab-panel').forEach(p => p.classList.remove('active'));
		this.classList.add('active');
		$id(this.dataset.panel).classList.add('active');
	}));

	$$('.inline-settings__toggle').forEach(btn => btn.addEventListener('click', function ()
	{
		const body = $id(this.dataset.target);
		const wrap = this.closest('.inline-settings');
		const opening = !wrap.classList.contains('open');
		wrap.classList.toggle('open');
		if (opening && body && (body.innerHTML.trim() === '' || !_cfg))
			loadInlineSettings(this.dataset.target);
	}));
}

// ── Status refresh ─────────────────────────────────────────

async function refreshAll()
{
	await Promise.all([refreshServices(), refreshSmtp(), refreshImap(), refreshPop3()]);
	pulse();
}

async function refreshServices()
{
	try
	{
		const d = await apiFetch('/api/status/all');
		setSvcTab('tab-smtp', d.smtp);
		setSvcTab('tab-imap', d.imap);
		setSvcTab('tab-pop3', d.pop3);
		const running = [d.smtp, d.imap, d.pop3].filter(Boolean).length;
		$id('header-status').textContent = `${running}/3 running`;
	}
	catch { $id('header-status').textContent = 'unreachable'; }
}

async function refreshSmtp()
{
	try
	{
		const d = await apiFetch('/api/status/smtp');
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
		setBar('bar-spf',   d.rejections.spf,  total);
		setBar('bar-dkim',  d.rejections.dkim,  total);
		setBar('bar-dmarc', d.rejections.dmarc, total);
		renderUserTable('smtp-tbl-senders',    d.topSenders,    'domain');
		renderUserTable('smtp-tbl-recipients', d.topRecipients, 'domain');
	}
	catch (e) { console.error('SMTP refresh failed', e); }
}

async function refreshImap()
{
	try
	{
		const d = await apiFetch('/api/status/imap');
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
		const d = await apiFetch('/api/status/pop3');
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

// ── Settings load ──────────────────────────────────────────

let _cfg = null;

async function fetchConfig()
{
	if (!_cfg) _cfg = await apiFetch('/api/config');
	return _cfg;
}

function wireSettingsContainer(el)
{
	el.addEventListener('click', function (e)
	{
		if (e.target.classList.contains('btn-add-port'))
		{
			const tbody = $id(e.target.dataset.prefix + '-ports-tbody');
			if (tbody) tbody.insertAdjacentHTML('beforeend', portRow({}));
		}
		if (e.target.classList.contains('btn-remove-port'))
		{
			e.target.closest('tr').remove();
		}
		if (e.target.classList.contains('btn-save'))
		{
			const key = e.target.dataset.save;
			const msg = $id('msg-' + key);
			e.target.disabled = true;
			saveConfig(key)
				.then(() => { _cfg = null; showMsg(msg, true, 'Saved'); })
				.catch(err => showMsg(msg, false, err.message || 'Error'))
				.finally(() => { e.target.disabled = false; });
		}
	});
}

async function loadInlineSettings(bodyId)
{
	const el = $id(bodyId);
	if (!el) return;
	el.innerHTML = '<p class="cfg-loading">Loading…</p>';
	try
	{
		const cfg = await fetchConfig();
		if      (bodyId === 'cfg-smtp-body') el.innerHTML = buildCfgSmtp(cfg.smtp, cfg.dmarc, cfg.mailbox);
		else if (bodyId === 'cfg-imap-body') el.innerHTML = buildCfgImap(cfg.imap);
		else if (bodyId === 'cfg-pop3-body') el.innerHTML = buildCfgPop3(cfg.pop3);
		wireSettingsContainer(el);
	}
	catch (e) { el.innerHTML = `<p style="color:var(--red)">${esc(e.message)}</p>`; }
}

async function saveConfig(key)
{
	switch (key)
	{
		case 'smtp':    return apiFetch('/api/config/smtp',    collectSmtp());
		case 'pop3':    return apiFetch('/api/config/pop3',    collectPop3());
		case 'imap':    return apiFetch('/api/config/imap',    collectImap());
		case 'dmarc':   return apiFetch('/api/config/dmarc',   collectDmarc());
		case 'mailbox': return apiFetch('/api/config/mailbox', collectMailbox());
	}
}

// ── SMTP panel ─────────────────────────────────────────────

function buildCfgSmtp(s, dmarc, mailbox)
{
	const secOpts = ['None', 'StartTls', 'Tls'].map(v =>
		`<option value="${v}"${s.relayConnectionSecurity === v ? ' selected' : ''}>${v}</option>`
	).join('');

	return `
	<div class="cfg-section-title">General</div>
	<div class="form-grid">
		${field('smtp-emlpath',  'Email storage path',   s.emlStoragePath)}
		${field('smtp-userstore','User store path',       s.userStorePath)}
		${field('smtp-certpath', 'Certificate path',      s.certificatePath)}
		${field('smtp-certpass', 'Certificate password',  s.certificatePassword, 'password')}
		${field('smtp-timeout',  'Command timeout (sec)', s.commandTimeoutSeconds, 'number')}
		${field('smtp-backlog',  'Backlog',                s.backLog, 'number')}
	</div>
	<div class="form-grid">
		${textarea('smtp-dns',     'DNS Resolvers (one per line)', (s.dnsResolvers || []).join('\n'))}
		${textarea('smtp-domains', 'Local domains (one per line)', (s.localDomains  || []).join('\n'))}
	</div>

	<div class="cfg-section-title">Connections</div>
	<div class="form-grid">
		${field('smtp-maxconn', 'Maximum simultaneous connections (0 for unlimited)', s.maxConnections, 'number')}
	</div>

	<div class="cfg-section-title">Other</div>
	<div class="form-grid">
		${field('smtp-welcome', 'Welcome message',       s.welcomeMessage)}
		${field('smtp-maxmsg',  'Max message size (KB)', s.maxMessageSizeKb, 'number')}
	</div>

	<div class="cfg-section-title">Delivery of e-mail</div>
	<div class="form-grid">
		${field('smtp-retries',       'Number of retries',           s.retryCount,           'number')}
		${field('smtp-retryinterval', 'Minutes between every retry', s.retryIntervalMinutes, 'number')}
		${field('smtp-localhost',     'Local host name',             s.localHostName)}
	</div>

	<div class="cfg-section-title">SMTP Relayer</div>
	<div class="toggle-list">
		${toggle('smtp-relay', 'Enable relay', 'Forward outgoing mail to an external SMTP server', s.relayEnabled)}
	</div>
	<div class="form-grid">
		${field('smtp-relayhost',  'Remote host name',   s.relayHost)}
		${field('smtp-relayport',  'Remote TCP/IP port', s.relayPort, 'number')}
		${field('smtp-relayqueue', 'Relay queue path',   s.relayQueuePath)}
	</div>
	<div class="toggle-list">
		${toggle('smtp-relayauth', 'Server requires authentication', 'Authenticate when relaying', s.relayRequiresAuth)}
	</div>
	<div class="form-grid">
		${field('smtp-relayuser', 'User name', s.relayUsername)}
		${field('smtp-relaypass', 'Password',  s.relayPassword, 'password')}
	</div>
	<div class="form-field">
		<label for="smtp-relaysec">Connection security</label>
		<select id="smtp-relaysec">${secOpts}</select>
	</div>

	<div class="cfg-section-title">RFC compliance</div>
	<div class="toggle-list">
		${toggle('smtp-plainauth',   'Allow plain text authentication',           'Allow AUTH PLAIN and AUTH LOGIN',           s.allowPlainTextAuth)}
		${toggle('smtp-emptysender', 'Allow empty sender address',                'Accept MAIL FROM:<>',                       s.allowEmptySender)}
		${toggle('smtp-badlineends', 'Allow incorrectly formatted line endings',  'Accept bare CR or LF in message data',      s.allowBadLineEndings)}
		${toggle('smtp-discbadcmds', 'Disconnect after too many invalid commands','Close connection on repeated bad commands',  s.disconnectOnTooManyInvalidCommands)}
	</div>
	<div class="form-grid">
		${field('smtp-maxbadcmds', 'Maximum number of invalid commands', s.maxInvalidCommands, 'number')}
	</div>

	<div class="cfg-section-title">Advanced</div>
	<div class="form-grid">
		${field('smtp-bindip',        'Bind to local IP address',          s.bindToLocalIp)}
		${field('smtp-maxrecipients', 'Maximum recipients in batch',       s.maxRecipientsPerBatch, 'number')}
		${field('smtp-ruleloop',      'Rule loop limit',                   s.ruleLoopLimit,         'number')}
		${field('smtp-maxrechosts',   'Maximum number of recipient hosts', s.maxRecipientHosts,     'number')}
	</div>
	<div class="toggle-list">
		${toggle('smtp-deliveredto', 'Add Delivered-To header', 'Insert Delivered-To header in relayed messages', s.addDeliveredToHeader)}
	</div>

	<div class="cfg-section-title">Auth / DKIM</div>
	<div class="toggle-list">
		${toggle('smtp-auth',     'Enable AUTH',     'Allow SMTP authentication',     s.enableAuth)}
		${toggle('smtp-starttls', 'Enable STARTTLS', 'Advertise STARTTLS in EHLO',    s.enableStartTls)}
		${toggle('smtp-vrfy',     'Enable VRFY',     'Allow address verification',     s.enableVrfy)}
		${toggle('smtp-expn',     'Enable EXPN',     'Allow mailing list expansion',   s.enableExpn)}
		${toggle('smtp-dkim',     'Require DKIM',    'Reject mail without valid DKIM', s.requireDkim)}
	</div>

	<div class="cfg-section-title">Ports <span class="cfg-restart-note">&#9888; restart required</span></div>
	${buildPorts('smtp', s.ports)}

	<div class="cfg-footer">
		<button class="btn-save" data-save="smtp">Save SMTP</button>
		<span class="save-msg" id="msg-smtp"></span>
	</div>

	<div class="cfg-section-title" style="margin-top:28px">DMARC</div>
	<div class="toggle-list">
		${toggle('dmarc-failopen', 'Fail open',     'Accept mail when DMARC lookup fails', dmarc.failOpen)}
		${toggle('dmarc-require',  'Require DMARC', 'Reject mail that fails DMARC policy', dmarc.requireDmarc)}
	</div>
	<div class="cfg-footer">
		<button class="btn-save" data-save="dmarc">Save DMARC</button>
		<span class="save-msg" id="msg-dmarc"></span>
	</div>

	<div class="cfg-section-title" style="margin-top:28px">Mailbox</div>
	<div class="form-grid">
		${field('mailbox-path', 'Storage path', mailbox.storagePath)}
	</div>
	<div class="cfg-footer">
		<button class="btn-save" data-save="mailbox">Save Mailbox</button>
		<span class="save-msg" id="msg-mailbox"></span>
	</div>`;
}

// ── POP3 panel ─────────────────────────────────────────────

function buildCfgPop3(s)
{
	return `
	<div class="cfg-section-title">General</div>
	<div class="form-grid">
		${field('pop3-certpath', 'Certificate path',     s.certificatePath)}
		${field('pop3-certpass', 'Certificate password', s.certificatePassword, 'password')}
	</div>

	<div class="cfg-section-title">Connections</div>
	<div class="form-grid">
		${field('pop3-maxconn', 'Maximum simultaneous connections (0 for unlimited)', s.maxConnections, 'number')}
	</div>

	<div class="cfg-section-title">Other</div>
	<div class="form-grid">
		${field('pop3-welcome', 'Welcome message', s.welcomeMessage)}
	</div>

	<div class="cfg-section-title">Ports <span class="cfg-restart-note">&#9888; restart required</span></div>
	${buildPorts('pop3', s.ports)}
	<div class="cfg-footer">
		<button class="btn-save" data-save="pop3">Save POP3</button>
		<span class="save-msg" id="msg-pop3"></span>
	</div>`;
}

// ── IMAP panel ─────────────────────────────────────────────

function buildCfgImap(s)
{
	const delimOpts = ['.', '/', '\\'].map(d =>
		`<option value="${d}"${s.hierarchyDelimiter === d ? ' selected' : ''}>${d}</option>`
	).join('');

	return `
	<div class="cfg-section-title">General</div>
	<div class="form-grid">
		${field('imap-certpath', 'Certificate path',     s.certificatePath)}
		${field('imap-certpass', 'Certificate password', s.certificatePassword, 'password')}
	</div>

	<div class="cfg-section-title">Connections</div>
	<div class="form-grid">
		${field('imap-maxconn', 'Maximum simultaneous connections (0 for unlimited)', s.maxConnections, 'number')}
	</div>

	<div class="cfg-section-title">Public Folders</div>
	<div class="form-grid">
		${field('imap-publicfolder', 'Public folder name', s.publicFolderName)}
	</div>

	<div class="cfg-section-title">Advanced</div>
	<div class="toggle-list">
		${toggle('imap-sort',  'IMAP Sort',  'Enable SORT extension',  s.enableSort)}
		${toggle('imap-quota', 'IMAP Quota', 'Enable QUOTA extension', s.enableQuota)}
		${toggle('imap-idle',  'IMAP IDLE',  'Enable IDLE extension',  s.enableIdle)}
		${toggle('imap-acl',   'IMAP ACL',   'Enable ACL extension',   s.enableAcl)}
	</div>

	<div class="cfg-section-title">Other</div>
	<div class="form-grid">
		${field('imap-welcome', 'Welcome message', s.welcomeMessage)}
	</div>
	<div class="form-field">
		<label for="imap-delim">Hierarchy delimiter</label>
		<select id="imap-delim">${delimOpts}</select>
	</div>

	<div class="cfg-section-title">Ports <span class="cfg-restart-note">&#9888; restart required</span></div>
	${buildPorts('imap', s.ports)}
	<div class="cfg-footer">
		<button class="btn-save" data-save="imap">Save IMAP</button>
		<span class="save-msg" id="msg-imap"></span>
	</div>`;
}

// ── Ports editor ───────────────────────────────────────────

const SEC_OPTIONS = [
	{ value: 'None',             label: 'None — plain' },
	{ value: 'StartTlsOptional', label: 'STARTTLS optional' },
	{ value: 'StartTls',         label: 'STARTTLS required' },
	{ value: 'Tls',              label: 'TLS (implicit)' }
];

function portRow(p)
{
	const opts = SEC_OPTIONS.map(o =>
		`<option value="${o.value}"${(p.security ?? 'None') === o.value ? ' selected' : ''}>${o.label}</option>`
	).join('');
	return `<tr>
		<td><input type="text"   class="port-host" value="${esc(p.host ?? '0.0.0.0')}"></td>
		<td><input type="number" class="port-port" value="${p.port ?? ''}" min="1" max="65535"></td>
		<td><select class="port-sec">${opts}</select></td>
		<td><button type="button" class="btn-remove-port" title="Remove">&#x2715;</button></td>
	</tr>`;
}

function buildPorts(prefix, ports)
{
	const rows = (ports || []).map(p => portRow(p)).join('');
	return `
	<table class="ports-table">
		<thead><tr><th>Host / IP</th><th>Port</th><th>Security</th><th></th></tr></thead>
		<tbody id="${prefix}-ports-tbody">${rows}</tbody>
	</table>
	<div class="ports-actions">
		<button type="button" class="btn-add-port" data-prefix="${prefix}">+ Add port</button>
	</div>`;
}

// ── Collectors ─────────────────────────────────────────────

function collectSmtp()
{
	return {
		emlStoragePath:        val('smtp-emlpath'),
		userStorePath:         val('smtp-userstore'),
		certificatePath:       val('smtp-certpath'),
		certificatePassword:   val('smtp-certpass'),
		commandTimeoutSeconds: int('smtp-timeout'),
		backLog:               int('smtp-backlog'),
		dnsResolvers:          lines('smtp-dns'),
		localDomains:          lines('smtp-domains'),
		maxConnections:        int('smtp-maxconn'),
		welcomeMessage:        val('smtp-welcome'),
		maxMessageSizeKb:      int('smtp-maxmsg'),
		retryCount:            int('smtp-retries'),
		retryIntervalMinutes:  int('smtp-retryinterval'),
		localHostName:         val('smtp-localhost'),
		relayEnabled:          chk('smtp-relay'),
		relayHost:             val('smtp-relayhost'),
		relayPort:             int('smtp-relayport'),
		relayQueuePath:        val('smtp-relayqueue'),
		relayRequiresAuth:     chk('smtp-relayauth'),
		relayUsername:         val('smtp-relayuser'),
		relayPassword:         val('smtp-relaypass'),
		relayConnectionSecurity: val('smtp-relaysec'),
		allowPlainTextAuth:    chk('smtp-plainauth'),
		allowEmptySender:      chk('smtp-emptysender'),
		allowBadLineEndings:   chk('smtp-badlineends'),
		disconnectOnTooManyInvalidCommands: chk('smtp-discbadcmds'),
		maxInvalidCommands:    int('smtp-maxbadcmds'),
		bindToLocalIp:         val('smtp-bindip'),
		maxRecipientsPerBatch: int('smtp-maxrecipients'),
		addDeliveredToHeader:  chk('smtp-deliveredto'),
		ruleLoopLimit:         int('smtp-ruleloop'),
		maxRecipientHosts:     int('smtp-maxrechosts'),
		enableAuth:            chk('smtp-auth'),
		enableStartTls:        chk('smtp-starttls'),
		enableVrfy:            chk('smtp-vrfy'),
		enableExpn:            chk('smtp-expn'),
		requireDkim:           chk('smtp-dkim'),
		ports:                 collectPorts('smtp')
	};
}

function collectPop3()
{
	return {
		certificatePath:     val('pop3-certpath'),
		certificatePassword: val('pop3-certpass'),
		ports:               collectPorts('pop3'),
		maxConnections:      int('pop3-maxconn'),
		welcomeMessage:      val('pop3-welcome')
	};
}

function collectImap()
{
	return {
		certificatePath:     val('imap-certpath'),
		certificatePassword: val('imap-certpass'),
		ports:               collectPorts('imap'),
		maxConnections:      int('imap-maxconn'),
		welcomeMessage:      val('imap-welcome'),
		publicFolderName:    val('imap-publicfolder'),
		enableSort:          chk('imap-sort'),
		enableQuota:         chk('imap-quota'),
		enableIdle:          chk('imap-idle'),
		enableAcl:           chk('imap-acl'),
		hierarchyDelimiter:  val('imap-delim')
	};
}

function collectDmarc()
{
	return { failOpen: chk('dmarc-failopen'), requireDmarc: chk('dmarc-require') };
}

function collectMailbox()
{
	return { storagePath: val('mailbox-path') };
}

function collectPorts(prefix)
{
	const tbody = $id(`${prefix}-ports-tbody`);
	if (!tbody) return [];
	return Array.from(tbody.querySelectorAll('tr')).map(tr => ({
		host:     tr.querySelector('.port-host')?.value ?? '',
		port:     parseInt(tr.querySelector('.port-port')?.value) || 0,
		security: tr.querySelector('.port-sec')?.value ?? 'None'
	}));
}

// ── Form helpers ───────────────────────────────────────────

function field(id, label, value, type = 'text')
{
	return `<div class="form-field">
		<label for="${id}">${esc(label)}</label>
		<input type="${type}" id="${id}" value="${esc(String(value ?? ''))}">
	</div>`;
}

function textarea(id, label, value)
{
	return `<div class="form-field">
		<label for="${id}">${esc(label)}</label>
		<textarea id="${id}">${esc(value ?? '')}</textarea>
	</div>`;
}

function toggle(id, label, sub, checked)
{
	return `<div class="toggle-row">
		<div>
			<div class="toggle-row__label">${esc(label)}</div>
			<div class="toggle-row__sub">${esc(sub)}</div>
		</div>
		<label class="toggle">
			<input type="checkbox" id="${id}"${checked ? ' checked' : ''}>
			<span class="toggle-track"></span>
		</label>
	</div>`;
}

function showMsg(el, ok, text)
{
	if (!el) return;
	el.textContent = text;
	el.className = 'save-msg ' + (ok ? 'show-ok' : 'show-err');
	setTimeout(() => { el.className = 'save-msg'; }, 3000);
}

function val(id)   { const el = $id(id); return el ? el.value : ''; }
function int(id)   { return parseInt(val(id)) || 0; }
function chk(id)   { const el = $id(id); return el ? el.checked : false; }
function lines(id) { return val(id).split('\n').map(s => s.trim()).filter(Boolean); }

// ── Shared helpers ─────────────────────────────────────────

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
	if (el) el.textContent = (value != null) ? value : '—';
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
	if (!rows || rows.length === 0) { el.innerHTML = '<div class="domain-card__empty">No data yet</div>'; return; }
	let html = '<table>';
	for (const r of rows) html += `<tr><td>${esc(r[key])}</td><td>${r.count}</td></tr>`;
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
	if (b < 1024)    return b + ' B';
	if (b < 1048576) return (b / 1024).toFixed(1) + ' KB';
	return (b / 1048576).toFixed(2) + ' MB';
}

function formatUptime(s)
{
	if (s == null) return '—';
	const d = Math.floor(s / 86400), h = Math.floor((s % 86400) / 3600), m = Math.floor((s % 3600) / 60);
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
