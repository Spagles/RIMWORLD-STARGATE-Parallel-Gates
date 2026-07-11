using Verse;

namespace RimGateJaffaKree
{
    public static class StarGateText
    {
        public static string Get(string key)
        {
            return key.Translate();
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }

        public static string Value(string value, string fallback = "unknown")
        {
            if (value.NullOrEmpty())
            {
                return Get(fallback);
            }

            string key = "StarGate_Value_" + value.Replace(" ", "_").Replace("-", "_");
            string translated = key.Translate();
            return translated == key ? value : translated;
        }
    }
}
