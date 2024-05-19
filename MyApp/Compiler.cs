using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace MyApp
{
    public class Compiler
    {
        public void InitializeArduinoCli() {
            string arduinoCliPath = GetArduinoCliPath();
            string configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArduinoCli");
            if (!Directory.Exists(configDirectory)) {
                Directory.CreateDirectory(configDirectory);
            }

            string arduinoCliConfigFile = Path.Combine(configDirectory, "arduino-cli.yaml");
            if (!File.Exists(arduinoCliConfigFile)) {
                // Configuration initiale
                RunArduinoCliCommand("config init --additional-urls https://downloads.arduino.cc/packages/package_index.json");

                // Sauvegarder le fichier de configuration
                File.WriteAllText(arduinoCliConfigFile, ""); 
            } else {
                RunArduinoCliCommand("config init --overwrite --additional-urls https://downloads.arduino.cc/packages/package_index.json");
            }

            // Vérification de la mise à jour des cores
            RunArduinoCliCommand("core update-index");
            RunArduinoCliCommand("core install arduino:avr");
        }

        private string GetArduinoCliPath() {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string arduinoCliPath = Path.Combine(basePath, "..", "..", "src", "arduino-cli.exe");
            arduinoCliPath = Path.GetFullPath(arduinoCliPath);
            return arduinoCliPath;
        }

        public void RunArduinoCliCommand(string arguments) {
            try {
                string arduinoCliPath = GetArduinoCliPath();
                if (!File.Exists(arduinoCliPath)) {
                    MessageBox.Show($"arduino-cli.exe introuvable à l'emplacement : {arduinoCliPath}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Process process = new Process {
                    StartInfo = new ProcessStartInfo {
                        FileName = arduinoCliPath,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                Console.WriteLine($"Commande exécutée : {arduinoCliPath} {arguments}");
                Console.WriteLine($"Sortie : {output}");
                if(error != "") {
                    Console.WriteLine($"Erreur : {error}");

                }

                if (process.ExitCode != 0) {
                    // Afficher uniquement en cas d'erreur
                    MessageBox.Show($"Erreur lors de l'exécution de la commande '{arguments}': {error}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex) {
                // Afficher l'exception dans la console
                Console.WriteLine($"Erreur : {ex.Message}");
                // Afficher une boîte de dialogue avec l'exception
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CompileAndUploadSketch(string sketchPath, string fqbn, string port) {
            try {
                if (!File.Exists(sketchPath)) {
                    MessageBox.Show($"Sketch introuvable à l'emplacement : {sketchPath}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Commande pour la compilation
                string compileArgs = $"compile --fqbn {fqbn} {sketchPath}";
                RunArduinoCliCommand(compileArgs);

                // Commande pour le téléversement
                string uploadArgs = $"upload -p {port} --fqbn {fqbn} {sketchPath}";
                RunArduinoCliCommand(uploadArgs);

                MessageBox.Show("Téléversement réussi !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
