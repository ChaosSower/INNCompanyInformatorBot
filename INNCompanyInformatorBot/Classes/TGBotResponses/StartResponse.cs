using INNCompanyInformatorBot.Classes.AbstractClasses;

namespace INNCompanyInformatorBot.Classes.TGBotResponses
{
    /// <summary>
    /// Класс вывода приветственного сообщения
    /// </summary>
    internal class StartResponse : ResponseClass
    {
        public override string Response => $"Приветсвую вас, пользователь!\n" +
            $"Я бот «Информатор компании по ИНН», и я помогу вам найти информацию о названии и адресе компании по её ИНН.\n\n" +
            $"Для начала, выберите тип клавиатуры:\n" +
            $"/inline — встроенная\n" +
            $"/reply — кнопки";
    }
}