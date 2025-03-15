using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Windows;
using Microsoft.Win32;
using System.Xml.Linq;
using System.Windows.Controls;
using System.Collections.Generic;


namespace GaskaApiServiceConfiguration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _serviceName = "GaskaApiService";
        private readonly string _serviceExePath = Path.Combine(Directory.GetCurrentDirectory(), "GaskaApiService.exe");
        private readonly string _configPath = Path.Combine(Directory.GetCurrentDirectory(), "GaskaApiService.exe") + ".config";
        private readonly string _serviceDescription = "Serwis do aktualizowania kart towarowych w bazie danych poprzez Gąska API";
        private Dictionary<string, Control> _settings;
        private XDocument _config;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(_serviceExePath))
            {
                MessageBox.Show($"Nie znaleziono serwisu {_serviceName} w katalogu {_serviceExePath}.{Environment.NewLine}Zamykam aplikację.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            try
            {
                _config = XDocument.Load(_configPath);

                _settings = new Dictionary<string, Control>
                {
                    { "LogsExpirationDays", LogsExpirationDaysTextBox },
                    { "ProductsResponsePerRequest", ProductsPerRequestTextBox },
                    { "StartProductsFetchHour", StartHourTextBox },
                    { "EndProductsFetchHour", EndHourTextBox },

                    { "DbUsername", DatabaseUsernameTextBox },
                    { "DbPassword", DatabasePasswordBox },
                    { "DbName", DatabaseNameTextBox },
                    { "DbTableName", DatabaseTableNameTextBox },
                    { "DbIp", DatabaseIpTextBox },

                    { "ApiAcronym", ApiUsernameTextBox },
                    { "ApiPerson", ApiPersonTextBox },
                    { "ApiPassword", ApiPasswordBox },
                    { "ApiKey", ApiKeyBox },
                    { "ApiBaseUrl", ApiUrlTextBox },
                };

                foreach (var entry in _settings)
                {
                    SetConfigValue(entry.Key, entry.Value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas wczytywania konfiguracji: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            UpdateServiceStatus();
        }

        private void SetConfigValue(string key, Control control)
        {
            XElement element = _config.Descendants("appSettings")
                .Elements("add")
                .FirstOrDefault(x => x.Attribute("key")?.Value == key);

            if (element != null)
            {
                string value = element.Attribute("value")?.Value;
                if (control is TextBox textBox)
                {
                    textBox.Text = value;
                }
                else if (control is PasswordBox passwordBox)
                {
                    passwordBox.Password = value;
                }
            }
            else
            {
                MessageBox.Show($"Nie znaleziono konfiguracji '{key}' w pliku konfiguracyjnym", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void UpdateConfigValue(string key, Control control)
        {
            XElement element = _config.Descendants("appSettings")
                .Elements("add")
                .FirstOrDefault(x => x.Attribute("key")?.Value == key);

            if (element != null)
            {
                string newValue = control is TextBox textBox ? textBox.Text
                               : control is PasswordBox passwordBox ? passwordBox.Password
                               : string.Empty;

                element.SetAttributeValue("value", newValue);
            }
            else
            {
                MessageBox.Show($"Nie znaleziono konfiguracji '{key}' w pliku konfiguracyjnym", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void InstallService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ServiceController.GetServices().Any(s => s.ServiceName == _serviceName))
                {
                    MessageBox.Show("Serwis już istnieje!", "Informacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ProcessStartInfo createProcessStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C sc create {_serviceName} binPath= \"{_serviceExePath}\" start= auto",
                    Verb = "runas",
                    CreateNoWindow = true,
                    UseShellExecute = true,
                };

                ProcessStartInfo descriptionProcessStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C sc description {_serviceName} \"{_serviceDescription}\"",
                    Verb = "runas",
                    CreateNoWindow = true,
                    UseShellExecute = true,
                };

                Process.Start(createProcessStartInfo);
                Process.Start(descriptionProcessStartInfo);

                MessageBox.Show("Zainstalowano serwis.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wystąpił błąd podczas instalacji serwisu:{Environment.NewLine}{ex}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UpdateServiceStatus();
            }
        }


        private void UninstallService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (ServiceController sc = new ServiceController(_serviceName))
                {
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                }
            }
            catch
            {

            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C sc delete {_serviceName}",
                    Verb = "runas",
                    CreateNoWindow = true,
                    UseShellExecute = true
                });

                MessageBox.Show("Serwis został odinstalowany.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wystąpił błąd podczas odinstalowywania serwisu:{Environment.NewLine}{ex}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UpdateServiceStatus();
            }
        }

        private void StartService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (ServiceController sc = new ServiceController(_serviceName))
                {
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running);
                    }
                }
                MessageBox.Show("Włączono serwis.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wystąpił błąd podczas włączania serwisu:{Environment.NewLine}{ex}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UpdateServiceStatus();
            }
        }

        private void StopService_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (ServiceController sc = new ServiceController(_serviceName))
                {
                    if (sc.Status != ServiceControllerStatus.Stopped)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped);
                    }
                }
                MessageBox.Show("Zatrzymano serwis.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wystąpił błąd podczas zatrzymywania serwisu.{Environment.NewLine}{ex}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UpdateServiceStatus();
            }
        }

        private void EditConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var entry in _settings)
                {
                    UpdateConfigValue(entry.Key, entry.Value);
                }
                _config.Save(_configPath);
                MessageBox.Show("Zaktualizowano konfigurację", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wystąpił błąd podczas edytowania parametrów serwisu.{Environment.NewLine}{ex}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateServiceStatus()
        {
            if (string.IsNullOrEmpty(_serviceName))
            {
                InstallServiceButton.IsEnabled = false;
                UninstallServiceButton.IsEnabled = false;
                StartServiceButton.IsEnabled = false;
                StopServiceButton.IsEnabled = false;
                return;
            }

            try
            {
                using (ServiceController sc = new ServiceController(_serviceName))
                {
                    switch (sc.Status)
                    {
                        case ServiceControllerStatus.Running:
                            StartServiceButton.IsEnabled = false;
                            StopServiceButton.IsEnabled = true;
                            InstallServiceButton.IsEnabled = false;
                            UninstallServiceButton.IsEnabled = false;
                            break;

                        case ServiceControllerStatus.Stopped:
                            StartServiceButton.IsEnabled = true;
                            StopServiceButton.IsEnabled = false;
                            InstallServiceButton.IsEnabled = false;
                            UninstallServiceButton.IsEnabled = true;
                            break;

                        default:
                            StartServiceButton.IsEnabled = false;
                            StopServiceButton.IsEnabled = false;
                            InstallServiceButton.IsEnabled = false;
                            UninstallServiceButton.IsEnabled = false;
                            break;
                    }
                }
            }
            catch
            {
                InstallServiceButton.IsEnabled = true;
                UninstallServiceButton.IsEnabled = false;
                StartServiceButton.IsEnabled = false;
                StopServiceButton.IsEnabled = false;
            }
        }

        private void ProductsPerRequestTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!String.IsNullOrEmpty(ProductsPerRequestTextBox.Text))
            {
                if (!int.TryParse(ProductsPerRequestTextBox.Text, out int value))
                {
                    MessageBox.Show("Wpisz liczbę", "Informacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ProductsPerRequestTextBox.Text = "";
                    return;
                }

                if (value > 1000)
                {
                    MessageBox.Show("Można pobierać maksymalnie 1000 produktów na 1 request!", "Informacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ProductsPerRequestTextBox.Text = "1000";
                }
            }
        }
    }
}
