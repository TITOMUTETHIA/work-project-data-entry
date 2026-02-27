namespace WorkTicketApp.Helpers;

public static class FormatUtils
{
    public static string FormatDateTime(DateTime? dateTime, string format = "dd/MM/yyyy HH:mm", string defaultValue = "-")
    {
        return dateTime?.ToString(format) ?? defaultValue;
    }
}