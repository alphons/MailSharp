namespace MailSharp.TestFormApp
{
	partial class Form1
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			label1 = new Label();
			txtServer = new TextBox();
			txtUserid = new TextBox();
			label2 = new Label();
			txtPassword = new TextBox();
			label3 = new Label();
			txtFrom = new TextBox();
			label4 = new Label();
			txtTo = new TextBox();
			label5 = new Label();
			groupBox1 = new GroupBox();
			rad3 = new RadioButton();
			rad2 = new RadioButton();
			rad1 = new RadioButton();
			txtSubject = new TextBox();
			label6 = new Label();
			txtBody = new TextBox();
			label7 = new Label();
			chkEnableSSL = new CheckBox();
			grpCredentials = new GroupBox();
			chkUseCredentials = new CheckBox();
			txtLog = new TextBox();
			grpEmail = new GroupBox();
			chkHtml = new CheckBox();
			button1 = new Button();
			button2 = new Button();
			groupBox1.SuspendLayout();
			grpCredentials.SuspendLayout();
			grpEmail.SuspendLayout();
			SuspendLayout();
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Location = new Point(19, 37);
			label1.Name = "label1";
			label1.Size = new Size(39, 15);
			label1.TabIndex = 0;
			label1.Text = "Server";
			// 
			// txtServer
			// 
			txtServer.Location = new Point(64, 34);
			txtServer.Name = "txtServer";
			txtServer.Size = new Size(120, 23);
			txtServer.TabIndex = 1;
			txtServer.Text = "127.0.0.1";
			// 
			// txtUserid
			// 
			txtUserid.Location = new Point(67, 22);
			txtUserid.Name = "txtUserid";
			txtUserid.Size = new Size(120, 23);
			txtUserid.TabIndex = 3;
			txtUserid.Text = "testuser";
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Location = new Point(22, 25);
			label2.Name = "label2";
			label2.Size = new Size(39, 15);
			label2.TabIndex = 2;
			label2.Text = "userid";
			// 
			// txtPassword
			// 
			txtPassword.Location = new Point(67, 51);
			txtPassword.Name = "txtPassword";
			txtPassword.Size = new Size(120, 23);
			txtPassword.TabIndex = 5;
			txtPassword.Text = "testpassword";
			// 
			// label3
			// 
			label3.AutoSize = true;
			label3.Location = new Point(4, 54);
			label3.Name = "label3";
			label3.Size = new Size(57, 15);
			label3.TabIndex = 4;
			label3.Text = "password";
			// 
			// txtFrom
			// 
			txtFrom.Location = new Point(64, 218);
			txtFrom.Name = "txtFrom";
			txtFrom.Size = new Size(151, 23);
			txtFrom.TabIndex = 7;
			txtFrom.Text = "alphons@heijden.com";
			// 
			// label4
			// 
			label4.AutoSize = true;
			label4.Location = new Point(19, 221);
			label4.Name = "label4";
			label4.Size = new Size(35, 15);
			label4.TabIndex = 6;
			label4.Text = "From";
			// 
			// txtTo
			// 
			txtTo.Location = new Point(64, 247);
			txtTo.Name = "txtTo";
			txtTo.Size = new Size(151, 23);
			txtTo.TabIndex = 9;
			txtTo.Text = "alphons@heijden.com";
			// 
			// label5
			// 
			label5.AutoSize = true;
			label5.Location = new Point(38, 250);
			label5.Name = "label5";
			label5.Size = new Size(19, 15);
			label5.TabIndex = 8;
			label5.Text = "To";
			// 
			// groupBox1
			// 
			groupBox1.Controls.Add(rad3);
			groupBox1.Controls.Add(rad2);
			groupBox1.Controls.Add(rad1);
			groupBox1.Location = new Point(200, 31);
			groupBox1.Name = "groupBox1";
			groupBox1.Size = new Size(165, 59);
			groupBox1.TabIndex = 10;
			groupBox1.TabStop = false;
			groupBox1.Text = "port";
			// 
			// rad3
			// 
			rad3.AutoSize = true;
			rad3.Location = new Point(112, 24);
			rad3.Name = "rad3";
			rad3.Size = new Size(43, 19);
			rad3.TabIndex = 2;
			rad3.TabStop = true;
			rad3.Text = "587";
			rad3.UseVisualStyleBackColor = true;
			// 
			// rad2
			// 
			rad2.AutoSize = true;
			rad2.Location = new Point(63, 24);
			rad2.Name = "rad2";
			rad2.Size = new Size(43, 19);
			rad2.TabIndex = 1;
			rad2.TabStop = true;
			rad2.Text = "465";
			rad2.UseVisualStyleBackColor = true;
			// 
			// rad1
			// 
			rad1.AutoSize = true;
			rad1.Checked = true;
			rad1.Location = new Point(14, 24);
			rad1.Name = "rad1";
			rad1.Size = new Size(37, 19);
			rad1.TabIndex = 0;
			rad1.TabStop = true;
			rad1.Text = "25";
			rad1.UseVisualStyleBackColor = true;
			// 
			// txtSubject
			// 
			txtSubject.Location = new Point(64, 276);
			txtSubject.Name = "txtSubject";
			txtSubject.Size = new Size(212, 23);
			txtSubject.TabIndex = 12;
			txtSubject.Text = "Test Subject";
			// 
			// label6
			// 
			label6.AutoSize = true;
			label6.Location = new Point(12, 279);
			label6.Name = "label6";
			label6.Size = new Size(46, 15);
			label6.TabIndex = 11;
			label6.Text = "Subject";
			// 
			// txtBody
			// 
			txtBody.AcceptsReturn = true;
			txtBody.AcceptsTab = true;
			txtBody.Location = new Point(64, 305);
			txtBody.Multiline = true;
			txtBody.Name = "txtBody";
			txtBody.ScrollBars = ScrollBars.Both;
			txtBody.Size = new Size(301, 97);
			txtBody.TabIndex = 14;
			txtBody.Text = "dit is een test\\r\\nklopt ook\\r\\n";
			// 
			// label7
			// 
			label7.AutoSize = true;
			label7.Location = new Point(24, 308);
			label7.Name = "label7";
			label7.Size = new Size(34, 15);
			label7.TabIndex = 13;
			label7.Text = "Body";
			// 
			// chkEnableSSL
			// 
			chkEnableSSL.AutoSize = true;
			chkEnableSSL.Location = new Point(64, 71);
			chkEnableSSL.Name = "chkEnableSSL";
			chkEnableSSL.Size = new Size(67, 19);
			chkEnableSSL.TabIndex = 18;
			chkEnableSSL.Text = "SSL/TLS";
			chkEnableSSL.UseVisualStyleBackColor = true;
			// 
			// grpCredentials
			// 
			grpCredentials.Controls.Add(txtPassword);
			grpCredentials.Controls.Add(label3);
			grpCredentials.Controls.Add(txtUserid);
			grpCredentials.Controls.Add(label2);
			grpCredentials.Enabled = false;
			grpCredentials.Location = new Point(64, 121);
			grpCredentials.Name = "grpCredentials";
			grpCredentials.Size = new Size(200, 81);
			grpCredentials.TabIndex = 19;
			grpCredentials.TabStop = false;
			grpCredentials.Text = "credentials";
			// 
			// chkUseCredentials
			// 
			chkUseCredentials.AutoSize = true;
			chkUseCredentials.Location = new Point(64, 96);
			chkUseCredentials.Name = "chkUseCredentials";
			chkUseCredentials.Size = new Size(104, 19);
			chkUseCredentials.TabIndex = 20;
			chkUseCredentials.Text = "use credentials";
			chkUseCredentials.UseVisualStyleBackColor = true;
			chkUseCredentials.CheckedChanged += UseCrendentials_CheckedChanged;
			// 
			// txtLog
			// 
			txtLog.AcceptsReturn = true;
			txtLog.AcceptsTab = true;
			txtLog.Location = new Point(411, 12);
			txtLog.Multiline = true;
			txtLog.Name = "txtLog";
			txtLog.ReadOnly = true;
			txtLog.ScrollBars = ScrollBars.Both;
			txtLog.Size = new Size(377, 458);
			txtLog.TabIndex = 21;
			// 
			// grpEmail
			// 
			grpEmail.Controls.Add(chkHtml);
			grpEmail.Controls.Add(txtServer);
			grpEmail.Controls.Add(label1);
			grpEmail.Controls.Add(chkUseCredentials);
			grpEmail.Controls.Add(label5);
			grpEmail.Controls.Add(label4);
			grpEmail.Controls.Add(txtTo);
			grpEmail.Controls.Add(grpCredentials);
			grpEmail.Controls.Add(groupBox1);
			grpEmail.Controls.Add(txtFrom);
			grpEmail.Controls.Add(label6);
			grpEmail.Controls.Add(chkEnableSSL);
			grpEmail.Controls.Add(txtSubject);
			grpEmail.Controls.Add(label7);
			grpEmail.Controls.Add(txtBody);
			grpEmail.Location = new Point(12, 12);
			grpEmail.Name = "grpEmail";
			grpEmail.Size = new Size(382, 419);
			grpEmail.TabIndex = 22;
			grpEmail.TabStop = false;
			grpEmail.Text = "email";
			// 
			// chkHtml
			// 
			chkHtml.AutoSize = true;
			chkHtml.Location = new Point(307, 280);
			chkHtml.Name = "chkHtml";
			chkHtml.Size = new Size(58, 19);
			chkHtml.TabIndex = 21;
			chkHtml.Text = "HTML";
			chkHtml.UseVisualStyleBackColor = true;
			// 
			// button1
			// 
			button1.Location = new Point(275, 437);
			button1.Name = "button1";
			button1.Size = new Size(75, 23);
			button1.TabIndex = 23;
			button1.Text = "Ok";
			button1.UseVisualStyleBackColor = true;
			button1.Click += SendEmail_Click;
			// 
			// button2
			// 
			button2.Enabled = false;
			button2.Location = new Point(188, 437);
			button2.Name = "button2";
			button2.Size = new Size(75, 23);
			button2.TabIndex = 24;
			button2.Text = "Cancel";
			button2.UseVisualStyleBackColor = true;
			button2.Click += Cancel_Click;
			// 
			// Form1
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(800, 509);
			Controls.Add(button2);
			Controls.Add(button1);
			Controls.Add(grpEmail);
			Controls.Add(txtLog);
			Name = "Form1";
			Text = "Email Test Client";
			groupBox1.ResumeLayout(false);
			groupBox1.PerformLayout();
			grpCredentials.ResumeLayout(false);
			grpCredentials.PerformLayout();
			grpEmail.ResumeLayout(false);
			grpEmail.PerformLayout();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Label label1;
		private TextBox txtServer;
		private TextBox txtUserid;
		private Label label2;
		private TextBox txtPassword;
		private Label label3;
		private TextBox txtFrom;
		private Label label4;
		private TextBox txtTo;
		private Label label5;
		private GroupBox groupBox1;
		private RadioButton rad3;
		private RadioButton rad2;
		private RadioButton rad1;
		private TextBox txtSubject;
		private Label label6;
		private TextBox txtBody;
		private Label label7;
		private CheckBox chkStartTls;
		private CheckBox chkEnableSSL;
		private GroupBox grpCredentials;
		private CheckBox chkUseCredentials;
		private TextBox txtLog;
		private GroupBox grpEmail;
		private Button button1;
		private Button button2;
		private CheckBox chkHtml;
	}
}
