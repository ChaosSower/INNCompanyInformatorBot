using System.Collections.Specialized;
using System.Configuration;

using INNCompanyInformatorBot.Classes.AbstractClasses;

namespace INNCompanyInformatorBot.Classes.TGBotResponses
{
    /// <summary>
    /// Класс сообщения вывода информации о создателе бота
    /// </summary>
    internal class HelloResponse : ResponseClass
    {
        private static readonly NameValueCollection AppSettings = ConfigurationManager.AppSettings; // поле файла конфигурации
        private readonly string? _Response;
        public bool IsExceptionOccurred { get; private set; }

        public HelloResponse()
        {
            string? CreatorInfo = null;

            try
            {
                foreach (string? Key in AppSettings.AllKeys.Where(Key => !(Key ?? string.Empty).StartsWith("TGBotAPIFragmentKey")))
                {
                    if (Key == "Name")
                    {
                        CreatorInfo += AppSettings[Key] + " ";
                    }

                    else if (Key == "GitHubRepositoryLink")
                    {
                        CreatorInfo += AppSettings[Key];
                    }

                    else
                    {
                        CreatorInfo += AppSettings[Key] + "\n";
                    }
                }

                _Response = CreatorInfo;
                IsExceptionOccurred = false;
            }

            catch
            {
                _Response = $"Похоже, что я ничего не могу вспомнить про моего создателя 🤔\n" +
                    $"Ничего, мы в процессе исправление проблем с моей памятью 😂";

                IsExceptionOccurred = true;
            }

            finally
            {
                if (_Response == null)
                {
                    _Response = $"Похоже, что я ничего не могу вспомнить про моего создателя 🤔\n" +
                        $"Ничего, мы в процессе исправление проблем с моей памятью 😂";

                    IsExceptionOccurred = true;
                }
            }
        }

        public override string Response
        {
            get
            {
                if (_Response != null)
                {
                    return _Response;
                }

                else
                {
                    return string.Empty;
                }
            }
        }
    }
}