using System;
using System.Windows;
using System.IO.Ports;
using System.Management;
using System.Windows.Controls;


namespace MyApp
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window 
    {
        private readonly DBConnection db;
        private ManagementEventWatcher creationWatcher;
        private ManagementEventWatcher deletionWatcher;

        public MainWindow()
        {
            InitializeComponent();

            db = new DBConnection();

            updatePortsList();
            StartDeviceWatcher();
        }

        private void updatePortsList() {
            combobox_ports_list.Items.Clear();
            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports)
            {
                // Recherche des informations du périphérique associé à ce port COM
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%" + port + "%'");
                string deviceName = "";
                foreach (ManagementObject device in searcher.Get())
                {
                    // Récupérer le nom du périphérique
                    deviceName = device["Name"].ToString();
                    // Retirer la mention "COM..." du nom du périphérique
                    int comIndex = deviceName.LastIndexOf("(COM");
                    if (comIndex != -1)
                    {
                        deviceName = deviceName.Substring(0, comIndex).Trim();
                        // Vérifier si la partie restante est un nombre (pour COM10 et supérieur)
                        string[] splitName = deviceName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (splitName.Length > 0 && int.TryParse(splitName[splitName.Length - 1], out _))
                        {
                            // Si le dernier élément est un nombre, retirer également cette partie
                            deviceName = string.Join(" ", splitName, 0, splitName.Length - 1);
                        }
                    }
                    break; // Sortir après avoir trouvé le premier périphérique
                }

                // Création de l'élément ComboBoxItem pour le port COM
                ComboBoxItem portItem = new ComboBoxItem();
                portItem.Content = $"{port} - {deviceName}";

                // Ajout du port COM à la liste déroulante
                combobox_ports_list.Items.Add(portItem);
            }
        }

        private void StartDeviceWatcher()
        {
            // Création de requêtes WMI pour surveiller les évènements de branchements et débranchements de périphériques
            string creationQuery = "SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'";
            string deletionQuery = "SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'";

            // Créer les watchers pour surveiller les événements de création et de suppression
            creationWatcher = new ManagementEventWatcher(creationQuery);
            creationWatcher.EventArrived += (s, e) => {
                // Mettre à jour la liste des ports lorsqu'un événement de création est détecté
                Dispatcher.Invoke(new Action(updatePortsList));
            };

            deletionWatcher = new ManagementEventWatcher(deletionQuery);
            deletionWatcher.EventArrived += (s, e) => {
                // Mettre à jour la liste des ports lorsqu'un événement de suppression est détecté
                Dispatcher.Invoke(new Action(updatePortsList));
            };

            // Démarrer la surveillance des deux types d'événements
            creationWatcher.Start();
            deletionWatcher.Start();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // Arrêter la surveillance des événements lorsque la fenêtre est fermée
            if (creationWatcher != null)
            {
                creationWatcher.Stop();
                creationWatcher.Dispose();
            }

            if (deletionWatcher != null)
            {
                deletionWatcher.Stop();
                deletionWatcher.Dispose();
            }
        }

        private void btn_compile_Click(object sender, RoutedEventArgs e) {
            updatePortsList();
        }
    }
}
