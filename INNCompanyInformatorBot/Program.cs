using System.Collections.Specialized;
using System.Configuration;

using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Exceptions;

using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;

namespace INNCompanyInformatorBot
{
    public class Program
    {
        private static readonly NameValueCollection AppSettings = ConfigurationManager.AppSettings; // поле файла конфигурации
        private static TelegramBotClient? TGBotClient; // клиент для работы с API бота

        private static ReceiverOptions? ReceiverOptions; // объект с настройками работы бота
        private static ReplyKeyboardMarkup? ReplyKeyboardMarkup;

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

                using CancellationTokenSource CancellationTokenSource = new();

                TGBotClient.StartReceiving(UpdateHandler, ErrorHandler, ReceiverOptions, CancellationTokenSource.Token); // запуск бота

                User UserBot = await TGBotClient.GetMeAsync(); // переменная информации о боте
                Console.WriteLine($"{UserBot.FirstName} запущен!");

                await Task.Delay(-1); // бесконечная задержка для постоянной работы бота
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
                        User = Message?.From; // From - от кого пришло сообщение (или любой другой Update)
                        Chat = Message?.Chat; // вся информация о чате

                        switch (Message?.Type) // обработка типов сообщений
                        {
                            case MessageType.Text: // текстовый тип

                                ExecuteCommand(Message.Text, Chat);

                                Console.WriteLine($"{User?.FirstName} ({User?.Id}) написал сообщение: {Message?.Text}");

                                break;

                            default:

                                await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), "Данный бот может принимать только текст! 😡");
                                
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
            if (TGBotClient != null)
            {
                switch (Command)
                {
                    case "/start":

                        await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), $"Приветсвую вас, пользователь!\n" +
                            $"Я бот «Информатор компании по ИНН», и я помогу вам найти информацию о названии и адресе компании по её ИНН.\n\n" +
                            $"Для начала, выберите тип клавиатуры:\n" +
                            $"/inline — встроенная\n" +
                            $"/reply — кнопки");

                        break;

                    case "/inline":

                        InlineKeyboardMarkup InlineKeyboardMarkup = new
                                (new List<InlineKeyboardButton[]>()
                                {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Помощь", "/help"),
                                                InlineKeyboardButton.WithCallbackData("О моём создателе", "/hello"),
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Поиск организации(-й) по ИНН", "/inn"),
                                                InlineKeyboardButton.WithCallbackData("Повтор последней команды", "/last"),
                                            },
                                });

                        await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), $"Отлично!", replyMarkup: new ReplyKeyboardRemove());
                        await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), $"Строго, но практично 😉\nИтак, чтобы вы хотели сделать?", replyMarkup: InlineKeyboardMarkup);

                        break;

                    case "/reply":

                        await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), $"Отличный выбор! Скорее, нажимайте на любую из них 😄", replyMarkup: ReplyKeyboardMarkup);

                        break;

                    case "/help":
                    case "Помощь":

                        //await TGBotClient.AnswerCallbackQueryAsync(CallbackQuery.Id);
                        await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), $"Список доступных комманд:\n" +
                            $"\n/help — вывод справки о доступных командах\n" +
                            $"\n/hello — вывод информации о создателе бота\n" +
                            $"\n/inn — вывод информации (наименования и адреса) компании(-й) по ИНН\n" +
                            $"\n/last — повтор последней команды");

                        break;

                    case "/hello":
                    case "О моём создателе":

                        //await TGBotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, "Тут может быть ваш текст!");
                        string? CreatorInfo = null;

                        try
                        {
                            foreach (string? Key in AppSettings.AllKeys.Where(key => !(key ?? string.Empty).StartsWith("TGBotAPIFragmentKey")))
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

                            await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), $"{CreatorInfo}");
                        }

                        catch
                        {
                            await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), $"Похоже, что я ничего не могу вспомнить про него 🤔" +
                                $"Ничего, мы в процессе исправление проблем с моей памятью 😂");

                            Console.WriteLine("Боту не удалось извлечь информацию о создателе! Возможно возникли ошибки или файл App.config, содержащий токены этой информации повреждён!");
                        }

                        break;

                    case "/inn":
                    case "Поиск организации(-й) по ИНН":

                        //await TGBotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, "А это полноэкранный текст!", showAlert: true);
                        await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), "Пожалуйста, введите ИНН организации(-й):");
                        string? userInput = null;
                        string[] innNumbers = userInput.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        await ParseCompanyByINN(["7719286104", "7720675962"]);

                        break;

                    case "/last":
                    case "Повтор последней команды":

                        break;
                }
            }
        }

        /// <summary>
        /// Асинхронная задача парсинга сайта для получения информации по ИНН
        /// </summary>
        /// <param name="CompaniesINN"></param>
        /// <returns></returns>
        private static async Task ParseCompanyByINN(string[] CompaniesINN)
        {
            try
            {
                ChromeOptions ChromeOptions = new();
                ChromeOptions.AddArgument("--headless"); // скрытый запуск браузера

                ChromeDriverService ChromeDriverService = ChromeDriverService.CreateDefaultService();
                ChromeDriverService.HideCommandPromptWindow = true; // скрытие вывода информации о запуске браузера в командной строке

                using ChromeDriver ChromeDriver = new(ChromeDriverService, ChromeOptions);

                foreach (string CompanyINN in CompaniesINN)
                {
                    ChromeDriver.Navigate().GoToUrl($"https://www.rusprofile.ru/search?query={CompanyINN}&search_inactive=0");

                    WebDriverWait WebDriverWait = new(ChromeDriver, TimeSpan.FromSeconds(10));
                    WebDriverWait.Until(WebDriver => WebDriver.FindElement(By.XPath("//*[@id='ab-test-wrp']/div[1]/div[1]")));

                    IWebElement ShortCompanyNameElement = ChromeDriver.FindElement(By.CssSelector("h1[itemprop='name']"));
                    string ShortCompanyName = ShortCompanyNameElement.Text;

                    IWebElement FullCompanyNameElement = ChromeDriver.FindElement(By.ClassName("company-header__full-name"));
                    string FullCompanyName = FullCompanyNameElement.Text;

                    IWebElement CompanyAddressElement = ChromeDriver.FindElement(By.Id("clip_address"));
                    string CompanyAddress = CompanyAddressElement.Text;

                    Console.WriteLine($"ИНН: {CompanyINN}");
                    Console.WriteLine($"Краткое название компании: {ShortCompanyName}");
                    Console.WriteLine($"Полное название компании: {FullCompanyName}");
                    Console.WriteLine($"Адрес компании: {CompanyAddress}");
                    Console.WriteLine();
                }
            }

            catch (Exception Exception)
            {
                Console.WriteLine($"Возникло исключение: {Exception.Message}");
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