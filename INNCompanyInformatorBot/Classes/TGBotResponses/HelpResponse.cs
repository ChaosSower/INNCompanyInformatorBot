using INNCompanyInformatorBot.Classes.AbstractClasses;

namespace INNCompanyInformatorBot.Classes.TGBotResponses
{
    /// <summary>
    /// Класс сообщения вывода информации о доступных командах
    /// </summary>
    internal class HelpResponse : ResponseClass
    {
        public override string Response => $"Список доступных комманд:\n\n" +
            $"/inline — встроенная клавиатура\n" +
            $"/reply — кнопочная клавиатура\n" +
            $"/help — вывод справки о доступных командах\n" +
            $"/hello — вывод информации о создателе бота\n" +
            $"/inn — вывод информации (наименования и адреса) компании(-й) по ИНН\n" +
            $"/last — повтор последней команды";
    }
}