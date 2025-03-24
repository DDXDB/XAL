using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace XAL
{
    public partial class Form1 : Form
    {
        private List<string> directories = new List<string>();
        private Dictionary<string, string> launchOptions = new Dictionary<string, string>();

        public Form1()
        {
            InitializeComponent();
            LoadSettings();
            UpdateUI();
        }

        private void LoadSettings()
        {
            if (File.Exists("settings.xml"))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load("settings.xml");
                XmlNodeList dirNodes = doc.SelectNodes("/Settings/Directories/Directory");
                foreach (XmlNode node in dirNodes)
                {
                    directories.Add(node.InnerText);
                }

                XmlNodeList optionNodes = doc.SelectNodes("/Settings/LaunchOptions/Option");
                foreach (XmlNode node in optionNodes)
                {
                    launchOptions[node.Attributes["Folder"].Value] = node.InnerText;
                }
            }
        }

        private void SaveSettings()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Settings");
            doc.AppendChild(root);

            XmlElement dirsNode = doc.CreateElement("Directories");
            root.AppendChild(dirsNode);
            foreach (string dir in directories)
            {
                XmlElement dirNode = doc.CreateElement("Directory");
                dirNode.InnerText = dir;
                dirsNode.AppendChild(dirNode);
            }

            XmlElement optionsNode = doc.CreateElement("LaunchOptions");
            root.AppendChild(optionsNode);
            foreach (var option in launchOptions)
            {
                XmlElement optNode = doc.CreateElement("Option");
                optNode.SetAttribute("Folder", option.Key);
                optNode.InnerText = option.Value;
                optionsNode.AppendChild(optNode);
            }

            doc.Save("settings.xml");
        }

        private void UpdateUI()
        {
            flowLayoutPanel1.Controls.Clear();
            foreach (string dir in directories)
            {
                string[] subDirs = Directory.GetDirectories(dir);
                foreach (string subDir in subDirs)
                {

                    try
                    {


                        XmlDocument doc = new XmlDocument();
                        doc.Load(Path.Combine(subDir, "Content", "appxmanifest.xml"));
                        XmlNode displayNameNode = doc.SelectSingleNode("/Package/Applications/Application/DisplayName");
                        string nameA = displayNameNode?.InnerText ?? Path.GetFileName(subDir);

                        Label nameLabel = new Label();
                        nameLabel.AutoSize = true;
                        nameLabel.Text = nameA;
                        doc.Load(Path.Combine(subDir, "Content", "appxmanifest.xml"));
                        XmlNode logoNode = doc.SelectSingleNode("/Package/Applications/Application/Logo");
                        string logoA = logoNode?.InnerText ?? Path.GetFileName(subDir);
                        PictureBox pictureBoxA = new PictureBox();
                        pictureBoxA.Size = new System.Drawing.Size(100, 100);
                        pictureBoxA.SizeMode = PictureBoxSizeMode.Zoom;
                        logoA = logoA.Replace("\\", "/");
                        string logoPath = Path.Combine(subDir, "Content", logoA);
                        if (File.Exists(logoPath))
                        {
                            pictureBoxA.Image = Image.FromFile(logoPath);
                        }
                        Button startButton = new Button();
                        startButton.Text = "启动";
                        startButton.Click += (s, e) =>
                        {
                            string launchPath = Path.Combine(subDir, "Content", "gamelaunchhelper.exe");
                            string arguments = launchOptions.ContainsKey(subDir) ? launchOptions[subDir] : "";
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = launchPath,
                                Arguments = arguments,
                                UseShellExecute = true
                            });
                        };

                        Button settingsButton = new Button();
                        settingsButton.Text = "设置";
                        settingsButton.Click += (s, e) =>
                        {
                            Form settingsForm = new Form
                            {
                                Text = $"设置 {nameA}",
                                Width = 300,
                                Height = 150
                            };
                            TextBox textBox = new TextBox
                            {
                                Dock = DockStyle.Top,
                                Text = launchOptions.ContainsKey(subDir) ? launchOptions[subDir] : ""
                            };
                            Button saveButton = new Button
                            {
                                Text = "保存",
                                Dock = DockStyle.Bottom
                            };
                            saveButton.Click += (ss, ee) =>
                            {
                                launchOptions[subDir] = textBox.Text;
                                SaveSettings();
                                settingsForm.Close();
                            };

                            settingsForm.Controls.Add(textBox);
                            settingsForm.Controls.Add(saveButton);

                            settingsForm.ShowDialog();
                        };

                        FlowLayoutPanel panel = new FlowLayoutPanel
                        {
                            Dock = DockStyle.Top,
                            AutoSize = true,
                            BackColor = System.Drawing.Color.LightGray,
                            Margin = new Padding(5)
                        };
                        panel.Controls.Add(pictureBoxA);
                        panel.Controls.Add(nameLabel);
                        panel.Controls.Add(startButton);
                        panel.Controls.Add(settingsButton);

                        flowLayoutPanel1.Controls.Add(panel);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"无法加载 {subDir}: {ex.Message}");
                    }
                }
            }
        }

        private void btnAddDirectory_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = fbd.SelectedPath;
                    if (!directories.Contains(selectedPath))
                    {
                        directories.Add(selectedPath);
                        SaveSettings();
                        UpdateUI();
                    }
                }
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }

}