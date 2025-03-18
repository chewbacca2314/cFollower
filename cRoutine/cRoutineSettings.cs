using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Common;

namespace cFollower.cRoutine
{
    public class cRoutineSettings : JsonSettings
    {
        private static cRoutineSettings _instance;
        public static cRoutineSettings Instance => _instance ?? (_instance = new cRoutineSettings());
        private cRoutineSettings() : base(GetSettingsFilePath(Configuration.Instance.Name, "cRoutineSettings.json"))
        {

        }
        private bool _debugEnabled;
        private bool _stopRequested;

        [DefaultValue(false)]
        public bool DebugEnabled
        {
            get { return _debugEnabled; }
            set
            {
                _debugEnabled = value;
                NotifyPropertyChanged(() => DebugEnabled);
            }
        }
        public bool StopRequested
        {
            get { return _stopRequested; }
            set
            {
                if (value == _stopRequested) return;
                _stopRequested = value;
                NotifyPropertyChanged(() => StopRequested);
            }
        }
    }
}
