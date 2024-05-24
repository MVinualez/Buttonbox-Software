using Microsoft.Win32;
using System;
using System.Windows;
using System.IO.Ports;
using System.Management;
using System.Windows.Controls;
using System.IO;

namespace MyApp {
    public partial class MainWindow : Window {
        private readonly DBConnection db;
        private ManagementEventWatcher creationWatcher;
        private ManagementEventWatcher deletionWatcher;
        private Compiler compiler;

        public MainWindow() {
            InitializeComponent();

            db = new DBConnection();
            compiler = new Compiler();

            compiler.InitializeUI(progressBar, txt_console);
            compiler.InitializeArduinoCli();
            compiler.ResetUI();

            updatePortsList();
            StartDeviceWatcher();
        }

        private void updatePortsList() {
            combobox_ports_list.Items.Clear();
            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports) {
                // Recherche des informations du périphérique associé à ce port COM
                ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%{port}%'");
                string deviceName = "";
                foreach (ManagementObject device in searcher.Get()) {
                    // Récupérer le nom du périphérique
                    deviceName = device["Name"].ToString();
                    // Retirer la mention "COM..." du nom du périphérique
                    int comIndex = deviceName.LastIndexOf("(COM");
                    if (comIndex != -1) {
                        deviceName = deviceName.Substring(0, comIndex).Trim();
                    }
                    break; // Sortir après avoir trouvé le premier périphérique
                }

                // Création de l'élément ComboBoxItem pour le port COM
                ComboBoxItem portItem = new ComboBoxItem {
                    Content = $"{port} - {deviceName}"
                };

                // Ajout du port COM à la liste déroulante
                combobox_ports_list.Items.Add(portItem);
            }
        }

        private void StartDeviceWatcher() {
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

        private void Window_Closed(object sender, EventArgs e) {
            // Arrêter la surveillance des événements lorsque la fenêtre est fermée
            creationWatcher?.Stop();
            creationWatcher?.Dispose();

            deletionWatcher?.Stop();
            deletionWatcher?.Dispose();
        }

        private void btn_refresh_Click(object sender, RoutedEventArgs e) {
            updatePortsList();
        }

        private void btn_upload_Click(object sender, RoutedEventArgs e) {
            string sketchPath = txt_sketch_path.Text;
            ComboBoxItem selectedPortItem = (ComboBoxItem)combobox_ports_list.SelectedItem;
            string selectedPort = selectedPortItem?.Content.ToString().Split('-')[0].Trim();

            if (string.IsNullOrEmpty(selectedPort)) {
                MessageBox.Show("Aucun port sélectionné", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string fqbn = compiler.DetectBoard(selectedPort);
            if (string.IsNullOrEmpty(fqbn)) {
                MessageBox.Show("Impossible de détecter le type de carte. Assurez-vous qu'une carte est connectée et réessayez.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!string.IsNullOrEmpty(sketchPath) && File.Exists(sketchPath)) {
                string sketchDirectory = Path.GetDirectoryName(sketchPath);
                string sketchName = Path.GetFileNameWithoutExtension(sketchPath);

                // Vérifie si le fichier est déjà dans un dossier portant le même nom
                if (Path.GetFileName(sketchDirectory) != sketchName) {
                    string newDirectory = Path.Combine(Path.GetDirectoryName(sketchDirectory), sketchName);
                    Directory.CreateDirectory(newDirectory);
                    string newSketchPath = Path.Combine(newDirectory, Path.GetFileName(sketchPath));
                    File.Copy(sketchPath, newSketchPath, true);
                    sketchPath = newSketchPath;
                }

                compiler.CompileAndUploadSketch(sketchPath, fqbn, selectedPort);
            } else {
                MessageBox.Show($"Sketch introuvable à l'emplacement : {sketchPath}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btn_select_file_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = "Sketch Files (*.ino)|*.ino|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true) {
                txt_sketch_path.Text = openFileDialog.FileName;
            }
        }
    }
}
