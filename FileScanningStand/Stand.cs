using System;
using System.Collections.Generic;
using System.IO;
using Vestris.VMWareLib;

namespace FileScanningStand
{
    public class Stand
    {
        private const string Login = "User";

        private const string Password = "1111";

        private const string GuestWorkingDirectory = @"C:\Users\User\Desktop\FileScanningStand";

        private static readonly string HostWorkingDirectory = Environment.CurrentDirectory;

        private const string InfectedTestFile = "EICAR.txt";

        private const string CleanTestFile = "CLEAN.docx";

        private static KasperskyApi kasperskyMachine;

        private const string KasperskyVmxPath =
            @"C:\Users\User\Documents\Virtual Machines\Windows_7_Starter_Kaspersky\Windows_7_Starter_Kaspersky.vmx";

        private const string AvpPath = @"C:\Program Files\Kaspersky Lab\Kaspersky Anti-Virus 19.0.0\avp.com";

        private const string KavBasesXml = @"C:\ProgramData\Kaspersky Lab\AVP19.0.0\Bases\hips-1313g.xml";

        private static DrWebApi drWebMachine;

        private const string DrWebVmxPath =
            @"C:\Users\User\Documents\Virtual Machines\Windows_7_Starter_DrWeb\Windows_7_Starter_DrWeb.vmx";

        private const string DwscanclPath = @"C:\Program Files\DrWeb\dwscancl.exe";

        private const string DwBasesXml = @"C:\ProgramData\Doctor Web\Updater\repo\versions.xml";

        public Stand()
        {
            var virtualHost = new VMWareVirtualHost();

            virtualHost.ConnectToVMWareWorkstation();

            kasperskyMachine = new KasperskyApi(virtualHost, KasperskyVmxPath, Login, Password, AvpPath,
                GuestWorkingDirectory, HostWorkingDirectory);

            drWebMachine = new DrWebApi(virtualHost, DrWebVmxPath, Login, Password, DwscanclPath, GuestWorkingDirectory,
                HostWorkingDirectory);
        }

        public void Scan(string path, ref List<string> res)
        {
            try
            {
                var subfolders = Directory.GetDirectories(path);

                var files = Directory.GetFiles(path);

                foreach (var subfolder in subfolders)
                    Scan(subfolder, ref res);

                foreach (var file in files)
                {
                    res.Add("KAVReport:");

                    res.Add(kasperskyMachine.Scan(file));

                    res.Add("DWReport:");

                    res.Add(drWebMachine.Scan(file));
                }
            }
            catch (IOException)
            {
                res.Add("KAVReport:");

                res.Add(kasperskyMachine.Scan(path));

                res.Add("DWReport:");

                res.Add(drWebMachine.Scan(path));
            }
        }

        public string GetBasesDate() =>
            kasperskyMachine.GetBasesDate(KavBasesXml) + "\n" + drWebMachine.GetBasesDate(DwBasesXml) + "\n";

        public string Test() => kasperskyMachine.Test(InfectedTestFile, CleanTestFile) +
                                drWebMachine.Test(InfectedTestFile, CleanTestFile);

        public void RevertToSnapshot()
        {
            kasperskyMachine.RevertToSnapshot();

            drWebMachine.RevertToSnapshot();
        }

        public void Exit()
        {
            kasperskyMachine.Exit();

            drWebMachine.Exit();
        }
    }
}