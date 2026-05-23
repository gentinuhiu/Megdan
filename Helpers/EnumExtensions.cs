namespace Megdan.Web.Helpers
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Converts an enum value to a human-readable format.
        /// For example: InProgress -> "In Progress", Urgent -> "Urgent"
        /// </summary>
        public static string ToDisplayString(this Enum value)
        {
            if (value == null)
                return "N/A";

            var enumType = value.GetType();
            var member = enumType.GetMember(value.ToString()).FirstOrDefault();

            if (member == null)
                return value.ToString();

            var displayAttribute = member.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
                .FirstOrDefault() as System.ComponentModel.DataAnnotations.DisplayAttribute;

            if (displayAttribute != null && !string.IsNullOrWhiteSpace(displayAttribute.Name))
                return displayAttribute.Name;

            // Fallback: Insert spaces before capitals
            return InsertSpacesBeforeCapitals(value.ToString());
        }

        private static string InsertSpacesBeforeCapitals(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var result = new System.Text.StringBuilder();
            foreach (char c in text)
            {
                if (char.IsUpper(c) && result.Length > 0)
                    result.Append(' ');
                result.Append(c);
            }
            return result.ToString();
        }
    }
}