using System.Collections.Specialized;
using System.Configuration;
using System.Text.RegularExpressions;

using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Exceptions;

using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;

using INNCompanyInformatorBot.Classes.TGBotResponses;

namespace INNCompanyInformatorBot
{
    public partial class Program
    {
        private static readonly NameValueCollection AppSettings = ConfigurationManager.AppSettings; // поле файла конфигурации
        
        private static TelegramBotClient? TGBotClient; // клиент для работы с API бота
        private static ReceiverOptions? ReceiverOptions; // объект с настройками работы бота
        private static InlineKeyboardMarkup? InlineKeyboardMarkup; // "линейная" клавиатура
        private static ReplyKeyboardMarkup? ReplyKeyboardMarkup; // кнопочная клавиатура

        private static readonly string StartResponse = new StartResponse().Response; // приветственное сообщение
        private static readonly string HelpResponse = new HelpResponse().Response; // сообщение вывода возможных команд
        private static readonly HelloResponse HelloResponse = new(); // сообщение вывода информации о создателе проекта

        private static bool IsAwaitingInnInput = false; // поле состояния ожидания ИНН от пользователя
        private static string? LastCommand = null; // поле последней команды бота

        /// <summary>
        /// Regex, пропускающий только числа с разделителем пробел и/или запятая
        /// </summary>
        /// <returns></returns>
        [GeneratedRegex(@"^[0-9\s,]+$")]
        private static partial Regex OnlyDigitsRegex();

        private static async Task Main()
        {
            string? TGBotAPIToken = null;

            try
            {
                foreach (string? Key in AppSettings.AllKeys.Where(Key => Key!.StartsWith("TGBotAPIFragmentKey")))
                {
                    TGBotAPIToken += AppSettings[Key];
                }
            }

            catch
            {
                Console.WriteLine("Возникла ошибка считывания данных с файла App.config!");
            }

            if (TGBotAPIToken != null)
            {
                TGBotClient = new(TGBotAPIToken);

                ReceiverOptions = new()
                {
                    AllowedUpdates =
                    [
                        UpdateType.Message,
                        UpdateType.CallbackQuery // Inline кнопки
                    ],
                    ThrowPendingUpdates = true, // обработка сообщений за время оффлайн бота (false — обрабатывать)
                };

                InlineKeyboardMarkup = new
                (
                    [
                        [InlineKeyboardButton.WithCallbackData("Встроенная клавиатура", "/inline"), InlineKeyboardButton.WithCallbackData("Кнопочная клавиатура", "/reply"), InlineKeyboardButton.WithCallbackData("Помощь", "/help")],
                        [InlineKeyboardButton.WithCallbackData("О моём создателе", "/hello"), InlineKeyboardButton.WithCallbackData("Поиск организации(-й) по ИНН", "/inn"), InlineKeyboardButton.WithCallbackData("Повтор последней команды", "/last")]
                    ]
                );

                ReplyKeyboardMarkup = new
                (
                    [
                        ["Помощь", "О моём создателе"],
                        ["Поиск организации(-й) по ИНН", "Повтор последней команды"]
                    ]
                )
                {
                    ResizeKeyboard = true
                };

                await Task.Run(async () =>
                {
                    TGBotClient.StartReceiving(UpdateHandler, ErrorHandler, ReceiverOptions); // запуск бота

                    User UserBot = await TGBotClient.GetMeAsync(); // переменная информации о боте
                    Console.WriteLine($"Бот «{UserBot.FirstName}» запущен!");
                });

                Console.WriteLine("Нажмите любую клавишу для остановки бота...");
                Console.ReadKey();
                Console.WriteLine("Бот был остановлен.");
            }

            else
            {
                Console.WriteLine("Бот не был запущен! Возможно возникли ошибки или файл App.config, содержащий токены API повреждён!");
            }
        }

        /// <summary>
        /// Асинхронная задача обработки входящих сообщений
        /// </summary>
        /// <param name="TGBotClient"></param>
        /// <param name="Update"></param>
        /// <param name="CancellationToken"></param>
        /// <returns></returns>
        private static async Task UpdateHandler(ITelegramBotClient TGBotClient, Update Update, CancellationToken CancellationToken)
        {
            try
            {
                Message? Message;
                User? User;
                Chat? Chat;

                switch (Update.Type)
                {
                    case UpdateType.Message:

                        Message = Update.Message;
                        User = Message?.From; // From — от кого пришло сообщение (или любой другой Update)
                        Chat = Message?.Chat; // вся информация о чате

                        switch (Message?.Type) // обработка типов сообщений
                        {
                            case MessageType.Text: // текстовый тип

                                ExecuteCommand(Message.Text, Chat);

                                Console.WriteLine($"{User?.FirstName} ({User?.Id}) написал сообщение: {Message?.Text}");

                                break;

                            default:

                                await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), "Данный бот может принимать только текст! 😡", cancellationToken: CancellationToken);
                                
                                Console.WriteLine($"{User?.FirstName} ({User?.Id}) отправил неприемлемый тип сообщения");

                                break;
                        }

                        break;

                    case UpdateType.CallbackQuery:

                        CallbackQuery? CallbackQuery = Update.CallbackQuery; // переменная информации нажатой кнопки
                        User = CallbackQuery?.From;
                        Chat = CallbackQuery?.Message?.Chat;

                        ExecuteCommand(CallbackQuery?.Data, Chat);

                        Console.WriteLine($"{User?.FirstName} ({User?.Id}) нажал на кнопку: {CallbackQuery?.Data}");

                        break;
                }
            }

            catch (Exception Exception)
            {
                Console.WriteLine($"Возникло исключение: {Exception.Message}");
            }
        }

        /// <summary>
        /// Асинхронный метод выполнения команды пользователя
        /// </summary>
        /// <param name="Command"></param>
        /// <param name="Chat"></param>
        private static async void ExecuteCommand(string? Command, Chat? Chat)
        {
            if (TGBotClient != null && Chat != null)
            {
                switch (Command?.ToLower())
                {
                    case "/start":

                        await TGBotClient.SendTextMessageAsync(Chat.Id, StartResponse);

                        break;

                    case "/inline":
                    case "встроенная клавиатура":
                    case "встроенная":

                        await TGBotClient.SendTextMessageAsync(Chat.Id, $"Отлично!", replyMarkup: new ReplyKeyboardRemove());
                        await TGBotClient.SendTextMessageAsync(Chat.Id, $"Строго, но практично 😉\nИтак, что бы вы хотели сделать?", replyMarkup: InlineKeyboardMarkup);

                        break;

                    case "/reply":
                    case "кнопочная клавиатура":
                    case "кнопочная":

                        await TGBotClient.SendTextMessageAsync(Chat.Id, $"Отличный выбор! Скорее, нажимайте на любую из них 😄", replyMarkup: ReplyKeyboardMarkup);

                        break;

                    case "/help":
                    case "помощь":

                        await TGBotClient.SendTextMessageAsync(Chat.Id, HelpResponse);

                        break;

                    case "/hello":
                    case "о моём создателе":
                    case "о моем создателе":
                    case "о создателе":
                    case "информация":
                    case "инфо":

                        await TGBotClient.SendTextMessageAsync(Chat.Id, HelloResponse.Response);

                        if (HelloResponse.IsExceptionOccurred)
                        {
                            Console.WriteLine("Боту не удалось извлечь информацию о создателе! Возможно возникли ошибки или файл App.config, содержащий токены этой информации повреждён!");
                        }

                        break;

                    case "/inn":
                    case "поиск организации(-й) по инн":
                    case "поиск по инн":
                    case "поиск":

                        await TGBotClient.SendTextMessageAsync(Chat.Id, "Пожалуйста, введите ИНН организации(-й)\n[Через пробел или запятую]:");
                        IsAwaitingInnInput = true;

                        break;

                    case "/last":
                    case "повтор последней команды":
                    case "повтор":

                        if (LastCommand != null && LastCommand != "/last" && LastCommand != "Повтор последней команды")
                        {
                            ExecuteCommand(LastCommand, Chat);
                        }

                        else
                        {
                            await TGBotClient.SendTextMessageAsync(Chat.Id, "Не помню, чтобы до этого были какие-либо команды! 🤔");
                        }

                        break;
                    
                    default:

                        if (IsAwaitingInnInput)
                        {
                            if (Command != null)
                            {
                                Regex DigitRegex = OnlyDigitsRegex();

                                if (DigitRegex.IsMatch(Command))
                                {
                                    await TGBotClient.SendTextMessageAsync(Chat.Id, $"Сейчас поищу 😉\nПожалуйсита, ожидайте");

                                    string[] CompaniesINN = Command.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                                    HashSet<string> UniqueCompaniesINNs = new(CompaniesINN);
                                    await ParseCompanyByINN(Chat, UniqueCompaniesINNs);

                                    await TGBotClient.SendTextMessageAsync(Chat.Id, $"Помочь ли чем-нибудь ещё? 😃");
                                    IsAwaitingInnInput = false;
                                }

                                else
                                {
                                    await TGBotClient.SendTextMessageAsync(Chat.Id, $"Ай-яй-яй, входные данные содержат недопустимые символы 😲\n" +
                                        $"Но я никому не скажу 🤫\n" +
                                        $"Пожалуйста, введите корректные ИНН организации(-й)\n[Через пробел или запятую]:");

                                    Console.WriteLine("Входные данные пользователя содержали недопустимые символы.");
                                }
                            }
                        }

                        else
                        {
                            await TGBotClient.SendTextMessageAsync(Chat.Id, $"Я вас не совсем понимаю 🤨\nПожалуйсита, используйте команды, доступные для моего понимания!");
                            await TGBotClient.SendTextMessageAsync(Chat.Id, HelpResponse);
                        }

                        return;
                }

                LastCommand = Command;
            }
        }

        /// <summary>
        /// Асинхронная задача парсинга сайта для получения информации по ИНН
        /// </summary>
        /// <param name="Chat"></param>
        /// <param name="CompaniesINN">Уникальный массив ИНН компаний</param>
        /// <returns></returns>
        private static async Task ParseCompanyByINN(Chat Chat, HashSet<string> CompaniesINN)
        {
            ChromeOptions ChromeOptions = new();
            ChromeOptions.AddArgument("--headless"); // скрытый запуск браузера

            ChromeDriverService ChromeDriverService = ChromeDriverService.CreateDefaultService();
            ChromeDriverService.HideCommandPromptWindow = true; // скрытие вывода информации о запуске браузера в командной строке

            using ChromeDriver ChromeDriver = new(ChromeDriverService, ChromeOptions);
            WebDriverWait WebDriverWait = new(ChromeDriver, TimeSpan.FromSeconds(10));

            foreach (string CompanyINN in CompaniesINN)
            {
                try
                {
                    ChromeDriver.Navigate().GoToUrl($"https://www.rusprofile.ru/search?query={CompanyINN}&search_inactive=0");

                    WebDriverWait.Until(WebDriver => WebDriver.FindElement(By.XPath("//*[@id='ab-test-wrp']/div[1]/div[1]")));

                    IWebElement ShortCompanyNameElement = ChromeDriver.FindElement(By.CssSelector("h1[itemprop='name']"));
                    string ShortCompanyName = ShortCompanyNameElement.Text;

                    IWebElement FullCompanyNameElement = ChromeDriver.FindElement(By.ClassName("company-header__full-name"));
                    string FullCompanyName = FullCompanyNameElement.Text;

                    IWebElement CompanyAddressElement = ChromeDriver.FindElement(By.Id("clip_address"));
                    string CompanyAddress = CompanyAddressElement.Text;

                    if (TGBotClient != null)
                    {
                        await TGBotClient.SendTextMessageAsync(Chat.Id, $"ИНН: {CompanyINN}\n" +
                            $"Краткое название компании: {ShortCompanyName}\n" +
                            $"Полное название компании: {FullCompanyName}\n" +
                            $"Адрес компании: {CompanyAddress}");
                    }
                }

                catch (Exception Exception)
                {
                    Console.WriteLine($"\nВозникло исключение при обработке компании с ИНН {CompanyINN}: {Exception.Message}");
                    
                    if (TGBotClient != null)
                    {
                        await TGBotClient.SendTextMessageAsync(Chat.Id, $"Я не смог найти организацию с таким ИНН: {CompanyINN} 😳");
                    }

                    if (CompanyINN != CompaniesINN.Last())
                    {
                        Console.WriteLine();
                    }
                }
            }
        }

        /// <summary>
        /// Задача обработки исключений
        /// </summary>
        /// <param name="TGBotClient"></param>
        /// <param name="Exception"></param>
        /// <param name="CancellationToken"></param>
        /// <returns></returns>
        private static Task ErrorHandler(ITelegramBotClient TGBotClient, Exception Exception, CancellationToken CancellationToken)
        {
            string ErrorMessage;

            switch (Exception)
            {
                case ApiRequestException ApiRequestException:

                    ErrorMessage = $"Ошибка Telegram API:\n[{ApiRequestException.ErrorCode}]\n{ApiRequestException.Message}";

                    break;

                default:

                    ErrorMessage = Exception.Message;

                    break;
            }

            Console.WriteLine($"Возникло исключение: {ErrorMessage}");

            return Task.CompletedTask;
        }
    }
}