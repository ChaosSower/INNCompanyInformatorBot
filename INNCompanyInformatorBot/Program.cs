using System.Collections.Specialized;
using System.Configuration;

using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Exceptions;

namespace INNCompanyInformatorBot
{
    public class Program
    {
        private static readonly NameValueCollection AppSettings = ConfigurationManager.AppSettings;
        private static ITelegramBotClient? _botClient; // клиент для работы с TGBot API

        private static ReceiverOptions? _receiverOptions; // объект с настройками работы бота

        static async Task Main()
        {
            string? TGBotAPIToken = null;

            foreach (string? Key in AppSettings.AllKeys)
            {
                TGBotAPIToken += AppSettings[Key];
            }

            _botClient = new TelegramBotClient(TGBotAPIToken ?? string.Empty);
            _receiverOptions = new()
            {
                AllowedUpdates =
                [
                    UpdateType.Message,
                    UpdateType.CallbackQuery // Inline кнопки
                ],
                ThrowPendingUpdates = true, // обработка сообщений за время оффлайн бота (false — обрабатывать)
            };

            using CancellationTokenSource CancellationTikenSource = new();

            _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, CancellationTikenSource.Token); // запуск бота

            User UserBot = await _botClient.GetMeAsync(); // переменная информации о боте
            Console.WriteLine($"{UserBot.FirstName} запущен!");

            await Task.Delay(-1); // бесконечная задержка для постоянной работы бота
        }

        /// <summary>
        /// Задача обработки входящих сообщений
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="update"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:

                        Message? message = update.Message;
                        User? user = message?.From; // From - от кого пришло сообщение (или любой другой Update)

                        Console.WriteLine($"{user?.FirstName} ({user?.Id}) написал сообщение: {message?.Text}");

                        Chat? chat = message?.Chat; // вся информация о чате

                        switch (message?.Type) // обработка типов сообщений
                        {
                            case MessageType.Text: // текстовый тип

                                if (message.Text == "/start") // обработка команды /start
                                {
                                    await botClient.SendTextMessageAsync(chat?.Id ?? new(), "Выбери клавиатуру:\n" + "/inline\n" + "/reply\n");
                                    
                                    return;
                                }

                                if (message.Text == "/inline")
                                {
                                    // Тут создаем нашу клавиатуру
                                    InlineKeyboardMarkup inlineKeyboard = new
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

                                    await botClient.SendTextMessageAsync(chat?.Id ?? new(), "Это inline клавиатура!", replyMarkup: inlineKeyboard); // передача всех клавиатур через параметр replyMarkup

                                    return;
                                }

                                if (message.Text == "/reply")
                                {
                                    ReplyKeyboardMarkup replyKeyboard = new
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

                                    await botClient.SendTextMessageAsync(chat?.Id ?? new(), "Это reply клавиатура!", replyMarkup: replyKeyboard);

                                    return;
                                }

                                if (message.Text == "Позвони мне!")
                                {
                                    await botClient.SendTextMessageAsync(chat?.Id ?? new(), "Хорошо, присылай номер!", replyToMessageId: message.MessageId);
                                    
                                    return;
                                }

                                if (message.Text == "Напиши моему соседу!")
                                {
                                    await botClient.SendTextMessageAsync(chat?.Id ?? new(), "А самому что, трудно что-ли ?", replyToMessageId: message.MessageId);
                                    
                                    return;
                                }

                                return;

                            default:

                                await botClient.SendTextMessageAsync(chat?.Id ?? new(), "Используй только текст!");
                                
                                return;
                        }

                        return;

                    case UpdateType.CallbackQuery:

                        CallbackQuery? callbackQuery = update.CallbackQuery; // переменная информации нажатой кнопки
                        User? user1 = callbackQuery?.From;

                        Console.WriteLine($"{user1?.FirstName} ({user1?.Id}) нажал на кнопку: {callbackQuery?.Data}");
                        
                        Chat? chat1 = callbackQuery?.Message?.Chat;

                        switch (callbackQuery?.Data)
                        {
                            case "button1":

                                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
                                await botClient.SendTextMessageAsync(chat1?.Id ?? new(), $"Вы нажали на {callbackQuery.Data}");
                                
                                return;

                            case "button2":

                                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "Тут может быть ваш текст!");
                                await botClient.SendTextMessageAsync(chat1?.Id ?? new(), $"Вы нажали на {callbackQuery.Data}");

                                return;

                            case "button3":

                                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "А это полноэкранный текст!", showAlert: true);
                                await botClient.SendTextMessageAsync(chat1?.Id ?? new(), $"Вы нажали на {callbackQuery.Data}");
                                
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

        /// <summary>
        /// Задача обработки ошибок
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="error"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
        {
            string ErrorMessage;

            switch (error)
            {
                case ApiRequestException apiRequestException:

                    ErrorMessage = $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}";

                    break;

                default:

                    ErrorMessage = error.ToString();

                    break;
            }

            Console.WriteLine(ErrorMessage);

            return Task.CompletedTask;
        }
    }
}