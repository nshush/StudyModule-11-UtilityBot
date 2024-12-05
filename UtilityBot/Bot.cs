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
            _telegramClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                new ReceiverOptions() { AllowedUpdates = { } },
                cancellationToken: stoppingToken);

            Console.WriteLine("Bot started");
        }

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
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
                else
                {
                    if (!string.IsNullOrWhiteSpace(messageText))
                    {
                        if (messageText.Contains(" "))
                        {
                            await SumNumbers(messageText, update.Message.From.Id, cancellationToken);
                        }
                        else
                        {
                            await CountChars(messageText, update.Message.From.Id, cancellationToken);
                        }
                    }
                    else
                    {
                        await _telegramClient.SendTextMessageAsync(update.Message.From.Id, "Пожалуйста, введите текст для обработки.", cancellationToken: cancellationToken);
                    }
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

        private async Task CountChars(string text, long chatId, CancellationToken cancellationToken)
        {
            var charCount = text.Length;
            await _telegramClient.SendTextMessageAsync(chatId, $"В вашем сообщении {charCount} символов.", cancellationToken: cancellationToken);
        }

        private async Task SumNumbers(string message, long chatId, CancellationToken cancellationToken)
        {
            var numbers = message.Split(' ')
                                 .Select(x => double.TryParse(x, out var num) ? num : 0)
                                 .ToArray();
            var sum = numbers.Sum();
            await _telegramClient.SendTextMessageAsync(chatId, $"Сумма чисел: {sum}", cancellationToken: cancellationToken);
        }

        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            Console.WriteLine("Waiting 10 seconds before retry");
            Thread.Sleep(10000);
            return Task.CompletedTask;
        }
    }
}