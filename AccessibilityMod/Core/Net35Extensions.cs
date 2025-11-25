namespace AccessibilityMod.Core
{
    public static class Net35Extensions
    {
        public static bool IsNullOrWhiteSpace(string value)
        {
            if (value == null)
                return true;

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i]))
                    return false;
            }
            return true;
        }

        public static void ClearStringBuilder(System.Text.StringBuilder sb)
        {
            sb.Length = 0;
        }
    }
}
