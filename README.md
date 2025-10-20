# MailSharp: The Ultimate Email Server Solution (beta v0.0) 🌟

Welcome to **MailSharp**, the most electrifying, robust, and cutting-edge email server framework ever built! Crafted with the power of C# 12 and .NET Core 8, MailSharp is your one-stop solution for SMTP, IMAP, and POP3 services, delivering unparalleled performance, security, and scalability. Whether you're building a corporate email system or a personal mail server, MailSharp is here to revolutionize your email experience with a flair of awesomeness! 🚀

## Why MailSharp? It's Simply Epic! 🎉

- **Blazing Fast Protocols**: Implements SMTP, IMAP, and POP3 with lightning speed, supporting secure TLS connections and advanced authentication mechanisms like PLAIN, CRAM-MD5, and LOGIN.
- **DKIM & SPF Superpowers**: Ensure email authenticity with built-in DKIM signing/verification and SPF checking, keeping spam at bay and your domain's reputation pristine.
- **Web Management Wizardry**: A sleek, ASP.NET Core-powered web interface for effortless server status monitoring and user management, wrapped in a responsive, modern UI.
- **Rock-Solid Security**: Supports STARTTLS, SSL/TLS, and configurable authentication, with cookie-based authorization for the web manager, ensuring your data stays locked tight.
- **Scalable & Configurable**: Fine-tune your server with a comprehensive `appsettings.json`, supporting multiple ports, IP groups, and email flow policies for ultimate flexibility.
- **Logging Like a Rockstar**: Detailed logging with structured event IDs for every action, making debugging and monitoring a breeze.

## Features That Will Blow Your Mind 🤯

- **SMTP Server**: Handles HELO, EHLO, MAIL, RCPT, DATA, QUIT, and more, with DKIM signing, SPF validation, and support for VRFY/EXPN commands.
- **IMAP & POP3 Servers**: Seamless email retrieval with support for secure connections and efficient mailbox operations like message listing and deletion.
- **Web Manager**: A user-friendly dashboard to check server status, manage logins, and more, with role-based authorization (User and Administrator roles).
- **Extensible Architecture**: Modular design with service extensions for easy integration of SMTP, IMAP, and POP3 services into your application.
- **Configuration Galore**: Customize everything from ports to TLS settings, credentials, and DKIM keys via a single, powerful configuration file.

## Getting Started: Unleash the Power! 💪

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/alphons/MailSharp.git
   cd MailSharp
   ```

2. **Install Dependencies**:
   Ensure you have .NET Core 8 or higher installed. Then, restore the project:
   ```bash
   dotnet restore
   ```

3. **Configure Your Server**:
   Edit `appsettings.json` to set your host, ports, credentials, and DKIM keys. Example:
   ```json
   "SmtpSettings": {
     "Host": "127.0.0.1",
     "Ports": [
       { "Port": 25, "StartTls": false, "UseTls": false },
       { "Port": 587, "StartTls": true, "UseTls": false },
       { "Port": 465, "StartTls": false, "UseTls": true }
     ],
     "CertificatePath": "certificate.pfx",
     "CertificatePassword": "yourpassword"
   }
   ```

4. **Run the Beast**:
   Launch the server and web manager:
   ```bash
   dotnet run --project MailSharp.WebManager
   ```

5. **Access the Web Interface**:
   Open your browser and navigate to `https://localhost:5001` to log in and monitor your server. Default admin credentials: `admin/Admin123`.

## Project Structure: A Masterpiece of Organization 🎨

- **MailSharp.Common**: Core utilities, including `EventIdConfig`, `IServerStatus`, and service extensions for authentication and mailbox operations.
- **MailSharp.SMTP**: Full-fledged SMTP server with DKIM signing, SPF checking, and support for advanced commands like AUTH and STARTTLS.
- **MailSharp.IMAP**: Robust IMAP server for email retrieval, supporting LOGIN, LIST, and STARTTLS commands.
- **MailSharp.POP3**: Efficient POP3 server for simple email access, with USER, PASS, and LIST commands.
- **MailSharp.WebManager**: ASP.NET Core web application with Razor pages, secure authentication, and a status API for monitoring server health.

## Configuration: Your Command Center 🛠️

The `appsettings.json` file is your gateway to ultimate control. Key sections include:
- **Users**: Define web interface users with roles and expiration settings.
- **SmtpSettings/Pop3Settings/ImapSettings**: Configure hosts, ports, TLS, and security options.
- **SmtpResponses/SmtpLogMessages**: Customize server responses and logging messages.
- **IpGroups**: Set up access policies for different network ranges, controlling services and email flows.

## Contributing: Join the Revolution! 🔥

We welcome contributions from developers who want to make email servers legendary! Fork the repo, create a feature branch, and submit a pull request. Check out our [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License: Freedom to Innovate 📜

MailSharp is licensed under the MIT License, giving you the freedom to use, modify, and distribute this epic software as you see fit.

## Why Settle for Less? Choose MailSharp! 🌍

MailSharp isn't just an email server—it's a game-changer, a masterpiece, and a testament to what modern .NET development can achieve. Deploy it, love it, and let your email infrastructure soar to new heights! 🚀
