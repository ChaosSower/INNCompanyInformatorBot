using System.Configuration;

System.Collections.Specialized.NameValueCollection AppSettings = ConfigurationManager.AppSettings;

foreach (string? Key in AppSettings.AllKeys)
{
    Console.WriteLine("Key: {0} Value: {1}", Key, AppSettings[Key]);
}