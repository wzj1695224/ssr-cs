﻿using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Shadowsocks.Core;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;


namespace Shadowsocks.View
{
    public class EventParams
    {
        public object sender;
        public EventArgs e;

        public EventParams(object sender, EventArgs e)
        {
            this.sender = sender;
            this.e = e;
        }
    }


    public class MenuViewController
    {
        public INotifyIconController NotifyIconController;

        // yes this is just a menu view controller
        // when config form is closed, it moves away from RAM
        // and it should just do anything related to the config form

        private ShadowsocksController controller;
        private UpdateChecker updateChecker;
        private UpdateFreeNode updateFreeNodeChecker;
        private UpdateSubscribeManager updateSubscribeManager;

        #region Fields: TrayIcon MenuItem 
        public ContextMenu TrayIconMenu { get; private set; }
        // Mode menu
        private MenuItem _modeNoModifyMenu;
        private MenuItem _modeDisableMenu;
        private MenuItem _modePacMenu;
        private MenuItem _modeGlobalMenu;
        // Proxy rule
        private MenuItem _ruleBypassLan;
        private MenuItem _ruleBypassChina;
        private MenuItem _ruleBypassNotChina;
        private MenuItem _ruleUser;
        private MenuItem _ruleDisableBypass;
        // Servers
        private MenuItem _serversMenu;
        private MenuItem SeperatorItem;
        private MenuItem _servSameHostMenu;
        // others
        private MenuItem _LoadBalanceMenu;
        private MenuItem _doUpdateMenu;
        #endregion

        private ConfigForm configForm;
        private SettingsForm settingsForm;
        private ServerLogForm serverLogForm;
        private PortSettingsForm portMapForm;
        private SubscribeForm subScribeForm;
        private LogForm logForm;
        private string _urlToOpen;
        private System.Timers.Timer timerDelayCheckUpdate;

        private bool configfrom_open = false;
        private List<EventParams> eventList = new List<EventParams>();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool DestroyIcon(IntPtr handle);




        public MenuViewController(ShadowsocksController controller)
        {
            this.controller = controller;

            controller.ToggleModeChanged += controller_ToggleModeChanged;
            controller.ToggleRuleModeChanged += controller_ToggleRuleModeChanged;
            controller.ConfigChanged += controller_ConfigChanged;
            controller.PACFileReadyToOpen += controller_FileReadyToOpen;
            controller.UserRuleFileReadyToOpen += controller_FileReadyToOpen;
            controller.Errored += controller_Errored;
            controller.UpdatePACFromGFWListCompleted += controller_UpdatePACFromGFWListCompleted;
            controller.UpdatePACFromGFWListError += controller_UpdatePACFromGFWListError;
            controller.ShowConfigFormEvent += Config_Click;

            InitTrayIconMenu();

            updateChecker = new UpdateChecker();
            updateChecker.NewVersionFound += updateChecker_NewVersionFound;

            updateFreeNodeChecker = new UpdateFreeNode();
            updateFreeNodeChecker.NewFreeNodeFound += updateFreeNodeChecker_NewFreeNodeFound;

            updateSubscribeManager = new UpdateSubscribeManager();

            LoadCurrentConfiguration();

            timerDelayCheckUpdate = new System.Timers.Timer(1000.0 * 10);
            timerDelayCheckUpdate.Elapsed += timer_Elapsed;
            timerDelayCheckUpdate.Start();
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (timerDelayCheckUpdate != null)
            {
                //if (timerDelayCheckUpdate.Interval <= 1000.0 * 30)
                //{
                //    timerDelayCheckUpdate.Interval = 1000.0 * 60 * 5;
                //}
                //else
                {
                    timerDelayCheckUpdate.Interval = 1000.0 * 60 * 60 * 6;
                }
            }
            updateChecker.CheckUpdate(controller.GetCurrentConfiguration());

            Configuration cfg = controller.GetCurrentConfiguration();
            if (cfg.isDefaultConfig() || cfg.nodeFeedAutoUpdate)
            {
                updateSubscribeManager.CreateTask(controller.GetCurrentConfiguration(), updateFreeNodeChecker, -1, !cfg.isDefaultConfig(), false);
            }
        }

        void controller_Errored(object sender, System.IO.ErrorEventArgs e)
        {
            MessageBox.Show(e.GetException().ToString(), String.Format(I18N.GetString("Shadowsocks Error: {0}"), e.GetException().Message));
        }


        private void InitTrayIconMenu()
        {
	        // Mode submenu
	        _modeDisableMenu    = CreateMenuItem("Disable system proxy",       SetProxyMode, ProxyMode.Direct );
	        _modePacMenu        = CreateMenuItem("PAC",                        SetProxyMode, ProxyMode.Pac );
	        _modeGlobalMenu     = CreateMenuItem("Global",                     SetProxyMode, ProxyMode.Global );
	        _modeNoModifyMenu   = CreateMenuItem("No modify system proxy",     SetProxyMode, ProxyMode.NoModify );

            // Proxy rule submenu
            _ruleBypassLan      = CreateMenuItem("Bypass LAN",                 SetRuleMode,  ProxyRuleMode.BypassLan);
            _ruleBypassChina    = CreateMenuItem("Bypass LAN && China",        SetRuleMode,  ProxyRuleMode.BypassLanAndChina);
            _ruleBypassNotChina = CreateMenuItem("Bypass LAN && not China",    SetRuleMode,  ProxyRuleMode.BypassLanAndNotChina);
            _ruleUser           = CreateMenuItem("User custom",                SetRuleMode,  ProxyRuleMode.UserCustom);
            _ruleDisableBypass  = CreateMenuItem("Disable bypass",             SetRuleMode,  ProxyRuleMode.Disable);

            // Servers submenu
            _servSameHostMenu   = CreateMenuItem("Same host for same address", ToggleSameHostForSameAddress);

            // others
            _LoadBalanceMenu    = CreateMenuItem("Load balance",               ToggleLoadBalance);
            _doUpdateMenu       = CreateMenuItem("Update available",           DoUpdateApp);

            // do some config
            _doUpdateMenu.Visible = false;

            TrayIconMenu = new ContextMenu(new[] {
                // Mode
                CreateMenuGroup("Mode", new[] {
	                _modeDisableMenu,
	                _modePacMenu,
	                _modeGlobalMenu,
                    new MenuItem("-"),
                    _modeNoModifyMenu
                }),

                // PAC
                CreateMenuGroup("PAC ", new[] {
                    CreateMenuItem("Update local PAC from Lan IP list",    DoUpdatePacFromLanIpList),
                    new MenuItem("-"),
                    CreateMenuItem("Update local PAC from Chn White list", DoUpdatePacFromChnWhiteList),
                    CreateMenuItem("Update local PAC from Chn IP list",    DoUpdatePacFromChnIpList),
                    CreateMenuItem("Update local PAC from GFWList",        DoUpdatePacFromGfwList),
                    new MenuItem("-"),
                    CreateMenuItem("Update local PAC from Chn Only list",  DoUpdatePacFromChnOnlyList),
                    new MenuItem("-"),
                    CreateMenuItem("Copy PAC URL",                         DoCopyPacUrl),
                    CreateMenuItem("Edit local PAC file...",               DoEditLocalPac),
                    CreateMenuItem("Edit user rule for GFWList...",        DoEditUserRule),
                }),

                // Proxy rule
                CreateMenuGroup("Proxy rule", new[] {
                    _ruleBypassLan,
                    _ruleBypassChina,
                    _ruleBypassNotChina,
                    _ruleUser,
                    new MenuItem("-"),
                    _ruleDisableBypass
                }),

                new MenuItem("-"),
                
                // Servers
                _serversMenu = CreateMenuGroup("Servers", new[] {
                    SeperatorItem = new MenuItem("-"),
                    CreateMenuItem("Edit servers...",             Config_Click),
                    CreateMenuItem("Import servers from file...", Import_Click),
                    new MenuItem("-"),
                    _servSameHostMenu,
                    new MenuItem("-"),
                    CreateMenuItem("Server statistic...",         ShowServerLogItem_Click),
                    CreateMenuItem("Disconnect current",          DisconnectCurrent_Click),
                }),

                // Servers Subscribe
                CreateMenuGroup("Servers Subscribe", new[] {
                    CreateMenuItem("Subscribe setting...",                    SubscribeSetting_Click),
                    CreateMenuItem("Update subscribe SSR node",               DoUpdateSubscribe, true),
                    CreateMenuItem("Update subscribe SSR node(bypass proxy)", DoUpdateSubscribe, false),
                }),

                _LoadBalanceMenu,

                CreateMenuItem("Global settings...", Setting_Click),
                CreateMenuItem("Port settings...",   ShowPortMapItem_Click),
                
                _doUpdateMenu,

                new MenuItem("-"),

                CreateMenuItem("Scan QRCode from screen...",         ScanQRCodeItem_Click),
                CreateMenuItem("Import SSR links from clipboard...", CopyAddress_Click),

                new MenuItem("-"),

                CreateMenuGroup("Help", new[] {
                    CreateMenuItem("Check update",         CheckUpdate_Click),
                    CreateMenuItem("Show logs...",         ShowLogItem_Click),
                    CreateMenuItem("Open wiki...",         OpenWiki_Click),
                    CreateMenuItem("Feedback...",          FeedbackItem_Click),
                    new MenuItem("-"),
                    CreateMenuItem("Gen custom QRCode...", showURLFromQRCode),
                    CreateMenuItem("Reset password...",    ResetPasswordItem_Click),
                    new MenuItem("-"),
                    CreateMenuItem("About...",             AboutItem_Click),
                    CreateMenuItem("Donate...",            DonateItem_Click),
                }),

                CreateMenuItem("Quit", Quit_Click)
            });
        }


        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
            NotifyIconController.UpdateTrayIcon();
        }

        private void controller_ToggleModeChanged(object sender, EventArgs e)
        {
            Configuration config = controller.GetCurrentConfiguration();
            UpdateSysProxyMode(config);
        }

        private void controller_ToggleRuleModeChanged(object sender, EventArgs e)
        {
            Configuration config = controller.GetCurrentConfiguration();
            UpdateProxyRule(config);
        }

        void controller_FileReadyToOpen(object sender, ShadowsocksController.PathEventArgs e)
        {
            string argument = @"/select, " + e.Path;

            Process.Start("explorer.exe", argument);
        }

        void controller_UpdatePACFromGFWListError(object sender, System.IO.ErrorEventArgs e)
        {
            GFWListUpdater updater = (GFWListUpdater)sender;
            NotifyIconController.ShowBalloonTip(I18N.GetString("Failed to update PAC file"), e.GetException().Message, ToolTipIcon.Error, 5000);
            Logging.LogUsefulException(e.GetException());
        }

        void controller_UpdatePACFromGFWListCompleted(object sender, GFWListUpdater.ResultEventArgs e)
        {
            GFWListUpdater updater = (GFWListUpdater)sender;
            string result = e.Success ?
                (updater.update_type <= 1 ? I18N.GetString("PAC updated") : I18N.GetString("Domain white list list updated"))
                : I18N.GetString("No updates found. Please report to GFWList if you have problems with it.");
            NotifyIconController.ShowBalloonTip(I18N.GetString("Shadowsocks"), result, ToolTipIcon.Info, 1000);
        }




        void updateFreeNodeChecker_NewFreeNodeFound(object sender, EventArgs e)
        {
            if (configfrom_open)
            {
                eventList.Add(new EventParams(sender, e));
                return;
            }

            string lastGroup = null;
            int count = 0;
            if (!String.IsNullOrEmpty(updateFreeNodeChecker.FreeNodeResult))
            {
                List<string> urls = new List<string>();
                updateFreeNodeChecker.FreeNodeResult = updateFreeNodeChecker.FreeNodeResult.TrimEnd('\r', '\n', ' ');
                Configuration config = controller.GetCurrentConfiguration();
                Server selected_server = null;
                if (config.index >= 0 && config.index < config.configs.Count)
                {
                    selected_server = config.configs[config.index];
                }
                try
                {
                    updateFreeNodeChecker.FreeNodeResult = Util.Base64.DecodeBase64(updateFreeNodeChecker.FreeNodeResult);
                }
                catch
                {
                    updateFreeNodeChecker.FreeNodeResult = "";
                }
                int max_node_num = 0;

                Match match_maxnum = Regex.Match(updateFreeNodeChecker.FreeNodeResult, "^MAX=([0-9]+)");
                if (match_maxnum.Success)
                {
                    try
                    {
                        max_node_num = Convert.ToInt32(match_maxnum.Groups[1].Value, 10);
                    }
                    catch
                    {

                    }
                }
                URL_Split(updateFreeNodeChecker.FreeNodeResult, ref urls);
                for (int i = urls.Count - 1; i >= 0; --i)
                {
                    if (!urls[i].StartsWith("ssr"))
                        urls.RemoveAt(i);
                }
                if (urls.Count > 0)
                {
                    bool keep_selected_server = false; // set 'false' if import all nodes
                    if (max_node_num <= 0 || max_node_num >= urls.Count)
                    {
                        urls.Reverse();
                    }
                    else
                    {
                        Random r = new Random();
                        Util.Utils.Shuffle(urls, r);
                        urls.RemoveRange(max_node_num, urls.Count - max_node_num);
                        if (!config.isDefaultConfig())
                            keep_selected_server = true;
                    }
                    string curGroup = null;
                    foreach (string url in urls)
                    {
                        try // try get group name
                        {
                            Server server = new Server(url, null);
                            if (!String.IsNullOrEmpty(server.group))
                            {
                                curGroup = server.group;
                                break;
                            }
                        }
                        catch
                        { }
                    }
                    string subscribeURL = updateSubscribeManager.Url;
                    if (String.IsNullOrEmpty(curGroup))
                    {
                        curGroup = subscribeURL;
                    }
                    for (int i = 0; i < config.serverSubscribes.Count; ++i)
                    {
                        if (subscribeURL == config.serverSubscribes[i].URL)
                        {
                            lastGroup = config.serverSubscribes[i].Group;
                            config.serverSubscribes[i].Group = curGroup;
                            break;
                        }
                    }
                    if (String.IsNullOrEmpty(lastGroup))
                    {
                        lastGroup = curGroup;
                    }

                    if (keep_selected_server && selected_server.group == curGroup)
                    {
                        bool match = false;
                        for (int i = 0; i < urls.Count; ++i)
                        {
                            try
                            {
                                Server server = new Server(urls[i], null);
                                if (selected_server.isMatchServer(server))
                                {
                                    match = true;
                                    break;
                                }
                            }
                            catch
                            { }
                        }
                        if (!match)
                        {
                            urls.RemoveAt(0);
                            urls.Add(selected_server.GetSSRLinkForServer());
                        }
                    }

                    // import all, find difference
                    {
                        Dictionary<string, Server> old_servers = new Dictionary<string, Server>();
                        Dictionary<string, Server> old_insert_servers = new Dictionary<string, Server>();
                        if (!String.IsNullOrEmpty(lastGroup))
                        {
                            for (int i = config.configs.Count - 1; i >= 0; --i)
                            {
                                if (lastGroup == config.configs[i].group)
                                {
                                    old_servers[config.configs[i].id] = config.configs[i];
                                }
                            }
                        }
                        foreach (string url in urls)
                        {
                            try
                            {
                                Server server = new Server(url, curGroup);
                                bool match = false;
                                if (!match)
                                {
                                    foreach (KeyValuePair<string, Server> pair in old_insert_servers)
                                    {
                                        if (server.isMatchServer(pair.Value))
                                        {
                                            match = true;
                                            break;
                                        }
                                    }
                                }
                                old_insert_servers[server.id] = server;
                                if (!match)
                                {
                                    foreach (KeyValuePair<string, Server> pair in old_servers)
                                    {
                                        if (server.isMatchServer(pair.Value))
                                        {
                                            match = true;
                                            old_servers.Remove(pair.Key);
                                            pair.Value.CopyServerInfo(server);
                                            ++count;
                                            break;
                                        }
                                    }
                                }
                                if (!match)
                                {
                                    int insert_index = config.configs.Count;
                                    for (int index = config.configs.Count - 1; index >= 0; --index)
                                    {
                                        if (config.configs[index].group == curGroup)
                                        {
                                            insert_index = index + 1;
                                            break;
                                        }
                                    }
                                    config.configs.Insert(insert_index, server);
                                    ++count;
                                }
                            }
                            catch
                            { }
                        }
                        foreach (KeyValuePair<string, Server> pair in old_servers)
                        {
                            for (int i = config.configs.Count - 1; i >= 0; --i)
                            {
                                if (config.configs[i].id == pair.Key)
                                {
                                    config.configs.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                        controller.SaveServersConfig(config);
                    }
                    config = controller.GetCurrentConfiguration();
                    if (selected_server != null)
                    {
                        bool match = false;
                        for (int i = config.configs.Count - 1; i >= 0; --i)
                        {
                            if (config.configs[i].id == selected_server.id)
                            {
                                config.index = i;
                                match = true;
                                break;
                            }
                            else if (config.configs[i].group == selected_server.group)
                            {
                                if (config.configs[i].isMatchServer(selected_server))
                                {
                                    config.index = i;
                                    match = true;
                                    break;
                                }
                            }
                        }
                        if (!match)
                        {
                            config.index = config.configs.Count - 1;
                        }
                    }
                    else
                    {
                        config.index = config.configs.Count - 1;
                    }
                    if (count > 0)
                    {
                        for (int i = 0; i < config.serverSubscribes.Count; ++i)
                        {
                            if (config.serverSubscribes[i].URL == updateFreeNodeChecker.subscribeTask.URL)
                            {
                                config.serverSubscribes[i].LastUpdateTime = (UInt64)Math.Floor(DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
                            }
                        }
                    }
                    controller.SaveServersConfig(config);
                }
            }

            if (count > 0)
            {
                if (updateFreeNodeChecker.Notify)
                    NotifyIconController.ShowBalloonTip(I18N.GetString("Success"),
                        String.Format(I18N.GetString("Update subscribe {0} success"), lastGroup), ToolTipIcon.Info, 10000);
            }
            else
            {
                if (lastGroup == null)
                {
                    lastGroup = updateFreeNodeChecker.subscribeTask.Group;
                    //lastGroup = updateSubscribeManager.LastGroup;
                }
                NotifyIconController.ShowBalloonTip(I18N.GetString("Error"),
                    String.Format(I18N.GetString("Update subscribe {0} failure"), lastGroup), ToolTipIcon.Info, 10000);
            }

            updateSubscribeManager.Next();
        }




        void updateChecker_NewVersionFound(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(updateChecker.LatestVersionNumber))
            {
                Logging.Log(LogLevel.Error, "connect to update server error");
            }
            else
            {
                if (!this._doUpdateMenu.Visible)
                {
                    NotifyIconController.ShowBalloonTip(String.Format(I18N.GetString("{0} {1} Update Found"), UpdateChecker.Name, updateChecker.LatestVersionNumber),
                        I18N.GetString("Click menu to download"), ToolTipIcon.Info, 10000);
                    // _notifyIcon.BalloonTipClicked += notifyIcon1_BalloonTipClicked;

                    timerDelayCheckUpdate.Elapsed -= timer_Elapsed;
                    timerDelayCheckUpdate.Stop();
                    timerDelayCheckUpdate = null;
                }
                this._doUpdateMenu.Visible = true;
                this._doUpdateMenu.Text = String.Format(I18N.GetString("New version {0} {1} available"), UpdateChecker.Name, updateChecker.LatestVersionNumber);
            }
        }

        void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            //System.Diagnostics.Process.Start(updateChecker.LatestVersionURL);
            // _notifyIcon.BalloonTipClicked -= notifyIcon1_BalloonTipClicked;
        }

        private void UpdateSysProxyMode(Configuration config)
        {
            _modeNoModifyMenu.Checked = config.sysProxyMode == (int)ProxyMode.NoModify;
            _modeDisableMenu.Checked = config.sysProxyMode == (int)ProxyMode.Direct;
            _modePacMenu.Checked = config.sysProxyMode == (int)ProxyMode.Pac;
            _modeGlobalMenu.Checked = config.sysProxyMode == (int)ProxyMode.Global;
        }

        private void UpdateProxyRule(Configuration config)
        {
            _ruleDisableBypass.Checked = config.proxyRuleMode == (int)ProxyRuleMode.Disable;
            _ruleBypassLan.Checked = config.proxyRuleMode == (int)ProxyRuleMode.BypassLan;
            _ruleBypassChina.Checked = config.proxyRuleMode == (int)ProxyRuleMode.BypassLanAndChina;
            _ruleBypassNotChina.Checked = config.proxyRuleMode == (int)ProxyRuleMode.BypassLanAndNotChina;
            _ruleUser.Checked = config.proxyRuleMode == (int)ProxyRuleMode.UserCustom;
        }

        private void LoadCurrentConfiguration()
        {
            Configuration config = controller.GetCurrentConfiguration();
            UpdateServersMenu();
            UpdateSysProxyMode(config);

            UpdateProxyRule(config);

            _LoadBalanceMenu.Checked = config.random;
            _servSameHostMenu.Checked = config.sameHostForSameTarget;
        }

        private void UpdateServersMenu()
        {
            var items = _serversMenu.MenuItems;
            while (items[0] != SeperatorItem)
            {
                items.RemoveAt(0);
            }

            Configuration configuration = controller.GetCurrentConfiguration();
            SortedDictionary<string, MenuItem> group = new SortedDictionary<string, MenuItem>();
            const string def_group = "!(no group)";
            string select_group = "";
            for (int i = 0; i < configuration.configs.Count; i++)
            {
                string group_name;
                Server server = configuration.configs[i];
                if (string.IsNullOrEmpty(server.group))
                    group_name = def_group;
                else
                    group_name = server.group;

                MenuItem item = new MenuItem(server.FriendlyName());
                item.Tag = i;
                item.Click += AServerItem_Click;
                if (configuration.index == i)
                {
                    item.Checked = true;
                    select_group = group_name;
                }

                if (group.ContainsKey(group_name))
                {
                    group[group_name].MenuItems.Add(item);
                }
                else
                {
                    group[group_name] = new MenuItem(group_name, new MenuItem[1] { item });
                }
            }
            {
                int i = 0;
                foreach (KeyValuePair<string, MenuItem> pair in group)
                {
                    if (pair.Key == def_group)
                    {
                        pair.Value.Text = "(empty group)";
                    }
                    if (pair.Key == select_group)
                    {
                        pair.Value.Text = "● " + pair.Value.Text;
                    }
                    else
                    {
                        pair.Value.Text = "　" + pair.Value.Text;
                    }
                    items.Add(i, pair.Value);
                    ++i;
                }
            }
        }

        private void ShowConfigForm(bool addNode)
        {
            if (configForm != null)
            {
                configForm.Activate();
                if (addNode)
                {
                    Configuration cfg = controller.GetCurrentConfiguration();
                    configForm.SetServerListSelectedIndex(cfg.index + 1);
                }
            }
            else
            {
                configfrom_open = true;
                configForm = new ConfigForm(controller, updateChecker, addNode ? -1 : -2);
                configForm.Show();
                configForm.Activate();
                configForm.BringToFront();
                configForm.FormClosed += configForm_FormClosed;
            }
        }

        private void ShowConfigForm(int index)
        {
            if (configForm != null)
            {
                configForm.Activate();
            }
            else
            {
                configfrom_open = true;
                configForm = new ConfigForm(controller, updateChecker, index);
                configForm.Show();
                configForm.Activate();
                configForm.BringToFront();
                configForm.FormClosed += configForm_FormClosed;
            }
        }

        private void ShowSettingForm()
        {
            if (settingsForm != null)
            {
                settingsForm.Activate();
            }
            else
            {
                settingsForm = new SettingsForm(controller);
                settingsForm.Show();
                settingsForm.Activate();
                settingsForm.BringToFront();
                settingsForm.FormClosed += settingsForm_FormClosed;
            }
        }

        private void ShowPortMapForm()
        {
            if (portMapForm != null)
            {
                portMapForm.Activate();
                portMapForm.Update();
                if (portMapForm.WindowState == FormWindowState.Minimized)
                {
                    portMapForm.WindowState = FormWindowState.Normal;
                }
            }
            else
            {
                portMapForm = new PortSettingsForm(controller);
                portMapForm.Show();
                portMapForm.Activate();
                portMapForm.BringToFront();
                portMapForm.FormClosed += portMapForm_FormClosed;
            }
        }

        private void ShowServerLogForm()
        {
            if (serverLogForm != null)
            {
                serverLogForm.Activate();
                serverLogForm.Update();
                if (serverLogForm.WindowState == FormWindowState.Minimized)
                {
                    serverLogForm.WindowState = FormWindowState.Normal;
                }
            }
            else
            {
                serverLogForm = new ServerLogForm(controller);
                serverLogForm.Show();
                serverLogForm.Activate();
                serverLogForm.BringToFront();
                serverLogForm.FormClosed += serverLogForm_FormClosed;
            }
        }

        private void ShowGlobalLogForm()
        {
            if (logForm != null)
            {
                logForm.Activate();
                logForm.Update();
                if (logForm.WindowState == FormWindowState.Minimized)
                {
                    logForm.WindowState = FormWindowState.Normal;
                }
            }
            else
            {
                logForm = new LogForm(controller);
                logForm.Show();
                logForm.Activate();
                logForm.BringToFront();
                logForm.FormClosed += globalLogForm_FormClosed;
            }
        }

        private void ShowSubscribeSettingForm()
        {
            if (subScribeForm != null)
            {
                subScribeForm.Activate();
                subScribeForm.Update();
                if (subScribeForm.WindowState == FormWindowState.Minimized)
                {
                    subScribeForm.WindowState = FormWindowState.Normal;
                }
            }
            else
            {
                subScribeForm = new SubscribeForm(controller);
                subScribeForm.Show();
                subScribeForm.Activate();
                subScribeForm.BringToFront();
                subScribeForm.FormClosed += subScribeForm_FormClosed;
            }
        }

        void configForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            configForm = null;
            configfrom_open = false;
            Util.Utils.ReleaseMemory();
            if (eventList.Count > 0)
            {
                foreach (EventParams p in eventList)
                {
                    updateFreeNodeChecker_NewFreeNodeFound(p.sender, p.e);
                }
                eventList.Clear();
            }
        }

        void settingsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            settingsForm = null;
            Util.Utils.ReleaseMemory();
        }

        void serverLogForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            serverLogForm = null;
            Util.Utils.ReleaseMemory();
        }

        void portMapForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            portMapForm = null;
            Util.Utils.ReleaseMemory();
        }

        void globalLogForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            logForm = null;
            Util.Utils.ReleaseMemory();
        }

        void subScribeForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            subScribeForm = null;
        }

        private void Config_Click(object sender, EventArgs e)
        {
            if (typeof(int) == sender.GetType())
            {
                ShowConfigForm((int)sender);
            }
            else
            {
                ShowConfigForm(false);
            }
        }

        private void Import_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.InitialDirectory = System.Windows.Forms.Application.StartupPath;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    string name = dlg.FileName;
                    Configuration cfg = Configuration.LoadFile(name);
                    if (cfg == null || (cfg.configs.Count == 1 && cfg.configs[0].server == Configuration.GetDefaultServer().server))
                    {
                        MessageBox.Show("Load config file failed", "ShadowsocksR");
                    }
                    else
                    {
                        controller.MergeConfiguration(cfg);
                        LoadCurrentConfiguration();
                    }
                }
            }
        }

        private void Setting_Click(object sender, EventArgs e)
        {
            ShowSettingForm();
        }

        private void Quit_Click(object sender, EventArgs e)
        {
            controller.Stop();
            if (configForm != null)
            {
                configForm.Close();
                configForm = null;
            }
            if (serverLogForm != null)
            {
                serverLogForm.Close();
                serverLogForm = null;
            }
            if (timerDelayCheckUpdate != null)
            {
                timerDelayCheckUpdate.Elapsed -= timer_Elapsed;
                timerDelayCheckUpdate.Stop();
                timerDelayCheckUpdate = null;
            }
            
            ServiceManager.EventBus.NotifyAppExit(this, 0);

            Application.Exit();
        }

        private void OpenWiki_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/shadowsocksrr/shadowsocks-rss/wiki");
        }

        private void FeedbackItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/shadowsocksrr/shadowsocksr-csharp/issues/new");
        }

        private void ResetPasswordItem_Click(object sender, EventArgs e)
        {
            ResetPassword dlg = new ResetPassword();
            dlg.Show();
            dlg.Activate();
        }

        private void AboutItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://breakwa11.github.io");
        }

        private void DonateItem_Click(object sender, EventArgs e)
        {
            NotifyIconController.ShowBalloonTip(I18N.GetString("Donate"), I18N.GetString("Please contract to breakwa11 to get more infomation"), ToolTipIcon.Info, 10000);
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        public void TrayIconClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int SCA_key = GetAsyncKeyState(Keys.ShiftKey) < 0 ? 1 : 0;
                SCA_key |= GetAsyncKeyState(Keys.ControlKey) < 0 ? 2 : 0;
                SCA_key |= GetAsyncKeyState(Keys.Menu) < 0 ? 4 : 0;
                if (SCA_key == 2)
                {
                    ShowServerLogForm();
                }
                else if (SCA_key == 1)
                {
                    ShowSettingForm();
                }
                else if (SCA_key == 4)
                {
                    ShowPortMapForm();
                }
                else
                {
                    ShowConfigForm(false);
                }
            }
            else if (e.Button == MouseButtons.Middle)
            {
                ShowServerLogForm();
            }
        }

        private void AServerItem_Click(object sender, EventArgs e)
        {
            Configuration config = controller.GetCurrentConfiguration();
            Console.WriteLine("config.checkSwitchAutoCloseAll:" + config.checkSwitchAutoCloseAll);
            if (config.checkSwitchAutoCloseAll)
            {
                controller.DisconnectAllConnections();
            }
            MenuItem item = (MenuItem)sender;
            controller.SelectServerIndex((int)item.Tag);
        }

        private void CheckUpdate_Click(object sender, EventArgs e)
        {
            updateChecker.CheckUpdate(controller.GetCurrentConfiguration());
        }

        private void ShowLogItem_Click(object sender, EventArgs e)
        {
            ShowGlobalLogForm();
        }

        private void ShowPortMapItem_Click(object sender, EventArgs e)
        {
            ShowPortMapForm();
        }

        private void ShowServerLogItem_Click(object sender, EventArgs e)
        {
            ShowServerLogForm();
        }

        private void SubscribeSetting_Click(object sender, EventArgs e)
        {
            ShowSubscribeSettingForm();
        }

        private void DisconnectCurrent_Click(object sender, EventArgs e)
        {
            controller.DisconnectAllConnections();
        }

        private void URL_Split(string text, ref List<string> out_urls)
        {
            if (String.IsNullOrEmpty(text))
            {
                return;
            }
            int ss_index = text.IndexOf("ss://", 1, StringComparison.OrdinalIgnoreCase);
            int ssr_index = text.IndexOf("ssr://", 1, StringComparison.OrdinalIgnoreCase);
            int index = ss_index;
            if (index == -1 || index > ssr_index && ssr_index != -1) index = ssr_index;
            if (index == -1)
            {
                out_urls.Insert(0, text);
            }
            else
            {
                out_urls.Insert(0, text.Substring(0, index));
                URL_Split(text.Substring(index), ref out_urls);
            }
        }

        private void CopyAddress_Click(object sender, EventArgs e)
        {
            try
            {
                IDataObject iData = Clipboard.GetDataObject();
                if (iData.GetDataPresent(DataFormats.Text))
                {
                    List<string> urls = new List<string>();
                    URL_Split((string)iData.GetData(DataFormats.Text), ref urls);
                    int count = 0;
                    foreach (string url in urls)
                    {
                        if (controller.AddServerBySSURL(url))
                            ++count;
                    }
                    if (count > 0)
                        ShowConfigForm(true);
                }
            }
            catch
            {

            }
        }

        private bool ScanQRCode(Screen screen, Bitmap fullImage, Rectangle cropRect, out string url, out Rectangle rect)
        {
            using (Bitmap target = new Bitmap(cropRect.Width, cropRect.Height))
            {
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(fullImage, new Rectangle(0, 0, cropRect.Width, cropRect.Height),
                                    cropRect,
                                    GraphicsUnit.Pixel);
                }
                var source = new BitmapLuminanceSource(target);
                var bitmap = new BinaryBitmap(new HybridBinarizer(source));
                QRCodeReader reader = new QRCodeReader();
                var result = reader.decode(bitmap);
                if (result != null)
                {
                    url = result.Text;
                    double minX = Int32.MaxValue, minY = Int32.MaxValue, maxX = 0, maxY = 0;
                    foreach (ResultPoint point in result.ResultPoints)
                    {
                        minX = Math.Min(minX, point.X);
                        minY = Math.Min(minY, point.Y);
                        maxX = Math.Max(maxX, point.X);
                        maxY = Math.Max(maxY, point.Y);
                    }
                    //rect = new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
                    rect = new Rectangle(cropRect.Left + (int)minX, cropRect.Top + (int)minY, (int)(maxX - minX), (int)(maxY - minY));
                    return true;
                }
            }
            url = "";
            rect = new Rectangle();
            return false;
        }

        private bool ScanQRCodeStretch(Screen screen, Bitmap fullImage, Rectangle cropRect, double mul, out string url, out Rectangle rect)
        {
            using (Bitmap target = new Bitmap((int)(cropRect.Width * mul), (int)(cropRect.Height * mul)))
            {
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(fullImage, new Rectangle(0, 0, target.Width, target.Height),
                                    cropRect,
                                    GraphicsUnit.Pixel);
                }
                var source = new BitmapLuminanceSource(target);
                var bitmap = new BinaryBitmap(new HybridBinarizer(source));
                QRCodeReader reader = new QRCodeReader();
                var result = reader.decode(bitmap);
                if (result != null)
                {
                    url = result.Text;
                    double minX = Int32.MaxValue, minY = Int32.MaxValue, maxX = 0, maxY = 0;
                    foreach (ResultPoint point in result.ResultPoints)
                    {
                        minX = Math.Min(minX, point.X);
                        minY = Math.Min(minY, point.Y);
                        maxX = Math.Max(maxX, point.X);
                        maxY = Math.Max(maxY, point.Y);
                    }
                    //rect = new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
                    rect = new Rectangle(cropRect.Left + (int)(minX / mul), cropRect.Top + (int)(minY / mul), (int)((maxX - minX) / mul), (int)((maxY - minY) / mul));
                    return true;
                }
            }
            url = "";
            rect = new Rectangle();
            return false;
        }

        private Rectangle GetScanRect(int width, int height, int index, out double stretch)
        {
            stretch = 1;
            if (index < 5)
            {
                const int div = 5;
                int w = width * 3 / div;
                int h = height * 3 / div;
                Point[] pt = new Point[5] {
                    new Point(1, 1),

                    new Point(0, 0),
                    new Point(0, 2),
                    new Point(2, 0),
                    new Point(2, 2),
                };
                return new Rectangle(pt[index].X * width / div, pt[index].Y * height / div, w, h);
            }
            {
                const int base_index = 5;
                if (index < base_index + 6)
                {
                    double[] s = new double[] {
                        1,
                        2,
                        3,
                        4,
                        6,
                        8
                    };
                    stretch = 1 / s[index - base_index];
                    return new Rectangle(0, 0, width, height);
                }
            }
            {
                const int base_index = 11;
                if (index < base_index + 8)
                {
                    const int hdiv = 7;
                    const int vdiv = 5;
                    int w = width * 3 / hdiv;
                    int h = height * 3 / vdiv;
                    Point[] pt = new Point[8] {
                        new Point(1, 1),
                        new Point(3, 1),

                        new Point(0, 0),
                        new Point(0, 2),

                        new Point(2, 0),
                        new Point(2, 2),

                        new Point(4, 0),
                        new Point(4, 2),
                    };
                    return new Rectangle(pt[index - base_index].X * width / hdiv, pt[index - base_index].Y * height / vdiv, w, h);
                }
            }
            return new Rectangle(0, 0, 0, 0);
        }

        private void ScanScreenQRCode(bool ss_only)
        {
            Thread.Sleep(100);
            foreach (Screen screen in Screen.AllScreens)
            {
                Point screen_size = Util.Utils.GetScreenPhysicalSize();
                using (Bitmap fullImage = new Bitmap(screen_size.X,
                                                screen_size.Y))
                {
                    using (Graphics g = Graphics.FromImage(fullImage))
                    {
                        g.CopyFromScreen(screen.Bounds.X,
                                         screen.Bounds.Y,
                                         0, 0,
                                         fullImage.Size,
                                         CopyPixelOperation.SourceCopy);
                    }
                    bool decode_fail = false;
                    for (int i = 0; i < 100; i++)
                    {
                        double stretch;
                        Rectangle cropRect = GetScanRect(fullImage.Width, fullImage.Height, i, out stretch);
                        if (cropRect.Width == 0)
                            break;

                        string url;
                        Rectangle rect;
                        if (stretch == 1 ? ScanQRCode(screen, fullImage, cropRect, out url, out rect) : ScanQRCodeStretch(screen, fullImage, cropRect, stretch, out url, out rect))
                        {
                            var success = controller.AddServerBySSURL(url);
                            QRCodeSplashForm splash = new QRCodeSplashForm();
                            if (success)
                            {
                                splash.FormClosed += splash_FormClosed;
                            }
                            else if (!ss_only)
                            {
                                _urlToOpen = url;
                                //if (url.StartsWith("http://") || url.StartsWith("https://"))
                                //    splash.FormClosed += openURLFromQRCode;
                                //else
                                splash.FormClosed += showURLFromQRCode;
                            }
                            else
                            {
                                decode_fail = true;
                                continue;
                            }
                            splash.Location = new Point(screen.Bounds.X, screen.Bounds.Y);
                            double dpi = Screen.PrimaryScreen.Bounds.Width / (double)screen_size.X;
                            splash.TargetRect = new Rectangle(
                                (int)(rect.Left * dpi + screen.Bounds.X),
                                (int)(rect.Top * dpi + screen.Bounds.Y),
                                (int)(rect.Width * dpi),
                                (int)(rect.Height * dpi));
                            splash.Size = new Size(fullImage.Width, fullImage.Height);
                            splash.Show();
                            return;
                        }
                    }
                    if (decode_fail)
                    {
                        MessageBox.Show(I18N.GetString("Failed to decode QRCode"));
                        return;
                    }
                }
            }
            MessageBox.Show(I18N.GetString("No QRCode found. Try to zoom in or move it to the center of the screen."));
        }

        private void ScanQRCodeItem_Click(object sender, EventArgs e)
        {
            ScanScreenQRCode(false);
        }

        void splash_FormClosed(object sender, FormClosedEventArgs e)
        {
            ShowConfigForm(true);
        }

        void openURLFromQRCode(object sender, FormClosedEventArgs e)
        {
            Process.Start(_urlToOpen);
        }

        void showURLFromQRCode()
        {
            ShowTextForm dlg = new ShowTextForm("QRCode", _urlToOpen);
            dlg.Show();
            dlg.Activate();
            dlg.BringToFront();
        }

        void showURLFromQRCode(object sender, FormClosedEventArgs e)
        {
            showURLFromQRCode();
        }




        #region Controller Wrapper

        public void SetProxyMode(ProxyMode mode)
        {
            controller.ToggleMode(mode);
        }

        public void SetRuleMode(ProxyRuleMode mode)
        {
            controller.ToggleRuleMode((int)mode);
        }


        private void ToggleSameHostForSameAddress(object sender, EventArgs e)
        {
            _servSameHostMenu.Checked = !_servSameHostMenu.Checked;
            controller.ToggleSameHostForSameTargetRandom(_servSameHostMenu.Checked);
        }

        private void ToggleLoadBalance(object sender, EventArgs e)
        {
            _LoadBalanceMenu.Checked = !_LoadBalanceMenu.Checked;
            controller.ToggleSelectRandom(_LoadBalanceMenu.Checked);
        }


        private void DoUpdateApp(object sender, EventArgs e)
        {
            Process.Start(updateChecker.LatestVersionURL);
        }


        private void DoUpdatePacFromLanIpList(object sender, EventArgs e)
        {
            controller.UpdatePACFromOnlinePac("https://raw.githubusercontent.com/shadowsocksrr/breakwa11.github.io/master/ssr/ss_lanip.pac");
        }

        private void DoUpdatePacFromChnWhiteList(object sender, EventArgs e)
        {
            controller.UpdatePACFromOnlinePac("https://raw.githubusercontent.com/shadowsocksrr/breakwa11.github.io/master/ssr/ss_white.pac");
        }

        private void DoUpdatePacFromChnIpList(object sender, EventArgs e)
        {
            controller.UpdatePACFromOnlinePac("https://raw.githubusercontent.com/shadowsocksrr/breakwa11.github.io/master/ssr/ss_cnip.pac");
        }

        private void DoUpdatePacFromGfwList(object sender, EventArgs e)
        {
            controller.UpdatePACFromGFWList();
        }

        private void DoUpdatePacFromChnOnlyList(object sender, EventArgs e)
        {
            controller.UpdatePACFromOnlinePac("https://raw.githubusercontent.com/shadowsocksrr/breakwa11.github.io/master/ssr/ss_white_r.pac");
        }


        private void DoCopyPacUrl(object sender, EventArgs e)
        {
            var config = controller.GetCurrentConfiguration();

            var port = config.localPort;
            var auth = config.localAuthPassword;
            var t = Util.Utils.GetTimestamp(DateTime.Now);
            var url = $"http://127.0.0.1:{port}/pac?auth={auth}&t={t}";

            Clipboard.SetText(url);
        }

        private void DoEditLocalPac(object sender, EventArgs e)
        {
            controller.TouchPACFile();
        }

        private void DoEditUserRule(object sender, EventArgs e)
        {
            controller.TouchUserRuleFile();
        }


        private void DoUpdateSubscribe(bool useProxy)
        {
            updateSubscribeManager.CreateTask(controller.GetCurrentConfiguration(), updateFreeNodeChecker, -1, useProxy, true);
        }

        #endregion




        #region Factory Method

        private static MenuItem CreateMenuItem(string text, EventHandler click)
        {
            return new MenuItem(I18N.GetString(text), click);
        }

        private static MenuItem CreateMenuItem(string text, Action click)
        {
            return CreateMenuItem(text, (sender, e) => click());
        }

        private static MenuItem CreateMenuItem<T>(string text, Action<T> action, T param)
        {
            return CreateMenuItem(text, (sender, e) => action(param));
        }

        private static MenuItem CreateMenuGroup(string text, MenuItem[] items)
        {
            return new MenuItem(I18N.GetString(text), items);
        }

        #endregion

    }

}
