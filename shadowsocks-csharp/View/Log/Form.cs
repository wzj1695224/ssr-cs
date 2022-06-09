using Shadowsocks.Controller;
using Shadowsocks.Core;
using System;
using System.IO;
using System.Windows.Forms;
using static Shadowsocks.Controller.I18N.Static;


namespace Shadowsocks.View.Log
{
	public partial class LogForm : Form
	{
		private readonly ShadowsocksController _controller;

		private const int MaxReadSize = 65536;

		private string _currentLogFile;
		private string _currentLogFileName;
		private long   _currentOffset;

		private bool   _listenLogEnable = false;


		public LogForm(ShadowsocksController controller)
		{
			_controller = controller;

			InitializeComponent();
			Icon = ResourceFactory.CreateIcon();
			UpdateTexts();
		}

		private void UpdateTexts()
		{
			// Form
			Text = S("Log Viewer");
			// File
			fileMenu.Text = S("&File");
			clearLogMenu.Text = S("Clear &log");
			showInExplorerMenu.Text = S("Show in &Explorer");
			closeMenu.Text = S("&Close");
			// View
			viewMenu.Text = S("&View");
			fontMenu.Text = S("&Font...");
			wrapTextMenu.Text = S("&Wrap Text");
			alwaysOnTopMenu.Text = S("&Always on top");
		}




		private void ReadLog()
		{
			if (Logging.save_to_file)
			{
				StopListenLog();
				ReadFileLog();
			}
			else 
				StartListenLog();
		}


		private void ReadFileLog()
		{
			var newLogFile = Logging.LogFilePath;
			if (newLogFile != _currentLogFile)
			{
				_currentOffset = 0;
				_currentLogFile = newLogFile;
				_currentLogFileName = Logging.LogFileName;
			}

			try
			{
				using (
					var reader = new StreamReader(new FileStream(newLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				)
				{
					if (_currentOffset == 0)
					{
						var maxSize = reader.BaseStream.Length;
						if (maxSize > MaxReadSize)
						{
							reader.BaseStream.Seek(-MaxReadSize, SeekOrigin.End);
							reader.ReadLine();
						}
					}
					else
					{
						reader.BaseStream.Seek(_currentOffset, SeekOrigin.Begin);
					}

					var txt = reader.ReadToEnd();
					if (!string.IsNullOrEmpty(txt))
					{
						logTextBox.AppendText(txt);
						logTextBox.ScrollToCaret();
					}

					_currentOffset = reader.BaseStream.Position;
				}
			}
			catch (FileNotFoundException)
			{
			}
			catch (ArgumentNullException)
			{
			}

			Text = $@"{S("Log Viewer")} {_currentLogFileName}";
		}




		private void StartListenLog()
		{
			if (_listenLogEnable)
				return;
			_listenLogEnable = true;

			Logging.LogListener += OnLogListener;
		}


		private void StopListenLog()
		{
			if (!_listenLogEnable)
				return;
			_listenLogEnable = false;

			Logging.LogListener -= OnLogListener;
		}


		private void OnLogListener(string msg)
		{
			logTextBox.BeginInvoke((Action)(() =>
			{
				logTextBox.AppendText(msg);
				logTextBox.ScrollToCaret();
			}));
		}




		private void closeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			StopListenLog();
			Close();
		}


		private void showInExplorerToolStripMenuItem_Click(object sender, EventArgs _)
		{
			if (!File.Exists(Logging.LogFilePath))
				return;

			try
			{
				string argument = "/n" + ",/select," + Logging.LogFilePath;
				System.Diagnostics.Process.Start("explorer.exe", argument);
			}
			catch (Exception e)
			{
				Logging.LogUsefulException(e);
			}
		}


		private void LogForm_Load(object sender, EventArgs e)
		{
			ReadLog();
		}


		private void refreshTimer_Tick(object sender, EventArgs e)
		{
			ReadLog();
		}


		private void LogForm_Shown(object sender, EventArgs e)
		{
			logTextBox.ScrollToCaret();
		}


		private void fontToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (FontDialog fontDialog = new FontDialog())
			{
				fontDialog.Font = logTextBox.Font;
				if (fontDialog.ShowDialog() == DialogResult.OK)
				{
					logTextBox.Font = fontDialog.Font;
				}
			}
		}


		private void wrapTextToolStripMenuItem_Click(object sender, EventArgs e)
		{
			wrapTextMenu.Checked = !wrapTextMenu.Checked;
		}


		private void alwaysOnTopToolStripMenuItem_Click(object sender, EventArgs e)
		{
			alwaysOnTopMenu.Checked = !alwaysOnTopMenu.Checked;
		}


		private void wrapTextToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			logTextBox.WordWrap = wrapTextMenu.Checked;
			logTextBox.ScrollToCaret();
		}


		private void alwaysOnTopToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			TopMost = alwaysOnTopMenu.Checked;
		}


		private void clearLogToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Logging.Clear();
			_currentOffset = 0;
			logTextBox.Clear();
		}
	}
}