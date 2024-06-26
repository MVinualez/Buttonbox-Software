﻿using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace MyApp {
    public class Compiler {
        private ProgressBar progressBar;
        private TextBox consoleTextBox;

        // Méthode pour initialiser la barre de progression et la console
        public void InitializeUI(ProgressBar progressBar, TextBox consoleTextBox) {
            this.progressBar = progressBar;
            this.consoleTextBox = consoleTextBox;
        }

        public void ResetUI() {
            progressBar.Value = 0;
            consoleTextBox.Clear();
        }

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
            RunArduinoCliCommand("core install esp32:esp32");
        }

        private string GetArduinoCliPath() {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string arduinoCliPath = Path.Combine(basePath, "..", "..", "src", "arduino-cli.exe");
            arduinoCliPath = Path.GetFullPath(arduinoCliPath);
            return arduinoCliPath;
        }

        public bool RunArduinoCliCommand(string arguments) {
            try {
                string arduinoCliPath = GetArduinoCliPath();
                if (!File.Exists(arduinoCliPath)) {
                    MessageBox.Show($"arduino-cli.exe introuvable à l'emplacement : {arduinoCliPath}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
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

                AppendToConsole($"Commande exécutée : {arduinoCliPath} {arguments}");
                AppendToConsole($"Sortie : {output}");
                if (error.Length > 0) {
                    AppendToConsole($"Erreur : {error}");
                }

                // Mise à jour de la barre de progression
                progressBar.Value = process.ExitCode == 0 ? 100 : 0;

                if (process.ExitCode != 0) {
                    MessageBox.Show($"Erreur lors de l'exécution de la commande '{arguments}': {error}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                return true; // Ajout de cette ligne pour s'assurer que true est retourné en cas de succès
            }
            catch (Exception ex) {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }


        public void CompileAndUploadSketch(string sketchPath, string fqbn, string port) {
            try {
                if (!File.Exists(sketchPath)) {
                    MessageBox.Show($"Sketch introuvable à l'emplacement : {sketchPath}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Créer un dossier temporaire pour la compilation
                string tempDir = Path.Combine(Path.GetTempPath(), "ArduinoBuild");
                if (!Directory.Exists(tempDir)) {
                    Directory.CreateDirectory(tempDir);
                }

                // Commande pour la compilation
                string compileArgs = $"compile --fqbn {fqbn} --build-path \"{tempDir}\" \"{sketchPath}\"";
                bool compileSuccess = RunArduinoCliCommand(compileArgs);

                if (!compileSuccess) {
                    MessageBox.Show("Échec de la compilation. Veuillez vérifier les erreurs et réessayer.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Commande pour le téléversement
                string uploadArgs = $"upload -p {port} --fqbn {fqbn} --input-dir \"{tempDir}\"";
                bool uploadSuccess = RunArduinoCliCommand(uploadArgs);

                if (uploadSuccess) {
                    MessageBox.Show("Téléversement réussi !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                } else {
                    MessageBox.Show("Échec du téléversement. Veuillez vérifier les erreurs et réessayer.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            } catch (Exception ex) {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string DetectBoard(string port) {
            string arduinoCliPath = GetArduinoCliPath();
            if (!File.Exists(arduinoCliPath)) {
                MessageBox.Show($"arduino-cli.exe introuvable à l'emplacement : {arduinoCliPath}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            string arguments = "board list";
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
            process.WaitForExit();

            // Analyser la sortie pour trouver le FQBN correspondant au port
            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines) {
                if (line.Contains(port)) {
                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string part in parts) {
                        if (part.Contains(":")) {
                            return part; // Retourner le FQBN
                        }
                    }
                }
            }

            return null; // Si aucune carte n'est détectée sur ce port
        }

        private void AppendToConsole(string text) {
            consoleTextBox.AppendText(text + Environment.NewLine);
        }
    }
}