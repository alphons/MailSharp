using System.Net;
using System.Net.Mail;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace MailSharp.TestFormApp
{
	public partial class Form1 : Form
	{
		private CancellationTokenSource? cts;
		public Form1()
		{
			InitializeComponent();
		}

		private void UseCrendentials_CheckedChanged(object sender, EventArgs e)
		{
			this.grpCredentials.Enabled = this.chkUseCredentials.Checked;
		}

		private async void SendEmail_Click(object sender, EventArgs e)
		{
			this.button1.Enabled = false;
			this.button2.Enabled = true;
			this.grpEmail.Enabled = false;

			cts = new CancellationTokenSource();

			try
			{
				await SendEmailAsync(cts.Token);

			}
			catch(OperationCanceledException)
			{
				//MessageBox.Show("Email sending was cancelled.");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"An error occurred: {ex.Message}");
			}
			finally
			{
				this.button1.Enabled = true;
				this.button2.Enabled = false;
				this.grpEmail.Enabled = true;
			}
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			if (this.cts is not null && !this.cts.IsCancellationRequested)
			{
				this.cts.Cancel();
			}
		}

		private async Task SendEmailAsync(CancellationToken ct)
		{
			int smtpPort = 25;
			if(rad1.Checked)
			{
				smtpPort = 25;
			}
			else if(rad2.Checked)
			{
				smtpPort = 465;
			}
			else if(rad3.Checked)
			{
				smtpPort = 587;
			}

			using SmtpClient client = new(this.txtServer.Text, smtpPort);

			if(chkUseCredentials.Checked)
				client.Credentials = new NetworkCredential(this.txtUserid.Text, this.txtPassword.Text);

			if(this.chkEnableSSL.Checked)
				client.EnableSsl = true;

			MailMessage message = new()
			{
				From = new MailAddress(this.txtFrom.Text),
				Subject = this.txtSubject.Text,
				Body = this.txtBody.Text,
				IsBodyHtml = false
			};
			message.To.Add(this.txtTo.Text);

			await client.SendMailAsync(message, ct);
		}

	}
}
