using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace BellaCode.KeepSiteAlive
{
    public partial class BellaCodeKeepSiteAliveService : ServiceBase
    {
        private System.Diagnostics.EventLog serviceEventLog;

        public BellaCodeKeepSiteAliveService()
        {
            InitializeComponent();

            if (!System.Diagnostics.EventLog.SourceExists("BellaCodeKeepSiteAliveService"))
            {
                System.Diagnostics.EventLog.CreateEventSource("BellaCodeKeepSiteAliveService", string.Empty);
            }

            this.serviceEventLog = new System.Diagnostics.EventLog();
            this.serviceEventLog.Source = "BellaCodeKeepSiteAliveService";
        }

        internal void CallOnStart(string[] args)
        {
            this.OnStart(args);
        }

        private Task[] _keepAliveTasks = null;
        private static volatile bool _isStopRequested = false;

        protected override void OnStart(string[] args)
        {
            try
            {
                _isStopRequested = false;   //TODO: CancelationToken

                var siteUrls = LoadSiteUrls();

                if (siteUrls != null && siteUrls.Any())
                {
                    _keepAliveTasks = siteUrls.Select(x => StartKeepAlive(x)).ToArray();
                }

                this.serviceEventLog.WriteEntry(string.Format("Service Started. Discovered {0} sites to keep alive.", _keepAliveTasks.Count()));
            }
            catch (Exception ex)
            {
                this.serviceEventLog.WriteEntry(string.Format("There was a problem starting the service. {0}", ex.Message), EventLogEntryType.Error);
            }
        }

        internal void CallOnStop()
        {
            this.OnStop();
        }

        protected override void OnStop()
        {
            if (_keepAliveTasks != null)
            {
                _isStopRequested = true;
                Task.WaitAll(this._keepAliveTasks, TimeSpan.FromSeconds(30));
            }
        }

        private static char[] SiteConfigSplitCharacters = new char[] { ' ', '\t' };
        private static TimeSpan DefaultKeepAliveDelay = TimeSpan.FromSeconds(5);

        private IEnumerable<SiteConfiguration> LoadSiteUrls()
        {
            try
            {
                var exePath = Assembly.GetExecutingAssembly().Location;
                var exeDirectory = Path.GetDirectoryName(exePath);
                var siteUrlsFile = Path.Combine(exeDirectory, "SiteUrls.txt");

                var lines = File.ReadAllLines(siteUrlsFile);

                var sites = lines.Select(x =>
                    {
                        var parts = x.Split(SiteConfigSplitCharacters, StringSplitOptions.RemoveEmptyEntries);
                        var site = new SiteConfiguration();

                        if (parts.Length > 1)
                        {
                            site.Delay = TimeSpan.Parse(parts[0]);
                            site.Url = parts[1];
                        }
                        else if (parts.Length > 0)
                        {
                            site.Delay = DefaultKeepAliveDelay;
                            site.Url = parts[0];
                        }

                        return site;
                    });

                return sites.Where(x => x.Url != null && x.Delay > TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                this.serviceEventLog.WriteEntry(string.Format("There was a problem loading SiteUrls.txt. {1}", ex.Message), EventLogEntryType.Error);
                return null;
            }

        }

        private Task StartKeepAlive(SiteConfiguration site)
        {
            return Task.Factory.StartNew(() =>
                {
                    this.serviceEventLog.WriteEntry(string.Format("Started keeping '{0}' alive.", site.Url));

                    while (!_isStopRequested)
                    {
                        try
                        {
                            var request = WebRequest.Create(site.Url);
                            request.UseDefaultCredentials = true;
                            request.PreAuthenticate = true;                            

                            using (var response = request.GetResponse() as HttpWebResponse)
                            {
                                if (response.StatusCode != HttpStatusCode.OK)
                                {
                                    this.serviceEventLog.WriteEntry(string.Format("Call to '{0}' returned status code '{1}'.", site.Url, response.StatusCode), EventLogEntryType.Warning);
                                }
                                Debug.WriteLine("[" + DateTime.Now + "] " + site.Url + " : " + response.StatusCode);
                            }

                        }
                        catch (Exception ex)
                        {
                            this.serviceEventLog.WriteEntry(string.Format("There was a problem with the call to '{0}'. {1}", site.Url, ex.Message), EventLogEntryType.Error);
                            Debug.WriteLine("[" + DateTime.Now + "] " + site.Url + " : " + ex.Message);
                        }

                        Thread.Sleep(site.Delay);
                    }
                }, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
        }

        private class SiteConfiguration
        {
            public string Url { get; set; }
            public TimeSpan Delay { get; set; }
        }
    }
}
