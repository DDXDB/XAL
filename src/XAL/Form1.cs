using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace XAL
{
    public partial class Form1 : Form
    {
        private List<string> directories = new();
        private Dictionary<string, string> launchOptions = new();

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
                XDocument doc = XDocument.Load("settings.xml");
                foreach (var node in doc.XPathSelectElements("/Settings/Directories/Directory"))
                {
                    directories.Add(node.Value);
                }

                foreach (var node in doc.XPathSelectElements("/Settings/LaunchOptions/Option"))
                {
                    launchOptions[node.Attribute("Folder").Value] = node.Value;
                }
            }
        }

        private void SaveSettings()
        {
            var doc = new XmlDocument();
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
            foreach (var dir in directories)
            {
                var subDirs = Directory.GetDirectories(dir);
                foreach (var subDir in subDirs)
                {
                    try
                    {
                        string? exeName = "", storeLogo = "", displayName = "";
                        var gameConfigPath = Path.Combine(subDir, "Content", "MicrosoftGame.config");
                        if (File.Exists(gameConfigPath))
                        {
                            var doc = XDocument.Load(Path.Combine(subDir, "Content", "MicrosoftGame.config"));
                            exeName = doc.XPathSelectElement("/Game/ExecutableList/Executable")?.Attribute("Name")?.Value;
                            storeLogo = doc.XPathSelectElement("/Game/ShellVisuals")?.Attribute("StoreLogo")?.Value;
                            displayName = doc.XPathSelectElement("/Game/ShellVisuals")?.Attribute("DefaultDisplayName")?.Value;
                        }

                        var manifestPath = Path.Combine(subDir, "Content", "appxmanifest.xml");
                        if ((string.IsNullOrEmpty(exeName) || string.IsNullOrEmpty(storeLogo)) && File.Exists(manifestPath))
                        {

                            var doc = XDocument.Load(manifestPath);
                            var namespaceManager = new XmlNamespaceManager(new NameTable());
                            namespaceManager.AddNamespace("ns", "http://schemas.microsoft.com/appx/manifest/foundation/windows10");
                            displayName = doc.XPathSelectElement("/ns:Package/ns:Properties/ns:DisplayName", namespaceManager)?.Value;
                            exeName = doc.XPathSelectElement("/ns:Package/ns:Applications/ns:Application", namespaceManager)?.Attribute("Executable")?.Value;
                            storeLogo = doc.XPathSelectElement("/ns:Package/ns:Properties/ns:Logo", namespaceManager)?.Value;
                        }

                        if (string.IsNullOrEmpty(exeName) || string.IsNullOrEmpty(displayName)) continue;
                        Label nameLabel = new()
                        {
                            AutoSize = false,
                            Width = 400,
                            Height = 30,
                            Text = displayName,
                            //BorderStyle = BorderStyle.FixedSingle,

                        };

                        var currentDir = Path.Combine(subDir, "Content");
                        var logoPath = Path.Combine(currentDir, storeLogo);
                        PictureBox? pictureBoxA = null;
                        if (string.IsNullOrEmpty(storeLogo) is false && File.Exists(logoPath))
                        {
                            pictureBoxA = new PictureBox
                            {
                                SizeMode = PictureBoxSizeMode.Zoom,
                                Size = new Size(100, 100),
                                Image = Image.FromFile(logoPath),
                            };
                        }
                        Button startButton = new()
                        {
                            Text = "启动",
                            Height = 40,
                            Dock = DockStyle.Bottom
                        };
                        var exePath = Path.Combine(currentDir, "gamelaunchhelper.exe");
                        if (File.Exists(exePath) is false) continue;
                        startButton.Click += (s, e) =>
                        {
                            string arguments = launchOptions.ContainsKey(subDir) ? launchOptions[subDir] : "";
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = exePath,
                                Arguments = arguments,
                                UseShellExecute = true
                            });
                        };

                        Button settingsButton = new Button();
                        settingsButton.Text = "设置";
                        settingsButton.Dock = DockStyle.Bottom;
                        settingsButton.Height = 40;
                        settingsButton.Click += (s, e) =>
                        {
                            Form settingsForm = new Form
                            {
                                Text = $"设置 {displayName}",
                                Width = 900,
                                Height = 450,
                                StartPosition = FormStartPosition.CenterScreen,
                                MaximizeBox = false

                            };
                            TextBox textBox = new TextBox
                            {
                                Dock = DockStyle.Top,
                                Text = launchOptions.ContainsKey(subDir) ? launchOptions[subDir] : ""
                            };
                            Button saveButton = new Button
                            {
                                Text = "保存",
                                Width = 100,
                                Height = 50,
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

                        var panel = new FlowLayoutPanel
                        {
                            Dock = DockStyle.Top,
                            AutoSize = true,
                            BackColor = Color.LightGray,
                            Margin = new Padding(5)
                        };
                        if (pictureBoxA is not null) panel.Controls.Add(pictureBoxA);
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
            using var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() != DialogResult.OK) return;
            var selectedPath = fbd.SelectedPath;
            if (directories.Contains(selectedPath)) return;
            directories.Add(selectedPath);
            SaveSettings();
            UpdateUI();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_Load_1(object sender, EventArgs e)
        {

        }
    }

}