# MailSharp

A self-hosted email server built with C# and .NET 10, providing SMTP, IMAP, and POP3 services with an integrated web management interface.

## Features

- **SMTP** — ports 25, 587 (STARTTLS), and 465 (TLS); DKIM signing/verification; SPF and DMARC checking; relay support; configurable email flow policies per IP group
- **IMAP** — ports 143 (STARTTLS) and 993 (TLS); IDLE, ACL, QUOTA, SORT extensions; public folder support
- **POP3** — ports 110 and 995 (TLS)
- **Web Manager** — ASP.NET Core Razor Pages dashboard for server status, domain management, and configuration; role-based access (User / Administrator)
- **DNS** — built-in async DNS resolver with configurable upstream servers
- **IP Groups** — per-CIDR access control with per-protocol and per-email-flow policies, expiry support
- **Anti-spam / Anti-virus** — pluggable per IP group

## Project Structure

| Project | Description |
|---|---|
| `MailSharp.Common` | Shared interfaces, models, and service extensions |
| `MailSharp.DNS` | Async DNS resolver |
| `MailSharp.SMTP` | SMTP server |
| `MailSharp.IMAP` | IMAP server |
| `MailSharp.POP3` | POP3 server |
| `MailSharp.WebManager` | ASP.NET Core web application and host process |
| `MailSharp.TestFormApp` | WinForms test client |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A valid TLS certificate (`.pfx`) for your mail domain

### Build and run

```bash
git clone https://github.com/alphons/MailSharp.git
cd MailSharp
dotnet run --project MailSharp.WebManager
```

The web interface is available at `https://localhost:5001`.

Default admin credentials: `admin` / `admin` — **change these before exposing the server externally**.

### Install as a Windows Service

Run `_install.cmd` as Administrator from the published output directory. The script creates and starts a `MailSharp` Windows Service configured for delayed auto-start with automatic restart on failure.

To remove the service, run `_uninstall.cmd` as Administrator.

## Configuration

All server settings are stored in `mailsharp.json` alongside the executable. The web manager provides a UI for most settings; `appsettings.json` controls logging and ASP.NET host options only.

### Key sections in `mailsharp.json`

**`IpGroups`** — define CIDR ranges with per-protocol access and email flow policies:
```json
{
  "Name": "Remote",
  "Priority": 3,
  "Cidr": "0.0.0.0/0",
  "Access": { "Smtp": true, "Pop3": false, "Imap": true, "RequireSslTlsForAuth": true },
  "EmailFlows": {
    "ExternalToLocal": { "Allowed": true, "RequireAuth": false },
    "LocalToExternal": { "Allowed": false, "RequireAuth": true }
  }
}
```

**`SmtpSettings`** — ports, TLS certificate, local domains, relay, DKIM, storage paths, connection limits.

**`Pop3Settings` / `ImapSettings`** — ports, TLS certificate, connection limits, IMAP extensions.

**`MailboxSettings`** — path where mailboxes are stored on disk.

**`MaintenanceUsers`** — web manager accounts with roles and optional expiry.

## License

MIT

## Contributing

Fork the repository, create a feature branch, and open a pull request.
