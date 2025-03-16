using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Common;

namespace TestPlugin.cMover
{
    public class cMoverSettings : JsonSettings
    {
        private static cMoverSettings _instance;
        public static cMoverSettings Instance => _instance ?? (_instance = new cMoverSettings());
        private cMoverSettings() : base(GetSettingsFilePath(Configuration.Instance.Name, "cMoverSettings.json"))
        {

        }
        private int _pathRefreshRate;
        private int _minMoveDistance;
        private bool _randomizeMove;

        [DefaultValue(true)]
        public bool RandomizeMove
        {
            get { return _randomizeMove; }
            set
            {
                if (value == _randomizeMove) return;
                _randomizeMove = value;
                NotifyPropertyChanged(() => RandomizeMove);
            }
        }
        public int PathRefreshRate
        {
            get { return _pathRefreshRate; }
            set
            {
                if (value == _pathRefreshRate) return;
                _pathRefreshRate = value;
                NotifyPropertyChanged(() => PathRefreshRate);
            }
        }

        public int MinMoveDistance
        {
            get { return _minMoveDistance; }
            set
            {
                if (value == _minMoveDistance) return;
                _minMoveDistance = value;
                NotifyPropertyChanged(() => MinMoveDistance);
            }
        }
    }
}
