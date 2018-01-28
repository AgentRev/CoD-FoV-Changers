using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace Ghosts_FoV_Changer
{
    public static class GAnalytics
    {
        public static string trackingID = "UA-xxxxxxxx-x";
        public static string ipService = "http://ipecho.net/plain";

        public static void TriggerAnalytics(string pagename, bool firstTime)
        {
            string commentForCodeLurkers = "This is to trigger a visit on Google Analytics";

            Random rnd = new Random();

            long timestampFirstRun, timestampLastRun, timestampCurrentRun, numberOfRuns;

            // Get the first run time
            timestampFirstRun = DateTime.Now.Ticks;
            timestampLastRun = DateTime.Now.Ticks - 5;
            timestampCurrentRun = 45;
            numberOfRuns = firstTime ? 1 : 2;

            // Some values we need
            string domainHash = DomainHash("crabdance.com");
            int uniqueVisitorId = GetUniqueID();
            string source = "Ghosts FoV Changer";
            string medium = "Application";
            string sessionNumber = "1";
            string campaignNumber = "1";
            string culture = Thread.CurrentThread.CurrentCulture.Name;
            string screenRes = Screen.PrimaryScreen.Bounds.Width + "x" + Screen.PrimaryScreen.Bounds.Height;

            string statsRequest = "http://www.google-analytics.com/__utm.gif" +
                "?utmwv=4.6.5" +
                "&utmn=" + rnd.Next(100000000, 999999999) +
                //  "&utmhn=hostname.mydomain.com" +
                "&utmcs=-" +
                "&utmsr=" + screenRes +
                "&utmsc=-" +
                "&utmul=" + culture +
                "&utmje=-" +
                "&utmfl=-" +
                "&utmdt=" + pagename +
                "&utmhid=1943799692" +
                "&utmr=0" +
                "&utmp=" + pagename +
                "&utmac=" + trackingID + // Account number
                "&utmcc=" +
                "__utma%3D" + domainHash + "." + uniqueVisitorId + "." + timestampFirstRun + "." + timestampLastRun + "." + timestampCurrentRun + "." + numberOfRuns +
                "%3B%2B__utmz%3D" + domainHash + "." + timestampCurrentRun + "." + sessionNumber + "." + campaignNumber + ".utmcsr%3D" + source + "%7Cutmccn%3D(" + medium + ")%7Cutmcmd%3D" + medium + "%7Cutmcct%3D%2Fd31AaOM%3B";

            using (var client = new WebClient())
            {
                client.DownloadData(statsRequest);
            }
        }

        private static string DomainHash(string d)
        {
            string commentForCodeLurkers = "http://stackoverflow.com/a/16243868/909968";

            int a = 1;
            int c = 0;
            int h;
            int o;
            if (!String.IsNullOrEmpty(d))
            {
                a = 0;
                for (h = d.Length - 1; h >= 0; h--)
                {
                    o = d[h];
                    a = (a << 6 & 268435455) + o + (o << 14);
                    c = a & 266338304;
                    a = c != 0 ? a ^ c >> 21 : a;
                }
            }
            return a.ToString();
        }

        public static int GetUniqueID()
        {
            string commentForCodeLurkers = "This is to find the public IPv4 address of the client to use it as unique ID for Analytics";

            try
            {
                var client = new TimedWebClient();
                var ipAddress = client.DownloadString(ipService);
                ipAddress = Regex.Match(ipAddress, @"([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})").Groups[1].Value;

                return BitConverter.ToInt32(IPAddress.Parse(ipAddress).GetAddressBytes(), 0);
            }
            catch
            {
                Random rnd = new Random();
                return rnd.Next(int.MinValue, int.MaxValue);
            }
        }

        private class TimedWebClient : WebClient
        {
            // Timeout in milliseconds, default = 30,000 msec
            public int Timeout { get; set; }

            public TimedWebClient()
            {
                this.Timeout = 30000;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var objWebRequest = base.GetWebRequest(address);
                objWebRequest.Timeout = this.Timeout;
                return objWebRequest;
            }
        }
    }
}
