using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace UtilityBot
{
    class Bot : BackgroundService
    {
        private readonly ITelegramBotClient _telegramClient;

        public Bot(ITelegramBotClient telegramClient)
        {
            _telegramClient = telegramClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var cts = new CancellationTokenSource();

            // Запускаем обработку обновлений
            _telegramClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                cancellationToken: cts.Token);

            Console.WriteLine("Bot started working...");
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message.Type == MessageType.Text)
            {
                var messageText = update.Message.Text;

                if (messageText.StartsWith("/start"))
                {
                    await SendMainMenu(update.Message.From.Id, cancellationToken);
                }
                else if (messageText.Equals("Подсчитать символы в тексте", StringComparison.OrdinalIgnoreCase))
                {
                    await _telegramClient.SendTextMessageAsync(update.Message.From.Id, "Пожалуйста, введите текст для подсчета символов:", cancellationToken: cancellationToken);
                }
                else if (messageText.Equals("Вычислить сумму чисел", StringComparison.OrdinalIgnoreCase))
                {
                    await _telegramClient.SendTextMessageAsync(update.Message.From.Id, "Пожалуйста, введите числа через пробел для их суммы:", cancellationToken: cancellationToken);
                }
                else if (!string.IsNullOrWhiteSpace(messageText))
                {
                    var response = await ProcessUserInput(messageText, update.Message.From.Id, cancellationToken);
                    await _telegramClient.SendTextMessageAsync(update.Message.From.Id, response, cancellationToken: cancellationToken);
                }
            }
        }

        private async Task SendMainMenu(long chatId, CancellationToken cancellationToken)
        {
            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("Подсчитать символы в тексте") },
                new[] { new KeyboardButton("Вычислить сумму чисел") },
            })
            {
                ResizeKeyboard = true
            };

            await _telegramClient.SendTextMessageAsync(chatId, "Выберите действие:", replyMarkup: replyKeyboard, cancellationToken: cancellationToken);
        }

        private async Task<string> ProcessUserInput(string message, long chatId, CancellationToken cancellationToken)
        {
            var numbers = message.Split(' ').Select(x => double.TryParse(x, out var num) ? num : (double?)null).ToArray();
            if (numbers.All(x => x.HasValue))
            {
                var sum = numbers.Sum(x => x.Value);
                return $"Сумма чисел: {sum}";
            }
            else
            {
                return $"В вашем сообщении {message.Length} символов.";
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }
    }
}