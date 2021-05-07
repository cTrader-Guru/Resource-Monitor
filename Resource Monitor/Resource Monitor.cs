using System;
using cAlgo.API;
using System.Net;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace cAlgo.Robots
{

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class ResourceMonitor : Robot
    {

        #region Enum

        public enum MyColors
        {

            AliceBlue,
            AntiqueWhite,
            Aqua,
            Aquamarine,
            Azure,
            Beige,
            Bisque,
            Black,
            BlanchedAlmond,
            Blue,
            BlueViolet,
            Brown,
            BurlyWood,
            CadetBlue,
            Chartreuse,
            Chocolate,
            Coral,
            CornflowerBlue,
            Cornsilk,
            Crimson,
            Cyan,
            DarkBlue,
            DarkCyan,
            DarkGoldenrod,
            DarkGray,
            DarkGreen,
            DarkKhaki,
            DarkMagenta,
            DarkOliveGreen,
            DarkOrange,
            DarkOrchid,
            DarkRed,
            DarkSalmon,
            DarkSeaGreen,
            DarkSlateBlue,
            DarkSlateGray,
            DarkTurquoise,
            DarkViolet,
            DeepPink,
            DeepSkyBlue,
            DimGray,
            DodgerBlue,
            Firebrick,
            FloralWhite,
            ForestGreen,
            Fuchsia,
            Gainsboro,
            GhostWhite,
            Gold,
            Goldenrod,
            Gray,
            Green,
            GreenYellow,
            Honeydew,
            HotPink,
            IndianRed,
            Indigo,
            Ivory,
            Khaki,
            Lavender,
            LavenderBlush,
            LawnGreen,
            LemonChiffon,
            LightBlue,
            LightCoral,
            LightCyan,
            LightGoldenrodYellow,
            LightGray,
            LightGreen,
            LightPink,
            LightSalmon,
            LightSeaGreen,
            LightSkyBlue,
            LightSlateGray,
            LightSteelBlue,
            LightYellow,
            Lime,
            LimeGreen,
            Linen,
            Magenta,
            Maroon,
            MediumAquamarine,
            MediumBlue,
            MediumOrchid,
            MediumPurple,
            MediumSeaGreen,
            MediumSlateBlue,
            MediumSpringGreen,
            MediumTurquoise,
            MediumVioletRed,
            MidnightBlue,
            MintCream,
            MistyRose,
            Moccasin,
            NavajoWhite,
            Navy,
            OldLace,
            Olive,
            OliveDrab,
            Orange,
            OrangeRed,
            Orchid,
            PaleGoldenrod,
            PaleGreen,
            PaleTurquoise,
            PaleVioletRed,
            PapayaWhip,
            PeachPuff,
            Peru,
            Pink,
            Plum,
            PowderBlue,
            Purple,
            Red,
            RosyBrown,
            RoyalBlue,
            SaddleBrown,
            Salmon,
            SandyBrown,
            SeaGreen,
            SeaShell,
            Sienna,
            Silver,
            SkyBlue,
            SlateBlue,
            SlateGray,
            Snow,
            SpringGreen,
            SteelBlue,
            Tan,
            Teal,
            Thistle,
            Tomato,
            Transparent,
            Turquoise,
            Violet,
            Wheat,
            White,
            WhiteSmoke,
            Yellow,
            YellowGreen

        }

        #endregion

        #region Identity

        public const string NAME = "Resource Monitor";

        public const string VERSION = "1.0.0";

        #endregion

        #region Params

        [Parameter("Seconds To Check", Group = "Monitoring", DefaultValue = 3, MinValue = 1, Step = 1)]
        public int MonitorCheck { get; set; }

        [Parameter("Trigger (%)", Group = "CPU", DefaultValue = 90, MinValue = 2, MaxValue = 100, Step = 1)]
        public int CPUTrigger { get; set; }

        [Parameter("Reset (%)", Group = "CPU", DefaultValue = 40, MinValue = 1, MaxValue = 100, Step = 1)]
        public int CPUReset { get; set; }

        [Parameter("Enabled?", Group = "Webhook", DefaultValue = false)]
        public bool WebhookEnabled { get; set; }

        [Parameter("API", Group = "Webhook", DefaultValue = "https://api.telegram.org/bot[ YOUR TOKEN ]/sendMessage")]
        public string Webhook { get; set; }

        [Parameter("POST params", Group = "Webhook", DefaultValue = "chat_id=[ @CHATID ]&text={0}")]
        public string PostParams { get; set; }

        [Parameter("Color", Group = "Styles", DefaultValue = MyColors.Coral)]
        public MyColors Boxcolor { get; set; }

        [Parameter("Vertical Position", Group = "Styles", DefaultValue = VerticalAlignment.Top)]
        public VerticalAlignment VAlign { get; set; }

        [Parameter("Horizontal Position", Group = "Styles", DefaultValue = API.HorizontalAlignment.Left)]
        public API.HorizontalAlignment HAlign { get; set; }

        #endregion

        #region Property

        PerformanceCounter cpuCounter;
        PerformanceCounter ramCounter;
        bool AlertCPUSent = false;

        #endregion

        #region cBot Method

        protected override void OnStart()
        {

            Timer.Start(MonitorCheck);
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");

        }

        protected override void OnTimer()
        {

            base.OnTimer();

            double CurrentCPU = Math.Round(cpuCounter.NextValue(), 2);
            double AvailableRAM = Math.Round(ramCounter.NextValue(), 2);

            if (!AlertCPUSent && CurrentCPU >= CPUTrigger)
            {

                _sendCPUmessage(CurrentCPU);

            }
            else if (CurrentCPU <= CPUReset)
            {

                AlertCPUSent = false;

            }

            if (RunningMode == RunningMode.RealTime || RunningMode == RunningMode.VisualBacktesting)
            {

                Chart.DrawStaticText(NAME, String.Format("CPU : {0}% ( {1} / {2} )\tRAM Available : {3}MB", (CurrentCPU > 0) ? CurrentCPU.ToString() : "...", CPUTrigger, CPUReset, (AvailableRAM > 0) ? AvailableRAM.ToString() : "..."), VAlign, HAlign, Color.FromName(Boxcolor.ToString("G")));

            }

        }

        protected override void OnTick()
        {

            // --> TODO

        }

        protected override void OnStop()
        {

            _toWebHook(NAME + " is stopped");

        }

        #endregion

        #region Private Method

        public void _toWebHook(string message)
        {

            if (!WebhookEnabled || !(RunningMode == RunningMode.RealTime || RunningMode == RunningMode.VisualBacktesting))
                return;

            string messageformat = string.Format("{0} : {1}", Environment.MachineName, message);

            try
            {
                // --> Mi servono i permessi di sicurezza per il dominio, compreso i redirect
                Uri myuri = new Uri(Webhook);

                string pattern = string.Format("{0}://{1}/.*", myuri.Scheme, myuri.Host);

                // --> Autorizzo tutte le pagine di questo dominio
                Regex urlRegEx = new Regex(pattern);
                WebPermission p = new WebPermission(NetworkAccess.Connect, urlRegEx);
                p.Assert();

                // --> Protocollo di sicurezza https://
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;

                using (WebClient wc = new WebClient())
                {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    string HtmlResult = wc.UploadString(myuri, string.Format(PostParams, messageformat));
                }

            } catch (Exception exc)
            {

                MessageBox.Show(string.Format("{0}\r\nstopping cBots...", exc.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Stop();

            }

        }

        public void _sendCPUmessage(double CPUvalue)
        {

            if (CPUvalue <= 0)
                return;

            string message = string.Format("CPU out of range : {0}% ( {1} / {2} )", CPUvalue, CPUTrigger, CPUReset);

            _toWebHook(message);

            AlertCPUSent = true;

        }

        #endregion

    }

}
