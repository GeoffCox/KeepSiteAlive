using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BellaCode.KeepSiteAlive
{
    // A self-installing service using .NET classes in the ProjectInstaller
    // http://www.thedavejay.com/2012/04/self-installing-c-windows-service-safe.html
    public static class Program
    {
        private enum MainMode
        {
            Run,
            RunInConsole,
            InstallService,
            UninstallService,
        }

        static void Main(string[] args)
        {
            var mainMode = MainMode.Run;

            if (args.Length > 0)
            {
                switch (args[0].ToUpperInvariant())
                {
                    case "/I":
                        mainMode = MainMode.InstallService;
                        break;
                    case "/U":
                        mainMode = MainMode.UninstallService;
                        break;
                    case "/D":
                        mainMode = MainMode.RunInConsole;
                        break;
                }
            }

            if (mainMode == MainMode.Run && Environment.UserInteractive && Debugger.IsAttached)
            {
                mainMode = MainMode.RunInConsole;
            }
         
            var service = new BellaCodeKeepSiteAliveService();

            switch (mainMode)
            {

                case MainMode.RunInConsole:
                    {
                        service.CallOnStart(args);
                        Console.WriteLine("Press any key to stop the program.");
                        Console.ReadKey();
                        service.CallOnStop();
                    }
                    break;
                case MainMode.InstallService:

                    if (IsServiceInstalled(service))
                    {
                        UninstallService();
                    }

                    InstallService();
                    break;
                case MainMode.UninstallService:
                    UninstallService();
                    break;
                case MainMode.Run:
                default:
                    ServiceBase.Run(service);
                    break;
            }
        }

        private static bool IsServiceInstalled(ServiceBase service)
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == service.ServiceName);
        }

        private static void InstallService()
        {
            ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
        }

        private static void UninstallService()
        {
            ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
        }
    }
}
