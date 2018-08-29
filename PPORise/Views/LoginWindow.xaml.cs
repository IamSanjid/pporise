using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using MaterialDesignThemes.Wpf;
using PPOBot;

// ReSharper disable once CheckNamespace
namespace PPORise
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class LoginWindow : Window
    {
        #region CUSTOM EVENTS
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
        internal void EnableBlur()
        {
            var windowHelper = new WindowInteropHelper(this);

            var accent = new AccentPolicy();
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;

            var accentStructSize = Marshal.SizeOf(accent);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EnableBlur();
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
        #endregion
        private readonly BotClient _bot;
        public bool ShowAccounts { get; set; }
        public string Username => UsernameTextBox.Text.Trim();
        public string Password => PasswordTextBox.Password;
        public bool HasProxy => ProxyCheckBox?.IsChecked != null && ProxyCheckBox.IsChecked.Value;
        public bool HasHttpProxy => HttpProxyCheck?.IsChecked != null && HttpProxyCheck.IsChecked.Value;

        public int ProxyVersion
        {
            set
            {
                switch (value)
                {
                    case 4:
                        Socks4RadioButton.IsChecked = true;
                        break;
                    case 5:
                        Socks5RadioButton.IsChecked = true;
                        break;
                }
            }
            get => Socks4RadioButton.IsChecked != null && Socks4RadioButton.IsChecked.Value ? 4 : 5;
        }
        public string ProxyHost => ProxyHostTextBox.Text.Trim();

        public int ProxyPort { get; private set; } = -1;

        public string ProxyUsername => ProxyUsernameTextBox.Text.Trim();

        public string ProxyPassword => ProxyPasswordTextBox.Password;

        public string HttpProxyHost => HttpProxyHostTextBox.Text.Trim();

        public int HttpProxyPort { get; private set; } = -1;


        public LoginWindow(BotClient bot)
        {
            InitializeComponent();

            _bot = bot;

            Title = App.Name + " - " + Title;
            UsernameTextBox.Focus();

            RefreshAccountList();
            RefreshVisibility();

            if (bot.AccountManager.Accounts.Count > 0)
                ShowAccounts_Click(null, null);
        }
        public void RefreshAccountList()
        {
            IEnumerable<Account> accountList;
            lock (_bot)
            {
                accountList = _bot.AccountManager.Accounts.Values.OrderBy(e => e.Name);
            }
            var accountListView = new List<string>();
            foreach (Account account in accountList)
            {
                accountListView.Add(account.FileName);
            }

            AccountListView.ItemsSource = accountListView;
            AccountListView.Items.Refresh();
        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (Username.Length == 0)
            {
                UsernameTextBox.Focus();
                return;
            }
            if (Password.Length == 0)
            {
                PasswordTextBox.Focus();
                return;
            }

            if (HasProxy)
            {
                if (int.TryParse(ProxyPortTextBox.Text.Trim(), out int port) && port >= 0 && port <= 65535)
                {
                    ProxyPort = port;
                    if (HasHttpProxy)
                    {
                        if (int.TryParse(HttpProxyPortTextBox.Text.Trim(), out int httpport) && httpport >= 0 && httpport <= 65535)
                        {
                            HttpProxyPort = httpport;
                            DialogResult = true;
                        }
                    }
                    else
                        DialogResult = true;
                }
            }
            else if (HasHttpProxy)
            {
                if (int.TryParse(HttpProxyPortTextBox.Text.Trim(), out int httpport) && httpport >= 0 && httpport <= 65535)
                {
                    HttpProxyPort = httpport;
                    DialogResult = true;
                }
            }
            else
            {
                DialogResult = true;
            }
        }
        private void ProxyCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            RefreshVisibility();
        }
        private void RefreshVisibility()
        {
            // ReSharper disable once PossibleInvalidOperationException
            var hasProxy = ProxyCheckBox.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
            // ReSharper disable once PossibleInvalidOperationException
            var isSocks5 = ProxyCheckBox.IsChecked.Value && Socks5RadioButton.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
            // ReSharper disable once PossibleInvalidOperationException
            var hasAuth = ProxyCheckBox.IsChecked.Value && Socks5RadioButton.IsChecked.Value && /*ReSharper disable once PossibleInvalidOperationException*/ !AnonymousCheckBox.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
            var hasHttp = HttpProxyCheck != null && HttpProxyCheck.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;

            if (ProxyTypePanel != null)
            {
                ProxyTypePanel.Visibility = hasProxy;
            }
            if (ProxyHostLabel != null)
            {
                ProxyHostLabel.Visibility = hasProxy;
            }
            if (ProxyHostTextBox != null)
            {
                ProxyHostTextBox.Visibility = hasProxy;
            }
            if (ProxyPortLabel != null)
            {
                ProxyPortLabel.Visibility = hasProxy;
            }
            if (ProxyPortTextBox != null)
            {
                ProxyPortTextBox.Visibility = hasProxy;
            }
            if (AnonymousCheckBox != null)
            {
                AnonymousCheckBox.Visibility = isSocks5;
            }
            if (ProxyUsernameLabel != null)
            {
                ProxyUsernameLabel.Visibility = hasAuth;
            }
            if (ProxyPasswordLabel != null)
            {
                ProxyPasswordLabel.Visibility = hasAuth;
            }
            if (ProxyUsernameTextBox != null)
            {
                ProxyUsernameTextBox.Visibility = hasAuth;
            }
            if (ProxyPasswordTextBox != null)
            {
                ProxyPasswordTextBox.Visibility = hasAuth;
            }
            if (HttpProxyHostLabel != null)
                HttpProxyHostLabel.Visibility = hasHttp;
            if (HttpProxyHostLabel != null)
                HttpProxyHostTextBox.Visibility = hasHttp;
            if (HttpProxyPortLabel != null)
                HttpProxyPortLabel.Visibility = hasHttp;
            if (HttpProxyPortTextBox != null)
                HttpProxyPortTextBox.Visibility = hasHttp;
        }
        private void ShowAccounts_Click(object sender, RoutedEventArgs e)
        {
            ShowAccounts = !ShowAccounts;
            if (ShowAccounts)
            {
                AccountList.Visibility = Visibility.Visible;
                AccountList.Width = 150;
                ArrowIcon.Kind = PackIconKind.ArrowLeft;
            }
            else
            {
                AccountList.Width = 0;
                AccountList.Visibility = Visibility.Hidden;
                ArrowIcon.Kind = PackIconKind.ArrowRight;
            }
        }
        private void AccountListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AccountListView.SelectedItem == null)
            {
                return;
            }
            string accountName = AccountListView.SelectedItem.ToString();
            lock (_bot)
            {
                if (_bot.AccountManager.Accounts.ContainsKey(accountName))
                {
                    Account account = _bot.AccountManager.Accounts[accountName];
                    UsernameTextBox.Text = account.Name;
                    if (account.Password == null)
                    {
                        PasswordTextBox.Clear();
                    }
                    else
                    {
                        PasswordTextBox.Password = account.Password;
                    }
                    if (account.Socks.Version != SocksVersion.None || account.Socks.Username != null || account.Socks.Password != null
                        || account.Socks.Host != null || account.Socks.Port != -1)
                    {
                        ProxyCheckBox.IsChecked = true;
                    }
                    else
                    {
                        ProxyCheckBox.IsChecked = false;
                    }
                    if (account.Socks.Version == SocksVersion.Socks4)
                    {
                        ProxyVersion = 4;
                    }
                    else if (account.Socks.Version == SocksVersion.Socks5)
                    {
                        ProxyVersion = 5;
                    }
                    if (account.Socks.Host == null)
                    {
                        ProxyHostTextBox.Clear();
                    }
                    else
                    {
                        ProxyHostTextBox.Text = account.Socks.Host;
                    }
                    if (account.Socks.Port == -1)
                    {
                        ProxyPortTextBox.Clear();
                    }
                    else
                    {
                        ProxyPortTextBox.Text = account.Socks.Port.ToString();
                    }
                    if (account.Socks.Username != null || account.Socks.Password != null)
                    {
                        AnonymousCheckBox.IsChecked = false;
                    }
                    else
                    {
                        AnonymousCheckBox.IsChecked = true;
                    }
                    if (account.Socks.Username == null)
                    {
                        ProxyUsernameTextBox.Clear();
                    }
                    else
                    {
                        ProxyUsernameTextBox.Text = account.Socks.Username;
                    }
                    if (account.Socks.Password == null)
                    {
                        ProxyPasswordTextBox.Clear();
                    }
                    else
                    {
                        ProxyPasswordTextBox.Password = account.Socks.Password;
                    }

                    if (!string.IsNullOrEmpty(account.HttpProxy.Host))
                        HttpProxyHostTextBox.Text = account.HttpProxy.Host;
                    else
                        HttpProxyHostTextBox.Clear();

                    if (account.HttpProxy.Port == -1)
                    {
                        HttpProxyPortTextBox.Clear();
                    }
                    else
                    {
                        HttpProxyPortTextBox.Text = account.HttpProxy.Port.ToString();
                    }
                }
            }
        }
        private void SaveAccountButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ShowAccounts)
            {
                ShowAccounts_Click(null, null);
            }
            if (string.IsNullOrEmpty(UsernameTextBox.Text))
            {
                return;
            }
            var account = new Account(UsernameTextBox.Text.Trim());

            if (!string.IsNullOrEmpty(PasswordTextBox.Password))
            {
                account.Password = PasswordTextBox.Password;
            }

            if (HasProxy)
            {
                var socksVersion = SocksVersion.None;
                if (ProxyVersion == 4)
                {
                    socksVersion = SocksVersion.Socks4;
                }
                else if (ProxyVersion == 5)
                {
                    socksVersion = SocksVersion.Socks5;
                }
                account.Socks.Version = socksVersion;
                if (!string.IsNullOrEmpty(ProxyHostTextBox.Text))
                {
                    account.Socks.Host = ProxyHostTextBox.Text.Trim();
                }
                if (!string.IsNullOrEmpty(ProxyPortTextBox.Text))
                {
                    if (int.TryParse(ProxyPortTextBox.Text.Trim(), out int port))
                    {
                        account.Socks.Port = port;
                    }
                }
                if (!string.IsNullOrEmpty(ProxyUsernameTextBox.Text))
                {
                    account.Socks.Username = ProxyUsernameTextBox.Text.Trim();
                }
                if (!string.IsNullOrEmpty(ProxyPasswordTextBox.Password))
                {
                    account.Socks.Password = ProxyPasswordTextBox.Password;
                }

                if (HasHttpProxy)
                {
                    if (!string.IsNullOrEmpty(HttpProxyHostTextBox.Text))
                    {
                        account.HttpProxy.Host = HttpProxyHostTextBox.Text.Trim();
                    }
                    if (!string.IsNullOrEmpty(HttpProxyPortTextBox.Text))
                    {
                        if (int.TryParse(HttpProxyPortTextBox.Text.Trim(), out int httpport))
                        {
                            account.HttpProxy.Port = httpport;
                        }
                    }
                }
            }
            lock (_bot)
            {
                _bot.AccountManager.Accounts[account.Name] = account;
                _bot.AccountManager.SaveAccount(account.Name);
            }
            RefreshAccountList();
        }

        private void DeleteAccountButton_Click(object sender, RoutedEventArgs e)
        {
            if (AccountListView.SelectedItem == null)
            {
                return;
            }
            string name = AccountListView.SelectedItem.ToString();
            lock (_bot)
            {
                if (_bot.AccountManager.Accounts.ContainsKey(name))
                {
                    _bot.AccountManager.DeleteAccount(name);
                    RefreshAccountList();
                }
            }
        }
    }
}
