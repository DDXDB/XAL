using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Drawing;
using System.Windows.Forms;

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

                        // 现代化UI - 使用更简洁的控件和样式
                        Label nameLabel = new()
                        {
                            AutoSize = false,
                            Width = 400,
                            Height = 40,
                            Text = displayName,
                            Font = new Font("Segoe UI", 10, FontStyle.Bold),
                            ForeColor = Color.FromArgb(32, 32, 32),
                            BackColor = Color.White,
                            Padding = new Padding(10, 0, 0, 0),
                            TextAlign = ContentAlignment.MiddleLeft
                        };

                        var currentDir = Path.Combine(subDir, "Content");
                        var logoPath = Path.Combine(currentDir, storeLogo);
                        PictureBox? pictureBoxA = null;
                        if (string.IsNullOrEmpty(storeLogo) is false && File.Exists(logoPath))
                        {
                            pictureBoxA = new PictureBox
                            {
                                SizeMode = PictureBoxSizeMode.Zoom,
                                Size = new Size(80, 80),
                                Image = Image.FromFile(logoPath),
                                Margin = new Padding(10, 5, 10, 5),
                                BackColor = Color.White
                            };
                        }

                        // 启动按钮 - Windows 11风格
                        Button startButton = new()
                        {
                            Text = "启动",
                            Height = 40,
                            Width = 100,
                            BackColor = Color.FromArgb(0, 120, 215),
                            ForeColor = Color.White,
                            Font = new Font("Segoe UI", 9),
                            FlatStyle = FlatStyle.Flat,
                            FlatAppearance = { BorderSize = 0 },
                            Margin = new Padding(5, 0, 5, 0)
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

                        // 设置按钮 - Windows 11风格
                        Button settingsButton = new Button();
                        settingsButton.Text = "设置";
                        settingsButton.Height = 40;
                        settingsButton.Width = 100;
                        settingsButton.BackColor = Color.FromArgb(243, 243, 243);
                        settingsButton.ForeColor = Color.FromArgb(32, 32, 32);
                        settingsButton.Font = new Font("Segoe UI", 9);
                        settingsButton.FlatStyle = FlatStyle.Flat;
                        settingsButton.FlatAppearance.BorderSize = 0;
                        settingsButton.Margin = new Padding(5, 0, 5, 0);

                        settingsButton.Click += (s, e) =>
                        {
                            Form settingsForm = new Form
                            {
                                Text = $"设置 {displayName}",
                                Width = 900,
                                Height = 450,
                                StartPosition = FormStartPosition.CenterScreen,
                                MaximizeBox = false,
                                BackColor = Color.White,
                                FormBorderStyle = FormBorderStyle.FixedSingle
                            };
                            TextBox textBox = new TextBox
                            {
                                Dock = DockStyle.Top,
                                Text = launchOptions.ContainsKey(subDir) ? launchOptions[subDir] : "",
                                Font = new Font("Segoe UI", 10),
                                Margin = new Padding(20, 20, 20, 0)
                            };
                            Button saveButton = new Button
                            {
                                Text = "保存",
                                Width = 100,
                                Height = 50,
                                Dock = DockStyle.Bottom,
                                BackColor = Color.FromArgb(0, 120, 215),
                                ForeColor = Color.White,
                                Font = new Font("Segoe UI", 10),
                                FlatStyle = FlatStyle.Flat,
                                FlatAppearance = { BorderSize = 0 }
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

                        // 创建卡片式面板
                        var panel = new FlowLayoutPanel
                        {
                            Dock = DockStyle.Top,
                            AutoSize = true,
                            BackColor = Color.White,
                            Margin = new Padding(10),
                            Padding = new Padding(0),
                            FlowDirection = FlowDirection.LeftToRight,
                            WrapContents = false
                        };

                        // 添加控件到面板中
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
