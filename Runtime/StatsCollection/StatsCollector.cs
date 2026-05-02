using System.Globalization;

namespace LGSToolbox
{
    public class StatsCollector
    {
        public StatsSession Session { get; private set; }

        public StatsCollector()
        {
            Session = new StatsSession();
        }

        public void SetCustom(string key, string value)
        {
            Session.SetCustom(key, value);
        }

        public void SetInt(string key, int value)
        {
            Session.SetCustom(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public void SetFloat(string key, float value)
        {
            Session.SetCustom(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public void SetBool(string key, bool value)
        {
            Session.SetCustom(key, value ? "true" : "false");
        }

        public void IncrementInt(string key, int amount = 1)
        {
            string currentValue = Session.GetCustom(key, "0");

            int currentInt = 0;
            int.TryParse(currentValue, out currentInt);

            Session.SetCustom(key, (currentInt + amount).ToString(CultureInfo.InvariantCulture));
        }

        public void ClearCustom()
        {
            Session.ClearCustom();
        }
    }
}