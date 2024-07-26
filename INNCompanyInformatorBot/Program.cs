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
        private static readonly NameValueCollection AppSettings = ConfigurationManager.AppSettings;
        private static ITelegramBotClient? TGBotClient; // клиент для работы с TGBot API

        private static ReceiverOptions? ReceiverOptions; // объект с настройками работы бота

        static async Task Main()
        {
            string? TGBotAPIToken = null;

            foreach (string? Key in AppSettings.AllKeys.Where(key => (key ?? string.Empty).StartsWith("TGBotAPIFragmentKey")))
            {
                TGBotAPIToken += AppSettings[Key];
            }

            TGBotClient = new TelegramBotClient(TGBotAPIToken ?? string.Empty);
            ReceiverOptions = new()
            {
                AllowedUpdates =
                [
                    UpdateType.Message,
                    UpdateType.CallbackQuery // Inline кнопки
                ],
                ThrowPendingUpdates = true, // обработка сообщений за время оффлайн бота (false — обрабатывать)
            };

            using CancellationTokenSource CancellationTokenSource = new();

            TGBotClient.StartReceiving(UpdateHandler, ErrorHandler, ReceiverOptions, CancellationTokenSource.Token); // запуск бота

            User UserBot = await TGBotClient.GetMeAsync(); // переменная информации о боте
            Console.WriteLine($"{UserBot.FirstName} запущен!");

            await Task.Delay(-1); // бесконечная задержка для постоянной работы бота
        }

        /// <summary>
        /// Задача обработки входящих сообщений
        /// </summary>
        /// <param name="TGBotClient"></param>
        /// <param name="Update"></param>
        /// <param name="CancellationToken"></param>
        /// <returns></returns>
        private static async Task UpdateHandler(ITelegramBotClient TGBotClient, Update Update, CancellationToken CancellationToken)
        {
            try
            {
                switch (Update.Type)
                {
                    case UpdateType.Message:

                        Message? Message = Update.Message;
                        User? User = Message?.From; // From - от кого пришло сообщение (или любой другой Update)

                        Console.WriteLine($"{User?.FirstName} ({User?.Id}) написал сообщение: {Message?.Text}");

                        Chat? Chat = Message?.Chat; // вся информация о чате

                        switch (Message?.Type) // обработка типов сообщений
                        {
                            case MessageType.Text: // текстовый тип

                                // Обработка команд //

                                if (Message.Text == "/start")
                                {
                                    await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), "Выбери клавиатуру:\n" + "/inline\n" + "/reply\n");
                                    
                                    return;
                                }

                                else if (Message.Text == "/help")
                                {
                                    await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), "[Пока без помощи))]Выбери клавиатуру:\n" + "/inline\n" + "/reply\n");

                                    return;
                                }

                                else if (Message.Text == "/hello")
                                {
                                    string? CreatorInfo = null;

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

                                    return;
                                }

                                else if (Message.Text == "/inn")
                                {
                                    await ParseCompanyByINN("7719286104");
                                    //await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), "[Пока без помощи))]Выбери клавиатуру:\n" + "/inline\n" + "/reply\n");

                                    return;
                                }

                                else if (Message.Text == "/inline")
                                {
                                    // Тут создаём нашу клавиатуру
                                    InlineKeyboardMarkup InlineKeyboard = new
                                        (new List<InlineKeyboardButton[]>()
                                        {
                                                new InlineKeyboardButton[]
                                                {
                                                    InlineKeyboardButton.WithUrl("Это кнопка с сайтом", "https://google.com/"),
                                                    InlineKeyboardButton.WithCallbackData("А это просто кнопка", "button1"),
                                                },
                                                new InlineKeyboardButton[]
                                                {
                                                    InlineKeyboardButton.WithCallbackData("Тут еще одна", "button2"),
                                                    InlineKeyboardButton.WithCallbackData("И здесь", "button3"),
                                                },
                                        });

                                    await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), "Это inline клавиатура!", replyMarkup: InlineKeyboard); // передача всех клавиатур через параметр replyMarkup

                                    return;
                                }

                                else if (Message.Text == "/reply")
                                {
                                    ReplyKeyboardMarkup ReplyKeyboard = new
                                        (new List<KeyboardButton[]>()
                                        {
                                                new KeyboardButton[]
                                                {
                                                    new KeyboardButton("Привет!"),
                                                    new KeyboardButton("Пока!"),
                                                },
                                                new KeyboardButton[]
                                                {
                                                    new KeyboardButton("Позвони мне!")
                                                },
                                                new KeyboardButton[]
                                                {
                                                    new KeyboardButton("Напиши моему соседу!")
                                                }
                                        })
                                    {
                                        //ResizeKeyboard = true,
                                    };

                                    await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), "Это reply клавиатура!", replyMarkup: ReplyKeyboard);

                                    return;
                                }

                                else if (Message.Text == "Позвони мне!")
                                {
                                    await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), "Хорошо, присылай номер!", replyToMessageId: Message.MessageId);
                                    
                                    return;
                                }

                                else if (Message.Text == "Напиши моему соседу!")
                                {
                                    await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), "А самому что, трудно что-ли ?", replyToMessageId: Message.MessageId);
                                    
                                    return;
                                }

                                return;

                            default:

                                await TGBotClient.SendTextMessageAsync(Chat?.Id ?? new(), "Используй только текст!");
                                
                                return;
                        }

                        return;

                    case UpdateType.CallbackQuery:

                        CallbackQuery? CallbackQuery = Update.CallbackQuery; // переменная информации нажатой кнопки
                        User? User1 = CallbackQuery?.From;

                        Console.WriteLine($"{User1?.FirstName} ({User1?.Id}) нажал на кнопку: {CallbackQuery?.Data}");
                        
                        Chat? Chat1 = CallbackQuery?.Message?.Chat;

                        switch (CallbackQuery?.Data)
                        {
                            case "button1":

                                await TGBotClient.AnswerCallbackQueryAsync(CallbackQuery.Id);
                                await TGBotClient.SendTextMessageAsync(Chat1?.Id ?? new(), $"Вы нажали на {CallbackQuery.Data}");
                                
                                return;

                            case "button2":

                                await TGBotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, "Тут может быть ваш текст!");
                                await TGBotClient.SendTextMessageAsync(Chat1?.Id ?? new(), $"Вы нажали на {CallbackQuery.Data}");

                                return;

                            case "button3":

                                await TGBotClient.AnswerCallbackQueryAsync(CallbackQuery.Id, "А это полноэкранный текст!", showAlert: true);
                                await TGBotClient.SendTextMessageAsync(Chat1?.Id ?? new(), $"Вы нажали на {CallbackQuery.Data}");
                                
                                return;
                        }

                        return;
                }
            }

            catch (Exception Exception)
            {
                Console.WriteLine(Exception.ToString());
            }
        }

        private static async Task ParseCompanyByINN(string companyINN)
        {
            ChromeOptions ChromeOptions = new();
            ChromeOptions.AddArgument("--headless"); // скрытый запуск браузера

            ChromeDriverService ChromeDriverService = ChromeDriverService.CreateDefaultService();
            ChromeDriverService.HideCommandPromptWindow = true; // скрытие вывода информации о запуске браузера в командной строке

            using ChromeDriver ChromeDriver = new(ChromeDriverService, ChromeOptions);
            ChromeDriver.Navigate().GoToUrl($"https://www.rusprofile.ru/search?query={companyINN}&search_inactive=0");

            WebDriverWait WebDriverWait = new(ChromeDriver, TimeSpan.FromSeconds(10));
            WebDriverWait.Until(WebDriver => WebDriver.FindElement(By.XPath("//*[@id='ab-test-wrp']/div[1]/div[1]")));

            IWebElement ShortCompanyNameElement = ChromeDriver.FindElement(By.CssSelector("h1[itemprop='name']"));
            string ShortCompanyName = ShortCompanyNameElement.Text;

            IWebElement FullCompanyNameElement = ChromeDriver.FindElement(By.ClassName("company-header__full-name"));
            string FullCompanyName = FullCompanyNameElement.Text;

            IWebElement CompanyAddressElement = ChromeDriver.FindElement(By.Id("clip_address"));
            string CompanyAddress = CompanyAddressElement.Text;

            Console.WriteLine($"Краткое название компании: {ShortCompanyName}");
            Console.WriteLine($"Полное название компании: {FullCompanyName}");
            Console.WriteLine($"Адрес компании: {CompanyAddress}");

            ChromeDriver.Quit();
        }

        /// <summary>
        /// Задача обработки ошибок
        /// </summary>
        /// <param name="TGBotClient"></param>
        /// <param name="Error"></param>
        /// <param name="CancellationToken"></param>
        /// <returns></returns>
        private static Task ErrorHandler(ITelegramBotClient TGBotClient, Exception Error, CancellationToken CancellationToken)
        {
            string ErrorMessage;

            switch (Error)
            {
                case ApiRequestException ApiRequestException:

                    ErrorMessage = $"Telegram API Error:\n[{ApiRequestException.ErrorCode}]\n{ApiRequestException.Message}";

                    break;

                default:

                    ErrorMessage = Error.ToString();

                    break;
            }

            Console.WriteLine(ErrorMessage);

            return Task.CompletedTask;
        }
    }
}