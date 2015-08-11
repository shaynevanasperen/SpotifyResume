using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SpotifyAPI.Local;

namespace SpotifyResume
{
	static class Program
	{
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new SpotifyResumeAppliationContext());
		}
	}

	class SpotifyResumeAppliationContext : ApplicationContext
	{
		NotifyIcon _trayIcon;
		ContextMenuStrip _trayIconContextMenu;
		ToolStripMenuItem[] _delayMenutItems;
		ToolStripMenuItem _closeMenuItem;
		SpotifyLocalAPI _spotifyLocalApi;
		System.Timers.Timer _resumeTimer;

		public SpotifyResumeAppliationContext()
		{
			Application.ApplicationExit += application_Exit;
			InitializeComponent();
		}

		void InitializeComponent()
		{
			_trayIcon = new NotifyIcon
			{
				BalloonTipIcon = ToolTipIcon.Info,
				BalloonTipTitle = "Resume Spotify",
				Text = "Resume playback on Spotify",
				Icon = Resources.Play,
				Visible = true
			};

			_trayIconContextMenu = new ContextMenuStrip();
			
			_delayMenutItems = new[]
			{
				new ToolStripMenuItem
				{
					Size = new Size(152, 22),
					Text = "Resume after 5 minutes",
					Tag = 1000 * 60 * 5,
					Checked = true
				},
				new ToolStripMenuItem
				{
					Size = new Size(152, 22),
					Text = "Resume after 30 minutes",
					Tag = 1000 * 60 * 30
				},
				new ToolStripMenuItem
				{
					Size = new Size(152, 22),
					Text = "Resume after 1 hour",
					Tag = 1000 * 60 * 60
				},
				new ToolStripMenuItem
				{
					Size = new Size(152, 22),
					Text = "Resume after 2 hours",
					Tag = 1000 * 60 * 60 * 2
				},
				new ToolStripMenuItem
				{
					Size = new Size(152, 22),
					Text = "Resume after 4 hours",
					Tag = 1000 * 60 * 60 * 4
				}
			};
			foreach (var delayMenuItem in _delayMenutItems)
				delayMenuItem.Click += delayMenuItem_Click;

			_closeMenuItem = new ToolStripMenuItem
			{
				Size = new Size(152, 22),
				Text = "Exit",
				Image = Resources.Exit
			};
			_closeMenuItem.Click += closeMenuItem_Click;

			_resumeTimer = new System.Timers.Timer();
			_resumeTimer.Elapsed += resumeTimer_Elapsed;

			_trayIconContextMenu.SuspendLayout();
			_trayIconContextMenu.Items.AddRange(_delayMenutItems.Concat(new[] { _closeMenuItem }).Cast<ToolStripItem>().ToArray());
			_trayIconContextMenu.Size = new Size(153, 70);
			_trayIconContextMenu.ResumeLayout(false);

			_trayIcon.ContextMenuStrip = _trayIconContextMenu;

			_spotifyLocalApi = new SpotifyLocalAPI();
			_spotifyLocalApi.Connect();
			_spotifyLocalApi.ListenForEvents = true;
			_spotifyLocalApi.OnPlayStateChange += spotifyLocalAPI_OnPlayStateChange;

			_resumeTimer.Stop();
			_resumeTimer.Interval = (int)_delayMenutItems.Single(x => x.Checked).Tag;
			_resumeTimer.Start();
		}

		void application_Exit(object sender, EventArgs e)
		{
			_trayIcon.Visible = false;
		}

		void resumeTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			var status = _spotifyLocalApi.GetStatus();
			if (status == null || !status.Online)
			{
				_spotifyLocalApi.Connect();
				status = _spotifyLocalApi.GetStatus();
			}
			if (status != null && status.Online && !status.Playing)
				_spotifyLocalApi.Play();
		}

		void delayMenuItem_Click(object sender, EventArgs e)
		{
			var clicked = (ToolStripMenuItem)sender;
			if (clicked.Checked)
				return;

			clicked.Checked = true;
			foreach (var item in _delayMenutItems.Where(x => x != sender))
				item.Checked = false;

			if (_resumeTimer.Enabled)
			{
				_resumeTimer.Stop();
				_resumeTimer.Interval = (int)clicked.Tag;
				_resumeTimer.Start();
			}
		}

		static void closeMenuItem_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		void spotifyLocalAPI_OnPlayStateChange(PlayStateEventArgs e)
		{
			if (!e.Playing)
			{
				var currentDelayMenuItem = _delayMenutItems.Single(x => x.Checked);
				_trayIcon.BalloonTipText = currentDelayMenuItem.Text;
				_trayIcon.ShowBalloonTip(5000);

				_resumeTimer.Stop();
				_resumeTimer.Interval = (int)currentDelayMenuItem.Tag;
				_resumeTimer.Start();
			}
			else if (_resumeTimer.Enabled)
				_resumeTimer.Stop();
		}
	}
}
