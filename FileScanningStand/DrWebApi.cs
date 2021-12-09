using System;
using System.IO;
using System.Linq;
using Vestris.VMWareLib;

namespace FileScanningStand
{
    public class DrWebApi
    {
        private VMWareVirtualMachine VirtualMachine { get; }

        private readonly string exePath;

        private readonly string guestWorkingDirectory;

        private readonly string hostWorkingDirectory;

        private string ParseReport(string fileName)
        {
            var path = $@"{hostWorkingDirectory}\DWReports\{fileName}.DWReport.txt";

            var toWrite = File.ReadLines(path)
                .Where(line =>
                    line.Contains("clean") || line.Contains("infected") || line.Contains("detected") ||
                    line.Contains("file scanned")).ToList();

            File.Delete(path);

            var res = "";

            using (var sw = new StreamWriter(path))
                foreach (var line in toWrite)
                {
                    if (line.Contains(" clean") && !line.Contains("no clean"))
                        res = " - clean";

                    if (line.Contains(" infected with "))
                    {
                        var index = line.IndexOf(" infected with ", StringComparison.Ordinal);

                        res = " - infected with " +
                              line.Substring(index + 15, line.Length - index - 25);
                    }

                    sw.WriteLine(line);
                }

            return fileName + res;
        }

        public DrWebApi(VMWareVirtualHost virtualHost, string vmxPath, string login, string password,
            string exePath, string guestWorkingDirectory, string hostWorkingDirectory)
        {
            this.exePath = exePath;

            this.guestWorkingDirectory = guestWorkingDirectory;

            this.hostWorkingDirectory = hostWorkingDirectory;

            VirtualMachine = virtualHost.Open(vmxPath);

            if (!VirtualMachine.IsRunning)
                VirtualMachine.PowerOn(600);

            VirtualMachine.LoginInGuest(login, password, 600);
        }

        public string GetBasesDate(string basesXmlPath)
        {
            var file = $@"{hostWorkingDirectory}\DWBasesDate.txt";

            VirtualMachine.CopyFileFromGuestToHost(basesXmlPath, file);

            File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);

            var res = File.ReadAllText(file);

            File.Delete(file);

            res = res.Substring(res.IndexOf("timestamp value=", StringComparison.Ordinal) + 17, 8);

            res = res.Insert(6, ".");

            res = res.Insert(4, ".");

            return "\nDW BasesDate = " + res;
        }

        public string Scan(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentOutOfRangeException(nameof(path));

            var fileName = path.Substring(path.LastIndexOf(@"\", StringComparison.Ordinal) + 1);

            VirtualMachine.CopyFileFromHostToGuest($@"{path}", $@"{guestWorkingDirectory}\{fileName}");

            VirtualMachine.RunProgramInGuest(exePath,
                $@"/RP:""{guestWorkingDirectory}\{fileName}.DWReport.txt"" ""{guestWorkingDirectory}\{fileName}""");

            VirtualMachine.CopyFileFromGuestToHost($@"{guestWorkingDirectory}\{fileName}.DWReport.txt",
                $@"{hostWorkingDirectory}\DWReports\{fileName}.DWReport.txt");

            try
            {
                VirtualMachine.DeleteFileFromGuest($@"{guestWorkingDirectory}\{fileName}");

                VirtualMachine.DeleteFileFromGuest($@"{guestWorkingDirectory}\{fileName}.DWReport.txt");
            }
            catch
            {
                // ignored
            }

            return ParseReport(fileName);
        }

        public string Test(string infectedFile, string cleanFile) => "\nDW test results:\n" +
                                "\nMust be infected:\n" + Scan(hostWorkingDirectory + @"\" + infectedFile) + "\n" +
                                "\nMust be clean:\n" + Scan(hostWorkingDirectory + @"\" + cleanFile) + "\n";

        public void RevertToSnapshot()
        {
            try
            {
                VirtualMachine.Snapshots.GetNamedSnapshot("Snapshot 1").RevertToSnapshot(120);
            }
            catch
            {
                // ignored
            }
        }

        public void Exit()
        {
            VirtualMachine.ShutdownGuest();
        }
    }
}