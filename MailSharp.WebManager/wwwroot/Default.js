// ── Helpers ────────────────────────────────────────────────

function $id(id) { return document.getElementById(id); }
function $$(sel) { return Array.from(document.querySelectorAll(sel)); }

function esc(str)
{
	return String(str ?? '').replace(/[&<>"']/g, c =>
		({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

async function apiFetch(url, data, method)
{
	let opts;
	if (method === 'DELETE')
		opts = { method: 'DELETE' };
	else if (data != null)
		opts = { method: method ?? 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) };
	else
		opts = { method: method ?? 'GET' };
	const res = await fetch(url, opts);
	if (!res.ok) throw new Error(`HTTP ${res.status}`);
	const ct = res.headers.get('content-type') ?? '';
	if (res.status === 204 || !ct.includes('application/json')) return null;
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
	initDomains();
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
		const [d, cfg] = await Promise.all([apiFetch('/api/status/all'), fetchConfig()]);
		applyServiceDot('tab-smtp', d.smtp, cfg.smtp?.enabled ?? true);
		applyServiceDot('tab-imap', d.imap, cfg.imap?.enabled ?? true);
		applyServiceDot('tab-pop3', d.pop3, cfg.pop3?.enabled ?? true);
		const running = [d.smtp, d.imap, d.pop3].filter(Boolean).length;
		$id('header-status').textContent = `${running}/3 running`;
	}
	catch { $id('header-status').textContent = 'unreachable'; }
}

function applyServiceDot(tabId, running, enabled)
{
	const el = $id(tabId);
	if (!el) return;
	el.classList.remove('svc-up', 'svc-down', 'svc-disabled');
	if (!enabled)  el.classList.add('svc-disabled');
	else if (running) el.classList.add('svc-up');
	else           el.classList.add('svc-down');
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
	el.addEventListener('change', function (e)
	{
		const t = e.target;
		if (t.id === 'smtp-enabled') { saveServiceEnabled('smtp', t.checked); return; }
		if (t.id === 'pop3-enabled') { saveServiceEnabled('pop3', t.checked); return; }
		if (t.id === 'imap-enabled') { saveServiceEnabled('imap', t.checked); return; }
		if (t.classList.contains('port-enabled')) {
			const row = t.closest('tr');
			if (row) row.classList.toggle('port-row-disabled', !t.checked);
		}
	});

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

async function saveServiceEnabled(svc, enabled)
{
	_cfg = null;
	try
	{
		await saveConfig(svc);
		// Optimistically update dot, then confirm via live status
		const el = $id(`tab-${svc}`);
		if (el)
		{
			el.classList.remove('svc-up', 'svc-down', 'svc-disabled');
			el.classList.add(enabled ? 'svc-up' : 'svc-disabled');
		}
		setTimeout(refreshServices, 800);
	}
	catch (e) { console.error(`Failed to save ${svc} enabled:`, e); }
}

// ── SMTP panel ─────────────────────────────────────────────

function buildCfgSmtp(s, dmarc, mailbox)
{
	const secOpts = ['None', 'StartTls', 'Tls'].map(v =>
		`<option value="${v}"${s.relayConnectionSecurity === v ? ' selected' : ''}>${v}</option>`
	).join('');

	return `
	<div class="toggle-list">
		${toggle('smtp-enabled', 'Enable SMTP service', 'Start the SMTP server on startup', s.enabled ?? true)}
	</div>
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
		<div class="form-field">
			<label for="smtp-relaysec">Connection security</label>
			<select id="smtp-relaysec">${secOpts}</select>
		</div>
	</div>
	<div class="toggle-list">
		${toggle('smtp-relayauth', 'Server requires authentication', 'Authenticate when relaying', s.relayRequiresAuth)}
	</div>
	<div class="form-grid">
		${field('smtp-relayuser', 'User name', s.relayUsername)}
		${field('smtp-relaypass', 'Password',  s.relayPassword, 'password')}
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
	<div class="toggle-list">
		${toggle('pop3-enabled', 'Enable POP3 service', 'Start the POP3 server on startup', s.enabled ?? true)}
	</div>
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
	<div class="toggle-list">
		${toggle('imap-enabled', 'Enable IMAP service', 'Start the IMAP server on startup', s.enabled ?? true)}
	</div>
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
	const enabled = p.enabled ?? true;
	return `<tr class="${enabled ? '' : 'port-row-disabled'}">
		<td><input type="text"   class="port-host" value="${esc(p.host ?? '0.0.0.0')}"></td>
		<td><input type="number" class="port-port" value="${p.port ?? ''}" min="1" max="65535"></td>
		<td><select class="port-sec">${opts}</select></td>
		<td><label class="port-toggle-wrap" title="${enabled ? 'Disable' : 'Enable'} this port">
			<input type="checkbox" class="port-enabled" ${enabled ? 'checked' : ''}>
			<span class="port-toggle-track"></span>
		</label></td>
		<td><button type="button" class="btn-remove-port" title="Remove">&#x2715;</button></td>
	</tr>`;
}

function buildPorts(prefix, ports)
{
	const rows = (ports || []).map(p => portRow(p)).join('');
	return `
	<table class="ports-table">
		<thead><tr><th>Host / IP</th><th>Port</th><th>Security</th><th></th><th></th></tr></thead>
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
		enabled:               chk('smtp-enabled'),
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
		enabled:             chk('pop3-enabled'),
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
		enabled:             chk('imap-enabled'),
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
		security: tr.querySelector('.port-sec')?.value ?? 'None',
		enabled:  tr.querySelector('.port-enabled')?.checked ?? true
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

// ── Domain manager ─────────────────────────────────────────

let _domains = [];
const _openSections = {};   // id → active sub-tab name

const DOM_TABS = ['General', 'Users', 'User Aliases', 'Email Lists', 'Limits', 'DKIM', 'Domein-aliases'];

function initDomains()
{
	const addBtn   = $id('dom-add-btn');
	const addInput = $id('dom-new-name');
	if (!addBtn) return;

	addBtn.addEventListener('click', async () =>
	{
		const name = addInput.value.trim();
		if (!name) return;
		try
		{
			await apiFetch('/api/domain', { name });
			addInput.value = '';
			await loadDomains();
			showMsg($id('msg-domains'), true, 'Domain added');
		}
		catch (e) { showMsg($id('msg-domains'), false, e.message || 'Error'); }
	});
	addInput.addEventListener('keydown', e => { if (e.key === 'Enter') addBtn.click(); });

	// delegated events on #dom-list
	const list = $id('dom-list');
	if (!list) return;
	list.addEventListener('click',  domListClick);
	list.addEventListener('change', domListChange);
	list.addEventListener('keydown', domListKeydown);
}

function domListClick(e)
{
	const t = e.target;
	const id = t.dataset.id;

	if (t.dataset.action === 'delete-domain')       { deleteDomain(id); return; }
	if (t.dataset.action === 'toggle-expand')        { toggleExpand(id ?? t.closest('.dom-card')?.id?.replace('dom-card-','')); return; }
	if (t.dataset.action === 'sub-tab')              { setSubTab(id, t.dataset.tab); return; }
	if (t.dataset.action === 'save-general')         { saveGeneral(id); return; }
	if (t.dataset.action === 'save-limits')          { saveLimits(id); return; }
	if (t.dataset.action === 'save-dkim')            { saveDkim(id); return; }
	if (t.dataset.action === 'add-alias')            { apiAddAlias(id); return; }
	if (t.dataset.action === 'remove-alias')         { apiRemoveAlias(id, t.dataset.value); return; }
	if (t.dataset.action === 'add-ualias')           { apiAddUserAlias(id); return; }
	if (t.dataset.action === 'remove-ualias')        { apiRemoveUserAlias(id, t.dataset.alias); return; }
	if (t.dataset.action === 'add-list')             { apiAddList(id); return; }
	if (t.dataset.action === 'remove-list')          { apiRemoveList(id, t.dataset.listid); return; }
	if (t.dataset.action === 'add-list-member')      { apiAddListMember(id, t.dataset.listid); return; }
	if (t.dataset.action === 'remove-list-member')   { apiRemoveListMember(id, t.dataset.listid, t.dataset.value); return; }
	if (t.dataset.action === 'add-user')             { apiAddUser(id); return; }
	if (t.dataset.action === 'save-user')            { apiSaveUser(id, t.dataset.userid); return; }
	if (t.dataset.action === 'remove-user')          { apiRemoveUser(id, t.dataset.userid); return; }
}

function domListChange(e)
{
	const t = e.target;
	if (t.dataset.action === 'toggle-enabled')
	{
		const d = _domains.find(x => x.id === t.dataset.id);
		if (!d) return;
		d.enabled = t.checked;
		const dot = $id(`dom-dot-${d.id}`);
		if (dot)
		{
			dot.classList.toggle('dom-status--on',  d.enabled);
			dot.classList.toggle('dom-status--off', !d.enabled);
		}
		saveGeneral(d.id);
	}
}

function domListKeydown(e)
{
	if (e.key !== 'Enter') return;
	const t = e.target;
	if (t.dataset.enter === 'add-alias')   { apiAddAlias(t.dataset.id); return; }
	if (t.dataset.enter === 'add-user')    { apiAddUser(t.dataset.id); return; }
	if (t.dataset.enter === 'add-ualias')  { apiAddUserAlias(t.dataset.id); return; }
	if (t.dataset.enter === 'add-list')    { apiAddList(t.dataset.id); return; }
	if (t.dataset.enter === 'add-lmember') { apiAddListMember(t.dataset.id, t.dataset.listid); return; }
}

// ── Load / render ──────────────────────────────────────────

async function loadDomains()
{
	_domains = await apiFetch('/api/domain');
	renderDomains();
}

function renderDomains()
{
	const el = $id('dom-list');
	if (!el) return;
	if (_domains.length === 0) { el.innerHTML = '<p class="dom-empty">No domains configured yet.</p>'; return; }
	el.innerHTML = _domains.map(d => domainCard(d)).join('');
}

function domainCard(d)
{
	const open      = !!_openSections[d.id];
	const activeTab = _openSections[d.id] || 'Aliases';
	const statusCls = d.enabled ? 'dom-status--on' : 'dom-status--off';

	const tabBar = DOM_TABS.map(t =>
		`<button class="dom-subtab-btn${t === activeTab ? ' active' : ''}" data-action="sub-tab" data-id="${esc(d.id)}" data-tab="${esc(t)}">${esc(t)}</button>`
	).join('');

	const body = open ? `
		<div class="dom-subtab-nav">${tabBar}</div>
		<div class="dom-subtab-body">
			${buildSubTab(d, activeTab)}
		</div>` : '';

	return `
	<div class="dom-card${open ? ' dom-card--open' : ''}" id="dom-card-${esc(d.id)}">
		<div class="dom-card__header" data-action="toggle-expand" data-id="${esc(d.id)}">
			<span class="dom-status-dot ${statusCls}" id="dom-dot-${esc(d.id)}"></span>
			<span class="dom-name-label" data-action="toggle-expand" data-id="${esc(d.id)}" id="dom-label-${esc(d.id)}">${esc(d.name)}</span>
			<label class="toggle" title="Enable / disable domain" onclick="event.stopPropagation()">
				<input type="checkbox" data-action="toggle-enabled" data-id="${esc(d.id)}"${d.enabled ? ' checked' : ''}>
				<span class="toggle-track"></span>
			</label>
			<button class="dom-delete-btn" data-action="delete-domain" data-id="${esc(d.id)}" title="Delete domain">&times;</button>
		</div>
		${body}
	</div>`;
}

function buildSubTab(d, tab)
{
	switch (tab)
	{
		case 'General':        return buildGeneralTab(d);
		case 'Users':          return buildUsersTab(d);
		case 'User Aliases':   return buildUserAliasesTab(d);
		case 'Email Lists':    return buildEmailListsTab(d);
		case 'Limits':         return buildLimitsTab(d);
		case 'DKIM':           return buildDkimTab(d);
		case 'Domein-aliases': return buildAliasesTab(d);
		default: return '';
	}
}

function tabFooter(domainId, action, label)
{
	return `<div class="dom-tab-footer">
		<button class="btn-save" data-action="${action}" data-id="${esc(domainId)}">${label}</button>
		<span class="save-msg" id="dom-msg-${esc(domainId)}-${action}"></span>
	</div>`;
}

function tabMsg(domainId, action) { return $id(`dom-msg-${domainId}-${action}`); }

// ── Sub-tab: General ───────────────────────────────────────

function buildGeneralTab(d)
{
	return `
	<div class="form-grid">
		<div class="form-field">
			<label for="gen-name-${esc(d.id)}">Domain name</label>
			<input type="text" class="dom-input" id="gen-name-${esc(d.id)}" value="${esc(d.name)}" placeholder="domain.tld"
				data-labelfor="${esc(d.id)}" oninput="domNameInput(this)">
		</div>
		<div class="form-field">
			<label>Catch-all address</label>
			<div class="dom-catchall-row">
				<input type="text" class="dom-input" id="gen-catchall-${esc(d.id)}" value="${esc(d.catchAll ?? '')}" placeholder="username">
				<span class="dom-catchall-suffix" id="gen-catchall-suffix-${esc(d.id)}">@${esc(d.name)}</span>
			</div>
		</div>
	</div>
	${tabFooter(d.id, 'save-general', 'Save')}`;
}

// ── Sub-tab: Aliases ───────────────────────────────────────

function buildAliasesTab(d)
{
	const tags = (d.aliases || []).map(a => `
		<span class="dom-alias-tag">${esc(a)}
			<button class="dom-alias-remove" data-action="remove-alias" data-id="${esc(d.id)}" data-value="${esc(a)}">&times;</button>
		</span>`).join('');
	return `
	<p class="dom-hint">Other domain names that resolve to this domain.</p>
	<div class="dom-alias-tags" id="dom-alias-tags-${esc(d.id)}">${tags || '<span class="dom-hint">None</span>'}</div>
	<div class="dom-alias-add-row">
		<input type="text" class="dom-input" id="dom-alias-input-${esc(d.id)}"
			placeholder="alias.tld" data-enter="add-alias" data-id="${esc(d.id)}">
		<button class="btn-add-port" data-action="add-alias" data-id="${esc(d.id)}">+ Add</button>
		<span class="save-msg" id="dom-msg-${esc(d.id)}-alias"></span>
	</div>`;
}

// ── Sub-tab: Users ─────────────────────────────────────────

function buildUsersTab(d)
{
	const users = d.users || [];
	const stats = d._stats || {};

	const rows = users.map(u =>
	{
		const s          = stats[u.id] || {};
		const sizeMb     = s.sizeMb     != null ? s.sizeMb.toFixed(2) + ' MB' : '—';
		const lastActive = s.lastActivity ? timeSince(s.lastActivity)         : '—';
		const usedPct    = (u.maxSizeMb > 0 && s.sizeMb != null)
			? Math.min(100, Math.round((s.sizeMb / u.maxSizeMb) * 100)) : null;

		const bar = usedPct != null ? `
			<div class="usr-bar-track" title="${usedPct}%">
				<div class="usr-bar-fill${usedPct >= 90 ? ' usr-bar-fill--warn' : ''}" style="width:${usedPct}%"></div>
			</div>` : '';

		return `
		<tr class="usr-row" id="usr-row-${esc(u.id)}">
			<td>
				<div class="usr-address-cell">
					<input type="text" class="dom-input usr-username" id="usr-name-${esc(u.id)}"
						value="${esc(u.username)}" placeholder="username">
					<span class="usr-domain-suffix">@${esc(d.name)}</span>
				</div>
			</td>
			<td>
				<input type="password" class="dom-input usr-password" id="usr-pass-${esc(u.id)}"
					value="${esc(u.password)}" placeholder="••••••••" autocomplete="new-password">
			</td>
			<td>
				<div class="usr-size-cell">
					<input type="number" class="dom-input usr-maxsize" id="usr-max-${esc(u.id)}"
						value="${u.maxSizeMb || 0}" min="0" placeholder="0">
					${bar}
				</div>
			</td>
			<td class="usr-stat">${lastActive}</td>
			<td class="usr-stat">${sizeMb}</td>
			<td class="usr-actions">
				<button class="btn-row-save" data-action="save-user" data-id="${esc(d.id)}" data-userid="${esc(u.id)}" title="Save">&#10003;</button>
				<button class="dom-delete-btn" data-action="remove-user" data-id="${esc(d.id)}" data-userid="${esc(u.id)}" title="Delete">&times;</button>
				<span class="save-msg" id="usr-msg-${esc(u.id)}"></span>
			</td>
		</tr>`;
	}).join('');

	return `
	<div class="cfg-section-title">Accounts on ${esc(d.name)}</div>
	<table class="dom-table usr-table">
		<thead>
			<tr>
				<th>Address</th>
				<th>Password</th>
				<th>Max size (MB)</th>
				<th>Last logon</th>
				<th>Current size</th>
				<th></th>
			</tr>
		</thead>
		<tbody id="usr-tbody-${esc(d.id)}">${rows}</tbody>
		<tfoot>
			<tr>
				<td>
					<div class="usr-address-cell">
						<input type="text" class="dom-input" id="usr-new-name-${esc(d.id)}"
							placeholder="username" data-enter="add-user" data-id="${esc(d.id)}">
						<span class="usr-domain-suffix">@${esc(d.name)}</span>
					</div>
				</td>
				<td>
					<input type="password" class="dom-input" id="usr-new-pass-${esc(d.id)}"
						placeholder="Password" autocomplete="new-password">
				</td>
				<td>
					<input type="number" class="dom-input" id="usr-new-max-${esc(d.id)}"
						placeholder="0" min="0">
				</td>
				<td colspan="2"></td>
				<td>
					<button class="btn-add-port usr-add-btn" data-action="add-user"
						data-id="${esc(d.id)}" title="Add user">+</button>
				</td>
			</tr>
		</tfoot>
	</table>`;
}

// ── Sub-tab: User Aliases ──────────────────────────────────

function buildUserAliasesTab(d)
{
	const domSuffix = `@${esc(d.name)}`;
	const localPart = v => v ? v.replace(/@.*$/, '') : v;
	const listId    = `ua-users-${esc(d.id)}`;
	const userOpts  = (d.users || []).map(u => `<option value="${esc(u.username)}">`).join('');
	const datalist  = `<datalist id="${listId}">${userOpts}</datalist>`;

	const targetInput = (id, value) =>
		`<div class="ua-target-row">
			<input type="text" class="dom-input" id="${id}" value="${esc(value)}" placeholder="username" list="${listId}">
			<span class="ua-domain-suffix">${domSuffix}</span>
		</div>`;

	const rows = (d.userAliases || []).map(ua => `
		<tr>
			<td class="dom-input-cell">${esc(ua.alias)}</td>
			<td style="padding:0 8px;color:var(--text-muted)">→</td>
			<td class="dom-input-cell">${esc(localPart(ua.target))}<span class="ua-domain-suffix">${domSuffix}</span></td>
			<td><button class="dom-delete-btn" data-action="remove-ualias" data-id="${esc(d.id)}" data-alias="${esc(ua.alias)}">&times;</button></td>
		</tr>`).join('');

	return `
	${datalist}
	<p class="dom-hint">Map an alias address to another mailbox.</p>
	<table class="dom-table" id="ua-table-${esc(d.id)}">
		<thead><tr><th>Alias</th><th></th><th>Target</th><th></th></tr></thead>
		<tbody>${rows || `<tr><td colspan="4" class="dom-hint" style="padding:8px">No aliases yet.</td></tr>`}</tbody>
	</table>
	<div class="ua-add-row">
		<input type="text" class="dom-input" id="ua-new-alias-${esc(d.id)}" placeholder="alias"
			data-enter="add-ualias" data-id="${esc(d.id)}">
		<span style="color:var(--text-muted);text-align:center">→</span>
		${targetInput(`ua-new-target-${esc(d.id)}`, '')}
		<button class="btn-add-port" data-action="add-ualias" data-id="${esc(d.id)}" title="Add">+</button>
	</div>
	<div style="margin-top:4px"><span class="save-msg" id="dom-msg-${esc(d.id)}-ualias"></span></div>`;
}

// ── Sub-tab: Email Lists ───────────────────────────────────

function buildEmailListsTab(d)
{
	const lists = (d.emailLists || []).map(list => {
		const memberTags = (list.members || []).map(m => `
			<span class="dom-alias-tag">${esc(m)}
				<button class="dom-alias-remove" data-action="remove-list-member"
					data-id="${esc(d.id)}" data-listid="${esc(list.id)}" data-value="${esc(m)}">&times;</button>
			</span>`).join('');
		return `
		<div class="dom-list-block">
			<div class="dom-list-block__header">
				<span class="dom-list-name">${esc(list.name)}@${esc(d.name)}</span>
				<button class="dom-delete-btn" data-action="remove-list" data-id="${esc(d.id)}" data-listid="${esc(list.id)}">&times;</button>
			</div>
			<div class="dom-alias-tags">${memberTags || '<span class="dom-hint">No members</span>'}</div>
			<div class="dom-alias-add-row">
				<input type="text" class="dom-input" id="lm-input-${esc(list.id)}" placeholder="member@example.com"
					data-enter="add-lmember" data-id="${esc(d.id)}" data-listid="${esc(list.id)}">
				<button class="btn-add-port" data-action="add-list-member"
					data-id="${esc(d.id)}" data-listid="${esc(list.id)}">+ Add member</button>
			</div>
		</div>`;
	}).join('');

	return `
	<div class="cfg-section-title">Email lists / distribution groups</div>
	${lists || '<p class="dom-hint">No lists yet.</p>'}
	<div class="dom-alias-add-row" style="margin-top:12px">
		<input type="text" class="dom-input" id="list-new-${esc(d.id)}" placeholder="listname"
			data-enter="add-list" data-id="${esc(d.id)}">
		<span style="color:var(--text-muted);padding:0 4px 0 2px">@${esc(d.name)}</span>
		<button class="btn-add-port" data-action="add-list" data-id="${esc(d.id)}">+ Create list</button>
	</div>`;
}

// ── Sub-tab: Limits ────────────────────────────────────────

function buildLimitsTab(d)
{
	const lim = d.limits || {};
	return `
	<div class="form-grid form-grid--narrow">
		${field(`lim-mailbox-${d.id}`,  'Max mailbox (KB)',     lim.maxMailboxSizeKb  ?? 0, 'number')}
		${field(`lim-allocated-${d.id}`,'Allocated (KB)',        lim.allocatedSizeKb   ?? 0, 'number')}
		${field(`lim-msgsize-${d.id}`,  'Max message (KB)',      lim.maxMessageSizeKb  ?? 0, 'number')}
		${field(`lim-accounts-${d.id}`, 'All accounts (KB)',     lim.maxAccountsSizeKb ?? 0, 'number')}
	</div>
	<p class="dom-hint">0 = unlimited.</p>
	${tabFooter(d.id, 'save-limits', 'Save')}`;
}

function dkimKeyFilePicked(picker, textId)
{
	const file = picker.files[0];
	if (!file) return;
	const el = $id(textId);
	if (el) el.value = file.name;
}

// ── Sub-tab: DKIM ──────────────────────────────────────────

function buildDkimTab(d)
{
	const dk = d.dkim || {};
	const hOpts = ['Simple', 'Relaxed'].map(v =>
		`<option value="${v}"${(dk.headerMethod ?? 'Relaxed') === v ? ' selected' : ''}>${v}</option>`).join('');
	const bOpts = ['Simple', 'Relaxed'].map(v =>
		`<option value="${v}"${(dk.bodyMethod ?? 'Relaxed') === v ? ' selected' : ''}>${v}</option>`).join('');
	const aOpts = ['SHA256', 'SHA1'].map(v =>
		`<option value="${v}"${(dk.algorithm ?? 'SHA256') === v ? ' selected' : ''}>${v}</option>`).join('');

	return `
	<div class="toggle-list">
		${toggle(`dkim-enabled-${d.id}`, 'Enable DKIM signing', 'Sign outgoing messages with DKIM', dk.enabled ?? false)}
	</div>
	<div class="form-grid form-grid--dkim">
		<div class="form-field">
			<label for="dkim-keyfile-${esc(d.id)}">Private key file</label>
			<div class="file-picker-row">
				<input type="text" id="dkim-keyfile-${esc(d.id)}" value="${esc(dk.privateKeyFile ?? '')}" placeholder="path/to/private.key">
				<input type="file" id="dkim-keyfile-picker-${esc(d.id)}" accept=".pem,.key,.p8,.der" style="display:none"
					onchange="dkimKeyFilePicked(this, 'dkim-keyfile-${esc(d.id)}')">
				<button type="button" class="btn-browse" onclick="$id('dkim-keyfile-picker-${esc(d.id)}').click()">Browse&hellip;</button>
			</div>
		</div>
		<div class="form-field dkim-selector-field">
			<label for="dkim-selector-${esc(d.id)}">Selector</label>
			<input type="text" id="dkim-selector-${esc(d.id)}" value="${esc(dk.selector ?? '')}" placeholder="mail">
		</div>
	</div>
	<div class="form-grid">
		<div class="form-field">
			<label for="dkim-hmethod-${esc(d.id)}">Header canonicalization</label>
			<select id="dkim-hmethod-${esc(d.id)}">${hOpts}</select>
		</div>
		<div class="form-field">
			<label for="dkim-bmethod-${esc(d.id)}">Body canonicalization</label>
			<select id="dkim-bmethod-${esc(d.id)}">${bOpts}</select>
		</div>
		<div class="form-field">
			<label for="dkim-algo-${esc(d.id)}">Signing algorithm</label>
			<select id="dkim-algo-${esc(d.id)}">${aOpts}</select>
		</div>
	</div>
	${tabFooter(d.id, 'save-dkim', 'Save')}`;
}

function collectLimits(d)
{
	return {
		maxMailboxSizeKb:  parseInt($id(`lim-mailbox-${d.id}`)?.value)   || 0,
		allocatedSizeKb:   parseInt($id(`lim-allocated-${d.id}`)?.value) || 0,
		maxMessageSizeKb:  parseInt($id(`lim-msgsize-${d.id}`)?.value)   || 0,
		maxAccountsSizeKb: parseInt($id(`lim-accounts-${d.id}`)?.value)  || 0
	};
}

function collectDkim(d)
{
	return {
		enabled:        $id(`dkim-enabled-${d.id}`)?.checked ?? false,
		privateKeyFile: $id(`dkim-keyfile-${d.id}`)?.value.trim()   ?? '',
		selector:       $id(`dkim-selector-${d.id}`)?.value.trim()  ?? '',
		headerMethod:   $id(`dkim-hmethod-${d.id}`)?.value          ?? 'Relaxed',
		bodyMethod:     $id(`dkim-bmethod-${d.id}`)?.value          ?? 'Relaxed',
		algorithm:      $id(`dkim-algo-${d.id}`)?.value             ?? 'SHA256'
	};
}

function domNameInput(input)
{
	const id = input.dataset.labelfor;
	const label  = $id(`dom-label-${id}`);
	const suffix = $id(`gen-catchall-suffix-${id}`);
	if (label)  label.textContent  = input.value || input.placeholder;
	if (suffix) suffix.textContent = `@${input.value || input.placeholder}`;
}

// ── Actions ────────────────────────────────────────────────

function toggleExpand(id)
{
	if (_openSections[id]) { delete _openSections[id]; renderDomains(); return; }
	_openSections[id] = 'General';
	renderDomains();
}

function setSubTab(id, tab)
{
	_openSections[id] = tab;
	renderDomains();
	const d = _domains.find(x => x.id === id);
	if (tab === 'Users' && d) loadUserStats(d);
}

async function loadUserStats(d)
{
	try
	{
		const stats = await apiFetch(`/api/domain/${d.id}/userstats`);
		d._stats = {};
		for (const s of stats) d._stats[s.userId] = s;
		// only re-render if still on Users tab
		if (_openSections[d.id] === 'Users') renderDomains();
	}
	catch { /* stats are optional */ }
}

async function saveGeneral(id)
{
	const d = _domains.find(x => x.id === id);
	if (!d) return;
	const name    = $id(`gen-name-${id}`)?.value.trim()    || d.name;
	const catchAll = $id(`gen-catchall-${id}`)?.value.trim() ?? d.catchAll;
	const msg = tabMsg(id, 'save-general');
	try
	{
		await apiFetch(`/api/domain/${id}/general`, { name, enabled: d.enabled, catchAll }, 'PUT');
		d.name    = name;
		d.catchAll = catchAll;
		const label = $id(`dom-label-${id}`);
		if (label) label.textContent = name;
		showMsg(msg, true, 'Saved');
	}
	catch (e) { showMsg(msg, false, e.message || 'Error'); }
}

async function saveLimits(id)
{
	const d = _domains.find(x => x.id === id);
	if (!d) return;
	const limits = collectLimits(d);
	const msg = tabMsg(id, 'save-limits');
	try
	{
		await apiFetch(`/api/domain/${id}/limits`, limits, 'PUT');
		d.limits = limits;
		showMsg(msg, true, 'Saved');
	}
	catch (e) { showMsg(msg, false, e.message || 'Error'); }
}

async function saveDkim(id)
{
	const d = _domains.find(x => x.id === id);
	if (!d) return;
	const dkim = collectDkim(d);
	const msg = tabMsg(id, 'save-dkim');
	try
	{
		await apiFetch(`/api/domain/${id}/dkim`, dkim, 'PUT');
		d.dkim = dkim;
		showMsg(msg, true, 'Saved');
	}
	catch (e) { showMsg(msg, false, e.message || 'Error'); }
}

async function deleteDomain(id)
{
	if (!confirm('Delete this domain and all its settings?')) return;
	try
	{
		const res = await fetch(`/api/domain/${id}`, { method: 'DELETE' });
		if (!res.ok) throw new Error(`HTTP ${res.status}`);
		delete _openSections[id];
		await loadDomains();
		showMsg($id('msg-domains'), true, 'Deleted');
	}
	catch (e) { showMsg($id('msg-domains'), false, e.message || 'Error'); }
}

// ── Per-item API helpers ───────────────────────────────────

async function apiAddAlias(domainId)
{
	const d     = _domains.find(x => x.id === domainId);
	const input = $id(`dom-alias-input-${domainId}`);
	const val   = input?.value.trim().toLowerCase();
	const msg   = $id(`dom-msg-${domainId}-alias`);
	if (!d || !val) return;
	try
	{
		await apiFetch(`/api/domain/${domainId}/aliases`, { value: val }, 'POST');
		if (!d.aliases.includes(val)) d.aliases.push(val);
		input.value = '';
		renderDomains();
	}
	catch (e) { showMsg(msg, false, e.message || 'Error'); }
}

async function apiRemoveAlias(domainId, alias)
{
	const d = _domains.find(x => x.id === domainId);
	if (!d) return;
	try
	{
		await apiFetch(`/api/domain/${domainId}/aliases/${encodeURIComponent(alias)}`, null, 'DELETE');
		d.aliases = d.aliases.filter(a => a !== alias);
		renderDomains();
	}
	catch (e) { console.error(e); }
}

async function apiAddUser(domainId)
{
	const d         = _domains.find(x => x.id === domainId);
	const username  = $id(`usr-new-name-${domainId}`)?.value.trim().toLowerCase();
	const password  = $id(`usr-new-pass-${domainId}`)?.value;
	const maxSizeMb = parseInt($id(`usr-new-max-${domainId}`)?.value) || 0;
	if (!d || !username || !password) return;
	try
	{
		const created = await apiFetch(`/api/domain/${domainId}/users`, { username, password, maxSizeMb }, 'POST');
		d.users.push(created);
		$id(`usr-new-name-${domainId}`).value = '';
		$id(`usr-new-pass-${domainId}`).value = '';
		$id(`usr-new-max-${domainId}`).value  = '';
		renderDomains();
	}
	catch (e) { console.error(e); }
}

async function apiSaveUser(domainId, userId)
{
	const d = _domains.find(x => x.id === domainId);
	const u = d?.users?.find(x => x.id === userId);
	if (!u) return;
	const username  = $id(`usr-name-${userId}`)?.value.trim().toLowerCase() || u.username;
	const password  = $id(`usr-pass-${userId}`)?.value || u.password;
	const maxSizeMb = parseInt($id(`usr-max-${userId}`)?.value) || 0;
	const msg = $id(`usr-msg-${userId}`);
	try
	{
		await apiFetch(`/api/domain/${domainId}/users/${userId}`, { id: userId, username, password, maxSizeMb }, 'PUT');
		u.username  = username;
		u.password  = password;
		u.maxSizeMb = maxSizeMb;
		showMsg(msg, true, 'Saved');
	}
	catch (e) { showMsg(msg, false, e.message || 'Error'); }
}

async function apiRemoveUser(domainId, userId)
{
	if (!confirm('Remove this user?')) return;
	const d = _domains.find(x => x.id === domainId);
	if (!d) return;
	try
	{
		await apiFetch(`/api/domain/${domainId}/users/${userId}`, null, 'DELETE');
		d.users = d.users.filter(u => u.id !== userId);
		renderDomains();
	}
	catch (e) { console.error(e); }
}

async function apiAddUserAlias(domainId)
{
	const d     = _domains.find(x => x.id === domainId);
	const alias = $id(`ua-new-alias-${domainId}`)?.value.trim();
	const local = $id(`ua-new-target-${domainId}`)?.value.trim();
	const target = local ? `${local}@${d?.name}` : '';
	const msg   = $id(`dom-msg-${domainId}-ualias`);
	if (!d || !alias || !target) return;
	try
	{
		await apiFetch(`/api/domain/${domainId}/useraliases`, { alias, target }, 'POST');
		d.userAliases = d.userAliases.filter(a => a.alias !== alias);
		d.userAliases.push({ alias, target });
		$id(`ua-new-alias-${domainId}`).value  = '';
		$id(`ua-new-target-${domainId}`).value = '';
		renderDomains();
	}
	catch (e) { showMsg(msg, false, e.message || 'Error'); }
}

async function apiRemoveUserAlias(domainId, alias)
{
	const d = _domains.find(x => x.id === domainId);
	if (!d) return;
	try
	{
		await apiFetch(`/api/domain/${domainId}/useraliases/${encodeURIComponent(alias)}`, null, 'DELETE');
		d.userAliases = d.userAliases.filter(a => a.alias !== alias);
		renderDomains();
	}
	catch (e) { console.error(e); }
}

async function apiAddList(domainId)
{
	const d    = _domains.find(x => x.id === domainId);
	const name = $id(`list-new-${domainId}`)?.value.trim().toLowerCase();
	if (!d || !name) return;
	try
	{
		const created = await apiFetch(`/api/domain/${domainId}/lists`, { name }, 'POST');
		d.emailLists = d.emailLists || [];
		d.emailLists.push(created);
		$id(`list-new-${domainId}`).value = '';
		renderDomains();
	}
	catch (e) { console.error(e); }
}

async function apiRemoveList(domainId, listId)
{
	const d = _domains.find(x => x.id === domainId);
	if (!d) return;
	try
	{
		await apiFetch(`/api/domain/${domainId}/lists/${listId}`, null, 'DELETE');
		d.emailLists = d.emailLists.filter(l => l.id !== listId);
		renderDomains();
	}
	catch (e) { console.error(e); }
}

async function apiAddListMember(domainId, listId)
{
	const d    = _domains.find(x => x.id === domainId);
	const list = d?.emailLists?.find(l => l.id === listId);
	const val  = $id(`lm-input-${listId}`)?.value.trim().toLowerCase();
	if (!list || !val) return;
	try
	{
		await apiFetch(`/api/domain/${domainId}/lists/${listId}/members`, { value: val }, 'POST');
		if (!list.members.includes(val)) list.members.push(val);
		$id(`lm-input-${listId}`).value = '';
		renderDomains();
	}
	catch (e) { console.error(e); }
}

async function apiRemoveListMember(domainId, listId, value)
{
	const d    = _domains.find(x => x.id === domainId);
	const list = d?.emailLists?.find(l => l.id === listId);
	if (!list) return;
	try
	{
		await apiFetch(`/api/domain/${domainId}/lists/${listId}/members/${encodeURIComponent(value)}`, null, 'DELETE');
		list.members = list.members.filter(m => m !== value);
		renderDomains();
	}
	catch (e) { console.error(e); }
}

// load domains when Domains tab becomes active
document.addEventListener('DOMContentLoaded', () =>
{
	const domainsBtn = $id('tab-domains');
	if (domainsBtn)
		domainsBtn.addEventListener('click', () => { if (_domains.length === 0) loadDomains(); });
});
