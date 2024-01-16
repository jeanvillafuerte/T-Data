using System.Globalization;

namespace Thomas.Database
{
    public sealed class DbSettings
    {
        public string Signature { get; set; }
        public string Culture { get; set; } = "en-US";
        public string StringConnection { get; set; }

        /// <summary>
        /// Show detail error message in error message and log adding store procedure name and parameters
        /// </summary>
        public bool DetailErrorMessage { get; set; }

        /// <summary>
        /// Hide sensible data value in error message and log
        /// </summary>
        public bool HideSensibleDataValue { get; set; }
        public int ConnectionTimeout { get; set; } = 0;

        private CultureInfo? _cultureInfo;
        public CultureInfo CultureInfo
        {
            get
            {
                if(_cultureInfo == null)
                    _cultureInfo = string.IsNullOrEmpty(Culture) ? CultureInfo.InvariantCulture : new CultureInfo(Culture);
                return _cultureInfo;
            }
        }
    }
}
