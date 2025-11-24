using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Reflection;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using System.Net.Http;
using System.Globalization;
using System.Text;
using DeskFrame.Properties;
namespace DeskFrame
{
    public class Updater
    {
        private static string _url = "";
        private static string _downloadUrl = "";
        private static string tag_name = "";
        private static int updateCount = 0;
        public static async Task CheckUpdateAsync(string url, bool showToastIfNoUpdate)
        {
            _url = url;
            string currentVersion = Process.GetCurrentProcess().MainModule!.FileVersionInfo.FileVersion!.ToString();
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("DeskFrame", currentVersion));
                    var response = await httpClient.GetStringAsync(_url);
                    Debug.WriteLine("got response");
                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        var root = doc.RootElement;
                        string latestVersion = root.GetProperty("tag_name").GetString()!;
                        tag_name = latestVersion;
                        string description = root.GetProperty("body").GetString()!;
                        string published_at = root.GetProperty("published_at").GetString()!;
                        string name = root.GetProperty("name").GetString()!;
                        string downloadUrl = root.GetProperty("assets")[0].GetProperty("browser_download_url").GetString()!;
                        _downloadUrl = downloadUrl;
                        string emoji = (name.ToLower().Contains("fix"), name.ToLower().Contains("feature")) switch
                        {
                            (true, true) => "🚀",
                            (true, false) => "🪛", // (screwdriver)
                            (false, true) => "✨",
                            _ => "🚀"
                        };

                        if (!latestVersion.Contains(currentVersion))
                        {
                            var toastBuilder = new ToastContentBuilder()
                                 .AddText($"{emoji} New release! {name}", AdaptiveTextStyle.Header)
                                 .AddText(description, AdaptiveTextStyle.Body)
                                 .AddButton(new ToastButton()
                                     .SetContent("Install")
                                     .AddArgument("action", "install_update")
                                     .SetBackgroundActivation())
                                 .AddButton(new ToastButton()
                                     .SetContent("Close")
                                     .AddArgument("action", "close")
                                     .SetBackgroundActivation());
                            toastBuilder.Show();
                        }
                        else if (showToastIfNoUpdate)
                        {
                            var toastBuilder = new ToastContentBuilder()
                               .AddText("You are up to date!", AdaptiveTextStyle.Header)
                               .AddText("There is no available update.", AdaptiveTextStyle.Body);
                            toastBuilder.Show();
                        }
                    }
                }
                updateCount++;
            }
            catch (Exception e)
            {
                if (updateCount != 0)
                {
                    var toastBuilder = new ToastContentBuilder()
                               .AddText("Failed to update.", AdaptiveTextStyle.Header)
                               .AddText(e.Message, AdaptiveTextStyle.Body);
                    toastBuilder.Show();
                }
                Debug.WriteLine($"Update error: {e.Message}");
            }
        }
        public static async Task InstallUpdate()
        {
            string tag = "update";
            string group = "downloads";

            var toast = new ToastContentBuilder()
                .AddText($"Updating to {tag_name}", AdaptiveTextStyle.Header)
                .AddVisualChild(new AdaptiveProgressBar()
                {
                    Title = $"Progress",
                    Value = new BindableProgressBarValue("progressValue"),
                    ValueStringOverride = new BindableString("progressValueString"),
                    Status = new BindableString("progressStatus")
                })
                .GetToastContent();

            var notif = new ToastNotification(toast.GetXml())
            {
                Tag = tag,
                Group = group
            };
            ToastNotificationManagerCompat.CreateToastNotifier().Show(notif);

            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var canReportProgress = totalBytes != -1;

                string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Assembly.GetExecutingAssembly().GetName().Name}.exe");

                using (var inputStream = await response.Content.ReadAsStreamAsync())
                using (var outputStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var buffer = new byte[8192];
                    long totalRead = 0;
                    int read;
                    int lastProgress = -1;

                    while ((read = await inputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await outputStream.WriteAsync(buffer, 0, read);
                        totalRead += read;

                        if (canReportProgress)
                        {
                            int progress = (int)(((long)totalRead * 100) / totalBytes);
                            if (progress != lastProgress)
                            {
                                lastProgress = progress;

                                var data = new NotificationData
                                {
                                    SequenceNumber = (uint)progress,
                                    Values =
                                        {
                                            ["progressValue"] = (progress / 100.0).ToString("0.##", CultureInfo.InvariantCulture),
                                            ["progressValueString"] = $"{progress}%",
                                            ["progressStatus"] = "Downloading..."
                                        }
                                };

                                ToastNotificationManagerCompat.CreateToastNotifier().Update(data, tag, group);

                            }
                        }
                    }
                }
                ToastNotificationManagerCompat.CreateToastNotifier().Hide(notif);
                RestartApplication(tempFilePath);
            }
        }
        private static bool HasPermissionToWrite(string currentExecutablePath)
        {
            string path = Path.GetDirectoryName(currentExecutablePath)!;
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C cd /d \"{path}\" && echo. > DeskFrameUpdatePermissionCheck",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            Process proc = Process.Start(psi)!;
            proc.WaitForExit();
            if (proc.ExitCode == 0)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C cd /d \"{path}\" && del DeskFrameUpdatePermissionCheck",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                return true;
            }
            return false;
        }

        private static void ExecuteCommand(string command, bool needAdmin)
        {
            Debug.WriteLine("Starting CMD...");
            string tempCmd = "";
            if (needAdmin)
            {
                tempCmd = Path.Combine(Path.GetTempPath(), "deskframe_update.cmd");
                File.WriteAllText(Path.Combine(Path.GetTempPath(), "deskframe_update.cmd"), command, Encoding.UTF8);
            }
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = needAdmin ? tempCmd : "cmd.exe",
                Arguments = needAdmin ? null : $"/C {command}",
                UseShellExecute = needAdmin,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas"
            };
            Process.Start(psi);

        }

        private static void RestartApplication(string tempPath)
        {
            string currentExecutablePath = Process.GetCurrentProcess().MainModule!.FileName;
            string command = $"timeout /t 2 && move /y \"{tempPath}\" \"{currentExecutablePath}\" & \"{currentExecutablePath}\" && exit ";

            if (HasPermissionToWrite(currentExecutablePath))
            {
                ExecuteCommand(command, false);
            }
            else
            {
                var dialog = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "DeskFrame",
                    Content = Lang.Deskframe_Update_DialogContent,
                    PrimaryButtonText = "OK"
                };

                var result = dialog.ShowDialogAsync();

                if (result.Result == Wpf.Ui.Controls.MessageBoxResult.Primary)
                {
                    ExecuteCommand(command, true);
                }
                else
                {
                    return;
                }
            }
            Environment.Exit(0);
        }
    }
}
