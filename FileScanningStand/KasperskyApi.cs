using System;
using System.IO;
using System.Linq;
using Vestris.VMWareLib;

namespace FileScanningStand
{
    public class KasperskyApi
    {
        private VMWareVirtualMachine VirtualMachine { get; }

        private readonly string exePath;

        private readonly string guestWorkingDirectory;

        private readonly string hostWorkingDirectory;

        private string ParseReport(string fileName)
        {
            var path = $@"{hostWorkingDirectory}\KAVReports\{fileName}.KAVReport.txt";

            var toWrite = File.ReadLines(path)
                .Where(line => line.Contains("ok") || line.Contains("OK") || line.Contains("detected")).ToList();

            File.Delete(path);

            var res = "";

            using (var sw = new StreamWriter(path, true))
                foreach (var line in toWrite)
                {
                    if (line.Contains("\tok"))
                        res = " - clean";

                    if (line.Contains("\tdetected\t"))
                        res = " - infected with " +
                              line.Substring(line.IndexOf("\tdetected\t", StringComparison.Ordinal) + 10);

                    sw.WriteLine(line);
                }

            return fileName + res;
        }

        public KasperskyApi(VMWareVirtualHost virtualHost, string vmxPath, string login, string password,
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
            var file = $@"{hostWorkingDirectory}\KAVBasesDate.txt";

            VirtualMachine.CopyFileFromGuestToHost(basesXmlPath, file);

            File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);

            var res = File.ReadAllText(file);

            File.Delete(file);

            res = res.Substring(res.IndexOf("Date=", StringComparison.Ordinal) + 6, 8);

            res = res.Insert(4, ".");

            res = res.Insert(2, ".");

            return "\nKAV BasesDate = " + res;
        }

        public string Scan(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentOutOfRangeException(nameof(path));

            var fileName = path.Substring(path.LastIndexOf(@"\", StringComparison.Ordinal) + 1);

            VirtualMachine.CopyFileFromHostToGuest($@"{path}", $@"{guestWorkingDirectory}\{fileName}");

            VirtualMachine.RunProgramInGuest(exePath,
                $@"scan ""{guestWorkingDirectory}\{fileName}"" /i4 /fa /RA:""{guestWorkingDirectory}\{fileName}.KAVReport.txt""");

            VirtualMachine.CopyFileFromGuestToHost($@"{guestWorkingDirectory}\{fileName}.KAVReport.txt",
                $@"{hostWorkingDirectory}\KAVReports\{fileName}.KAVReport.txt");

            try
            {
                VirtualMachine.DeleteFileFromGuest($@"{guestWorkingDirectory}\{fileName}");

                VirtualMachine.DeleteFileFromGuest($@"{guestWorkingDirectory}\{fileName}.KAVReport.txt");
            }
            catch
            {
                // ignored
            }

            return ParseReport(fileName);
        }

        public string Test(string infectedFile, string cleanFile) => "\nKAV test results:\n" +
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