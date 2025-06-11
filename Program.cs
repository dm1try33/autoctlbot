using static Telegram.Bot.TelegramBotClient;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System;
using System.Net.Http;
using System.Text.Json;


class Car
{
    public string Name { get; set; }
    public int Year { get; set; }
    public decimal Price { get; set; }
    public string Engine { get; set; }
    public int Power { get; set; }
    public string Transmission { get; set; }
    public int MaxSpeed { get; set; }
    public int Massa { get; set; }
    

}

class Program
{
    private static Dictionary<long, bool> waitingForYear = new Dictionary<long, bool>();
    // Это клиент для работы с Telegram Bot API, который позволяет отправлять сообщения, управлять ботом, подписываться на обновления и многое другое.
    private static ITelegramBotClient _botClient;

    // Это объект с настройками работы бота. Здесь мы будем указывать, какие типы Update мы будем получать, Timeout бота и так далее.
    private static ReceiverOptions _receiverOptions;
    
    private static readonly string UsersFile = "users.txt";
    private static readonly long chatId = 1085651731;
    private static CancellationTokenSource _cts;
    private static HashSet<long> _users = new HashSet<long>();
    private static readonly List<int> sentMessageIds = new List<int>();
    private static string? selectedCar1 = null;
    private static string? selectedCar2 = null;
    
    static async Task Main()
    {

        _botClient = new TelegramBotClient("7729505729:AAE4JcpLbZu4lklwrAeNh95ZaWUP0NL6ta4");
        _cts = new CancellationTokenSource();

        LoadUsers();




        // Бесконечный цикл для поддержания работы приложения


        // Присваиваем нашей переменной значение, в параметре передаем Token, полученный от BotFather
        _receiverOptions = new ReceiverOptions // Также присваем значение настройкам бота
        {
            AllowedUpdates = new[] // Тут указываем типы получаемых Update`ов, о них подробнее расказано тут https://core.telegram.org/bots/api#update
            {
                UpdateType.Message,// Сообщения (текст, фото/видео, голосовые/видео сообщения и т.д.)
                UpdateType.CallbackQuery // Inline кнопки
                
            },
            // Параметр, отвечающий за обработку сообщений, пришедших за то время, когда ваш бот был оффлайн
            // True - не обрабатывать, False (стоит по умолчанию) - обрабаывать
            DropPendingUpdates = true
        };

        using var cts = new CancellationTokenSource();

        // UpdateHander - обработчик приходящих Update`ов
        // ErrorHandler - обработчик ошибок, связанных с Bot API

        _botClient.StartReceiving(UpdateHandler, ErrorHandler, _receiverOptions, cts.Token);

        // Запускаем бота
        Console.WriteLine("Бот успешно запущен....");
        await SendMessageAllUsersAsync("Бот запущен и готов к работе!");
        Console.CancelKeyPress += async (sender, e) =>
        {
            e.Cancel = true; // Отменяем стандартное завершение, чтобы успеть отправить сообщения

            Console.WriteLine("Получен сигнал завершения. Оповещаем пользователей...");

            await SendMessageAllUsersAsync("Бот останавливается. До скорой встречи!");

            StopBot(); // Отменяем токен для остановки StartReceiving

            Environment.Exit(0); // Завершаем приложение
        };
        await Task.Delay(-1);


        
    }
    static void LoadUsers()
    {
        if (File.Exists(UsersFile))
        {
            var lines = File.ReadAllLines(UsersFile);
            foreach (var line in lines)
            {
                if (long.TryParse(line, out long chatId))
                    _users.Add(chatId);
            }
        }
    }

    static void SaveUsers()
    {
        // Записываем всех пользователей из _users в файл
        File.WriteAllLines(UsersFile, _users.Select(id => id.ToString()));
    }
    static async Task SendMessageAllUsersAsync(string text)
    {
        foreach (var chatId in _users)
        {
            try
            {
                await _botClient.SendMessage(chatId, text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось отправить сообщение пользователю {chatId}: {ex.Message}");
            }
        }
    }
        
    
    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Обязательно ставим блок try-catch, чтобы наш бот не "падал" в случае каких-либо ошибок
        try
        {
            // Сразу же ставим конструкцию switch, чтобы обрабатывать приходящие Update
            switch (update.Type)
            {
                case UpdateType.Message:
                    {
                        // Эта переменная будет содержать в себе все связанное с сообщениями
                        var message = update.Message;

                        // From - это от кого пришло сообщение (или любой другой Update)
                        var user = message.From;
                        var callbackQuery = update.CallbackQuery;
                        // Выводим на экран то, что пишут нашему боту, а также небольшую информацию об отправителе
                        Console.WriteLine($"{user.FirstName} ({user.Id}) написал сообщение: {message.Text}");

                        // Chat - содержит всю информацию о чате

                        
                        var chat = message.Chat;
                        var chatId = chat.Id;
                        
                        if (_users.Add(chatId))
                        {
                            SaveUsers();
                            Console.WriteLine($"Добавлен новый пользователь: {chatId}");
                        }
                        var messageText = update.Message.Text;
                        switch (messageText)
                        {
                            case "/currency":
                                var ratesMessage = await GetCurrencyRatesAsync();
                                await botClient.SendMessage(chatId, ratesMessage, cancellationToken: cancellationToken);
                                return;
                                break;
                            case "/compare":
                                selectedCar1 = null; // Сбросить предыдущие выборы
                                selectedCar2 = null;
                                var inlineKeyboard2 = new InlineKeyboardMarkup(
                                           new List<InlineKeyboardButton[]>() // здесь создаем лист (массив), который содрежит в себе массив из класса кнопок
                                           {
                                        // Каждый новый массив - это дополнительные строки,
                                        // а каждая дополнительная кнопка в массиве - это добавление ряда

                                        new InlineKeyboardButton[] // тут создаем массив кнопок
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Toyota", "/toyota1"),
                                            InlineKeyboardButton.WithCallbackData("🔗Nissan", "/nissan1"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Mersedes", "/mersedes1"),
                                            InlineKeyboardButton.WithCallbackData("🔗BMW", "/bmw1"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Лада", "/lada1"),
                                            InlineKeyboardButton.WithCallbackData("🔗Lexus", "/lexus1"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Ford", "/ford1"),
                                            InlineKeyboardButton.WithCallbackData("🔗Mazda", "/mazda1"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Audi", "/audi1"),
                                            InlineKeyboardButton.WithCallbackData("🔗Volkswagen", "/volkswagen1"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Honda", "/honda1"),
                                            InlineKeyboardButton.WithCallbackData("🔗Hyundai", "/hyundai1"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Kia", "/kia1"),
                                            InlineKeyboardButton.WithCallbackData("🔗Mitsubishi", "/mitsubishi1"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Главное меню", "/start")
                                        }
                                        

                                        
                                           });
                                await botClient.SendMessage(chatId,
                                    "🚙✨ <b>Выберите марку первой машины:</b>",
                                    cancellationToken: cancellationToken,
                                    replyMarkup: inlineKeyboard2,
                                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                                return;
                                break;

                            
                            


                          
                        }
                        // Добавляем проверку на тип Message
                        switch (message.Type)
                        {
                            

                            // Тут понятно, текстовый тип
                            case MessageType.Text:
                                {
                                    // тут обрабатываем команду /start, остальные аналогично
                                    if (message.Text == "/start")
                                    {

                                        var start = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Католог машин", "/autocatalog"),

                                            }

                                                    });
                                        await botClient.SendMessage(
                                            chat.Id,
                                            "🌟 <b>Главное меню:</b>\n" +
                                            "               \n" +
                                            "Каталог машин:\n" +
                                            "🔗 /autocatalog\n" +
                                            "Сравнить 2 разных автомобиля:\n" +
                                            "🔗 /compare ( Готово не до конца )\n" +
                                            "Найти машину по ее году выпуска:\n" +
                                            "🔗 /search_year ( Готово не до конца )\n" +
                                            "Актуальный курс валют:\n" +
                                            "🔗 /currency ( Готово не до конца )\n" +
                                            "Подписаться на рассылку бота:\n" +
                                            "🔗 /sub\n" +
                                            "Отписаться от рассылки бота:\n" +
                                            "🔗 /unsub\n" +
                                            "Прочие команды:\n" +
                                            "🔗 /commands\n" +
                                            "               \n" +
                                            "🚀 <b>Дополнительные упрощенные команды:</b>\n" +
                                            "" +
                                            "🔗 /add\n",
                                            
                                                                        
                                                                    

                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                            replyMarkup: start
                                        );
                                        return;
                                    }
                                    if (message.Text == "/add")
                                    {
                                        var add = new InlineKeyboardMarkup(
                                                   new List<InlineKeyboardButton[]>()
                                                   {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),

                                            }

                                                   });
                                        await botClient.SendMessage(
                                            chat.Id,
                                            " 🚀<b>Дополнительные команды для упрощенной навигации:</b>\r\nToyota: 🔘 /toyota\r\nNissan: 🔘 /nissan\r\nMercedes: 🔘 /mersedes\r\nBMW: 🔘 /bmw\r\nЛада: 🔘 /lada\r\nLexus: 🔘 /lexus\r\nFord: 🔘 /ford\r\nMazda: 🔘 /mazda\r\nAudi: 🔘 /audi\r\nVolkswagen: 🔘 /volkswagen\r\nHonda: 🔘 /honda\r\nHyundai: 🔘 /hyundai\r\nKia: 🔘 /kia\r\nMitsubishi: 🔘 /mitsubishi",
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                            replyMarkup: add
                                            );
                                        return;
                                    }
                                    if (message.Text == "/sub")
                                    {
                                        // Подписка на рассылку
                                        if (_users.Add(chat.Id))
                                        {
                                            SaveUsers(); // Сохраняем изменения
                                            await botClient.SendMessage(chat.Id, "Вы успешно подписались на рассылку.");
                                        }
                                        else
                                        {
                                            await botClient.SendMessage(chat.Id, "Вы уже подписаны на рассылку.");
                                        }
                                        return;
                                    }
                                    else if (message.Text == "/unsub")
                                    {
                                        // Отписка от рассылки
                                        if (_users.Remove(chat.Id))
                                        {
                                            SaveUsers(); // Сохраняем изменения
                                            await botClient.SendMessage(chat.Id, "Вы успешно отписались от рассылки.");
                                        }
                                        else
                                        {
                                            await botClient.SendMessage(chat.Id, "Вы не были подписаны на рассылку.");
                                        }
                                        return;
                                    }

                                    if (message.Text == "/autocatalog")
                                    {
                                        // Тут создаем нашу клавиатуру
                                        var inlineKeyboard = new InlineKeyboardMarkup(
                                            new List<InlineKeyboardButton[]>() // здесь создаем лист (массив), который содрежит в себе массив из класса кнопок
                                            {
                                        // Каждый новый массив - это дополнительные строки,
                                        // а каждая дополнительная кнопка в массиве - это добавление ряда

                                        new InlineKeyboardButton[] // тут создаем массив кнопок
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Toyota", "/toyota"),
                                            InlineKeyboardButton.WithCallbackData("🔗Nissan", "/nissan"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Mersedes", "/mersedes"),
                                            InlineKeyboardButton.WithCallbackData("🔗BMW", "/bmw"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("мЛада", "/lada"),
                                            InlineKeyboardButton.WithCallbackData("🔗Lexus", "/lexus"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Ford", "/ford"),
                                            InlineKeyboardButton.WithCallbackData("🔗Mazda", "/mazda"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Audi", "/audi"),
                                            InlineKeyboardButton.WithCallbackData("🔗Volkswagen", "/volkswagen"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Honda", "/honda"),
                                            InlineKeyboardButton.WithCallbackData("🔗Hyundai", "/hyundai"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Kia", "/kia"),
                                            InlineKeyboardButton.WithCallbackData("🔗Mitsubishi", "/mitsubishi"),
                                        },

                                        new InlineKeyboardButton []
                                        {
                                            InlineKeyboardButton.WithCallbackData("Назад", "/start")
                                        }
                                            });

                                        await botClient.SendMessage(
                                            chat.Id,
                                            "<b>🚗✨ Список самых популярных марок автомобилей:</b>", // Жирный текст без экранирования
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                            replyMarkup: inlineKeyboard
                                            );

                                        return;
                                    }

                                    if (message.Text == "/commands")
                                    {
                                        // Тут все аналогично Inline клавиатуре, только меняются классы
                                        // НО! Тут потребуется дополнительно указать один параметр, чтобы
                                        // клавиатура выглядела нормально, а не как абы что

                                        var replyKeyboard = new ReplyKeyboardMarkup(
                                            new List<KeyboardButton[]>()
                                            {
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Сосал?"),
                                            new KeyboardButton("Привет!"),
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Пока!")
                                        },
                                        new KeyboardButton[]
                                        {
                                            new KeyboardButton("Создатель бота!")
                                        }
                                            })
                                        {
                                            // автоматическое изменение размера клавиатуры, если не стоит true,
                                            // тогда клавиатура растягивается чуть ли не до луны,
                                            // проверить можете сами
                                            ResizeKeyboard = true,
                                        };

                                        await botClient.SendMessage(
                                            chat.Id,
                                            "<b>Прочие полезные команды!</b>",
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                            replyMarkup: replyKeyboard); // опять передаем клавиатуру в параметр replyMarkup

                                        return;
                                    }
                                    if (message.Text == "Сосал?")
                                    {
                                        await botClient.SendMessage(
                                            chat.Id,
                                            "ДА");

                                        return;
                                    }

                                    if (message.Text == "Привет!")
                                    {
                                        await botClient.SendMessage(
                                            chat.Id,
                                            "Привет!");

                                        return;
                                    }

                                    if (message.Text == "Пока!")
                                    {
                                        await botClient.SendMessage(
                                            chat.Id,
                                            "Пока!"
                                            );

                                        return;
                                    }
                                    if (message.Text == "Создатель бота!")
                                    {
                                        await botClient.SendMessage(
                                            chat.Id,
                                            "<b>Разработчик:</b> @dm1try33👨‍💻",
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                                            );


                                        return;
                                    }
                                    if (message.Text == "/toyota")
                                    {
                                        var newinlineke1 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Toyota Mark II (x90), 1993", "/toyotamr"),
                                                InlineKeyboardButton.WithCallbackData("Toyota Chaser (x100), 1999", "/toyotach")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Toyota Cresta JZD100, 2000", "/toyotacr"),
                                                InlineKeyboardButton.WithCallbackData("Toyota Camry 3.5, 2015", "/toyotaca")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Toyota Land Cruiser 4.7, 1985", "/toyotacri"),
                                                InlineKeyboardButton.WithCallbackData("Toyota Land Cruiser Prado 4.0, 2010", "/toyotacrpr")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")
                                            }
                                                    });
                                        await botClient.SendMessage(
                                            chat.Id,
                                            $"<b>🚙✨ Самые популярные модели Toyota:</b>\n",
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                            replyMarkup: newinlineke1);
                                        return;
                                    }
                                    if (message.Text == "/nissan")
                                    {
                                        var nissan = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Nissan Silvia, 2002", "/nissansi"),
                                                InlineKeyboardButton.WithCallbackData("Nissan SkyLine, 1998", "/nissansk")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Nissan Laurel, 1999", "/nissanla"),
                                                InlineKeyboardButton.WithCallbackData("Nissan Sunny, 2000", "/nissansu")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Nissan Sentra, 2002", "/nissanse"),
                                                InlineKeyboardButton.WithCallbackData("Nissan GT-R, 2008", "/nissangtr")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                        await botClient.SendMessage(
                                            chat.Id,
                                            $"<b>🚙✨ Самые популярные модели Nissan:</b>\n",

                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                            replyMarkup: nissan);
                                        return;
                                    }
                                    if (message.Text == "/mersedes")
                                    {
                                        var mersedes = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mersedes E-Class, 1995", "/mersedese"),
                                                InlineKeyboardButton.WithCallbackData("Mersedes C-Class, 2003", "/mersedesc")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mersedes G-Class, 1985", "/mersedesg"),
                                                InlineKeyboardButton.WithCallbackData("Mersedes S-Class, 1992", "/mersedess")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mersedes M-Class, 2001", "/mersedesm"),
                                                InlineKeyboardButton.WithCallbackData("Mersedes W123, 1982", "/mersedesw")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                        await botClient.SendMessage(
                                            chat.Id,
                                            $"<b>🚙✨ Самые популярные модели Mersedes:</b>",
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                            replyMarkup: mersedes);
                                        return;
                                    }
                                    if (message.Text == "/bmw")
                                    {
                                        var bmw = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("BMW M5 E34, 1995", "/bmwe34"),
                                                InlineKeyboardButton.WithCallbackData("BMW M5 E39, 2000", "/bmwe39")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("BMW M5 E60, 2005", "/bmwe60"),
                                                InlineKeyboardButton.WithCallbackData("BMW M5 F10, 2012", "bmwf10")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("BMW 3-Series, 2011", "/bmw3se"),
                                                InlineKeyboardButton.WithCallbackData("BMW 5-Series, 2001", "/bmw5se")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });


                                        await botClient.SendMessage(
                                            chat.Id,
                                            $"<b>🚙✨ Самые популярные модели BMW:</b>",
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                            replyMarkup: bmw);
                                        return;
                                    }
                                    if (message.Text == "/lada")
                                    {
                                        var lada = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Лада 2107, 2010", "/lada07"),
                                                InlineKeyboardButton.WithCallbackData("Лада 2105, 2007", "/lada05")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Лада 2114, 2010", "/lada14"),
                                                InlineKeyboardButton.WithCallbackData("Лада Приора, 2012", "/ladapr")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Лада 2112, 2008", "/lada12"),
                                                InlineKeyboardButton.WithCallbackData("Лада 2109, 2003", "/lada09")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                        await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Самые популярные модели Лада:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lada);
                                        return;
                                    }

                                    if (message.Text == "/lexus")
                                    {
                                        var lexus = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Lexus IS200, 2000", "/lexusis200"),
                                                InlineKeyboardButton.WithCallbackData("Lexus IS300, 2019", "/lexusis300")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Lexus IS250, 2006", "/lexusis250"),
                                                InlineKeyboardButton.WithCallbackData("Lexus ES300h, 2019", "/lexuses300h")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Lexus LS460, 2008", "/lexusls460"),
                                                InlineKeyboardButton.WithCallbackData("Lexus GS300, 2005", "/lexusgs300")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                        await botClient.SendMessage(
                                            chat.Id,
                                            $"<b>🚙✨ Самые популярные модели Lexus:</b>",
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                            replyMarkup: lexus);
                                        return;
                                    }
                                    if (message.Text == "/ford")
                                    {
                                        var ford = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Ford Fokus, 2006", "/fordfo"),
                                                InlineKeyboardButton.WithCallbackData("Ford Mondeo, 2006", "/fordmo")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Ford Fiesta, 2015", "/fordfi"),
                                                InlineKeyboardButton.WithCallbackData("Ford Fusion, 2010", "/fordfu")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Ford Mustang, 1999", "/fordmu"),
                                                InlineKeyboardButton.WithCallbackData("Ford Thunderbird, 1989", "/fordthu")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                        await botClient.SendMessage(
                                            chat.Id,
                                            $"<b>🚙✨ Самые популярные модели Ford:</b>",
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                            replyMarkup: ford);
                                        return;
                                    }
                                    if (message.Text == "/mazda")
                                    {
                                        var mazda = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mazda RX-7, 2000", "/mazdarx7"),
                                                InlineKeyboardButton.WithCallbackData("Mazda Mazda6, 2005", "/mazda6")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mazda Atenza, 2008", "/mazdaat"),
                                                InlineKeyboardButton.WithCallbackData("Mazda Axela, 2016", "/mazdaax")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mazda Familia, 2003", "/mazdafa"),
                                                InlineKeyboardButton.WithCallbackData("Mazda Capella, 1998", "/mazdaca")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                        await botClient.SendMessage(
                                            chat.Id,
                                            $"<b>🚙✨ Самые популярные модели Mazda:</b>",
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                            replyMarkup: mazda);
                                        return;
                                    }
                                    if (message.Text == "/audi")
                                    {
                                        var audi = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Audi A4, 2004", "/audia4"),
                                                InlineKeyboardButton.WithCallbackData("Audi A5, 2009", "/audia5")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Audi A6, 2006", "/audia6"),
                                                InlineKeyboardButton.WithCallbackData("Audi V8, 1988", "/audiv8")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Audi A8, 2004", "/audia8"),
                                                InlineKeyboardButton.WithCallbackData("Audi 100, 1991", "/audi100")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                        await botClient.SendMessage(
                                            chat.Id,
                                            $"<b>🚙✨ Самые популярные модели Audi:</b>",
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                            replyMarkup: audi);
                                        return;
                                    }

                                    if (message.Text == "/volkswagen")
                                    {
                                        var volks = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Passat, 1998", "/volkswagenpa"),
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Polo, 2013", "/volkswagenpo")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Golf, 1992", "/volkswagengo"),
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Phaeton, 2008", "/volkswagenpha")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Bora, 2002", "/volkswagenbo"),
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Vento, 1993", "/volkswagenve")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                        await botClient.SendMessage(
                                            chat.Id,
                                            $"<b>🚙✨ Самые популярные модели Volkswagen:</b>",
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                            replyMarkup: volks);
                                        return;
                                    }
                                    if (message.Text == "/honda")
                                    {
                                        var honda = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Honda Accord, 2006", "/hondaacc"),
                                                InlineKeyboardButton.WithCallbackData("Honda Civic, 2006", "/hondaci")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Honda Torneo, 2001", "/hondato"),
                                                InlineKeyboardButton.WithCallbackData("Honda Legend, 2006", "/hondale")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Honda Prelude, 1999", "/hondapre"),
                                                InlineKeyboardButton.WithCallbackData("Honda Inspire, 2001", "/hondains")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                        await botClient.SendMessage(
                                            chat.Id,
                                            $"<b>🚙✨ Самые популярные модели Honda:</b>",
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,

                                            replyMarkup: honda);
                                        return;
                                    }
                                    if (message.Text == "/hyundai")
                                    {
                                        var hyundai = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Hyundai Sonata, 2004", "/hyundaiso"),
                                                InlineKeyboardButton.WithCallbackData("Hyundai Avente, 2005", "/hyundaiav")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Hyundai Accent, 2008", "/hyundaiacc"),
                                                InlineKeyboardButton.WithCallbackData("Hyundai Elantra, 2003", "/hyundaiela")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Hyundai NF, 2006", "/hyundainf"),
                                                InlineKeyboardButton.WithCallbackData("Hyundai Tiburon, 2006", "/hyundaiti")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                        await botClient.SendMessage(
                                            chat.Id,
                                            $"<b>🚙✨ Самые популярные модели Hyundai:</b>",
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                            replyMarkup: hyundai);
                                        return;
                                    }
                                    if (message.Text == "/kia")
                                    {
                                        var kia = new InlineKeyboardMarkup(
                                       new List<InlineKeyboardButton[]>()
                                       {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Kia Spectra, 2006", "/kiaspe"),
                                                InlineKeyboardButton.WithCallbackData("Kia Cerato, 2009", "/kiace")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Kia Magentis, 2004", "/kiama"),
                                                InlineKeyboardButton.WithCallbackData("Kia Optima, 2012", "/kiaopt")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Kia Opirus, 2006", "/kiaopi"),
                                                InlineKeyboardButton.WithCallbackData("Kia Forte, 2010", "/kiafo")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                       });
                                        await botClient.SendMessage(
                                            chat.Id,
                                            $"<b>🚙✨ Самые популярные модели Kia:</b>",
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                            replyMarkup: kia);
                                        return;
                                    }
                                    if (message.Text == "/mitsubishi")
                                    {
                                        var mitsi = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Lancer, 2007", "/mitsubishila"),
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Galant, 2000", "/mitsubishiga")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Mirage, 1998", "/mitsubishimi"),
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Diamante, 1997", "/mitsubishidi")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Eslipse, 2002", "/mitsubishiesl"),
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi FTO, 1996", "/mitsubishifto")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                        await botClient.SendMessage(
                                            chat.Id,
                                            $"<b>🚙✨ Самые популярные модели Mitsubishi:</b>",
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                            replyMarkup: mitsi);
                                        return;
                                    }
                                    
                                    if (waitingForYear.ContainsKey(chatId) && waitingForYear[chatId])
                                    {
                                        if (int.TryParse(message.Text, out int year))
                                        {
                                            waitingForYear[chatId] = false;

                                            var carsByYear = cars.Where(c => c.Value.Year == year).ToList();

                                            if (carsByYear.Any())
                                            {

                                                string response = $"<b>🚗 Автомобили {year} года:</b> 🚗\n" +
                                                    $"              \n" +
                                                    string.Join("\n", carsByYear.Select(c => $"🔹{c.Value.Name} - 💵{c.Value.Price} руб."));

                                                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                                                {
                                                new []
                                            {
                                                    InlineKeyboardButton.WithCallbackData("Ввести другой год выпуска", "/search_year"),
                                                },
                                                new []
                                                    {
                                                    InlineKeyboardButton.WithCallbackData("Вернуться в главное меню", "/start")
                                                    }
                                                    
                                                });

                                                await botClient.SendMessage(chatId, response, replyMarkup: inlineKeyboard, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                                            }
                                            else
                                            {
                                                await botClient.SendMessage(chatId, $"Автомобили {year} года не найдены.");
                                            }
                                            return;

                                        }
                                        else
                                        {
                                            await botClient.SendMessage(chatId, "Пожалуйста, введите корректный год (например, 2015).");
                                        }
                                        return;
                                    }
                                    if (message.Text == "/search_year")
                                    {
                                        waitingForYear[chatId] = true; // Устанавливаем состояние ожидания
                                        await botClient.SendMessage(chatId, "🚗💬 <b>Пожалуйста, укажите год выпуска автомобиля, который вы хотите найти:</b>\r\n📅 Например: <b>2015</b>",
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                                        return; // Выходим, чтобы не обрабатывать другие команды
                                    }
                                    if (message.Text == "/test")
                                    {
                                        await botClient.SendSticker(
                                            chatId: chat.Id,
                                            sticker: "CAACAgIAAxkBAAEBUd1oQHa2mR8xGqy2v2ytZu9w9RcFUgACZVMAAi-6sUoDsaxVZuuAJDYE"
                                            );
                                    }
                                    else
                                    {
                                        await botClient.SendMessage(message.Chat.Id, "Извините, я не понимаю эту команду. Попробуйте /start для списка доступных команд.");
                                    }




                                    //if (message.Text == "/clear")
                                    //{
                                    //    await ClearChat(message.Chat.Id);
                                    //}
                                    //else
                                    //{
                                    //    // Сохраняем ID отправленных сообщений
                                    //    var sentMessage = await botClient.SendMessage(message.Chat.Id, "Ваше сообщение: " + message.Text);
                                    //    sentMessageIds.Add(sentMessage.MessageId);
                                    //}

                                    return;

                                }

                            // Добавил default , чтобы показать вам разницу типов Message
                            default:
                                {
                                    await botClient.SendMessage(message.Chat.Id, "Извините, я не понимаю эту команду. Попробуйте /start для списка доступных команд.");
                                    return;
                                }
                        }

                        return;
                    }

                case UpdateType.CallbackQuery:
                    {
                        // Переменная, которая будет содержать в себе всю информацию о кнопке, которую нажали
                        var callbackQuery = update.CallbackQuery;
                        var message = update.Message;
                        // Аналогично и с Message мы можем получить информацию о чате, о пользователе и т.д.
                        var user = callbackQuery.From;

                        // Выводим на экран нажатие кнопки
                        Console.WriteLine($"{user.FirstName} ({user.Id}) нажал на кнопку: {callbackQuery.Data}");

                        // Вот тут нужно уже быть немножко внимательным и не путаться!
                        // Мы пишем не callbackQuery.Chat , а callbackQuery.Message.Chat , так как
                        // кнопка привязана к сообщению, то мы берем информацию от сообщения.
                        var chat = callbackQuery.Message.Chat;

                        // Добавляем блок switch для проверки кнопок
                        switch (callbackQuery.Data)
                        {
                            // Data - это придуманный нами id кнопки, мы его указывали в параметре
                            // callbackData при создании кнопок. У меня это button1, button2 и button3

                            case "/toyota":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);

                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var newinlineke1 = new InlineKeyboardMarkup(
                                                new List<InlineKeyboardButton[]>()
                                                {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Toyota Mark II (x90), 1993", "/toyotamr"),
                                                InlineKeyboardButton.WithCallbackData("Toyota Chaser (x100), 1999", "/toyotach")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Toyota Cresta JZD100, 2000", "/toyotacr"),
                                                InlineKeyboardButton.WithCallbackData("Toyota Camry 3.5, 2015", "/toyotaca")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Toyota Land Cruiser 4.7, 1985", "/toyotacri"),
                                                InlineKeyboardButton.WithCallbackData("Toyota Land Cruiser Prado 4.0, 2010", "/toyotacrpr")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")
                                            }
                                                });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Самые популярные модели Toyota:</b>\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: newinlineke1);
                                    return;

                                }

                            case "/nissan":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var nissan = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Nissan Silvia, 2002", "/nissansi"),
                                                InlineKeyboardButton.WithCallbackData("Nissan SkyLine, 1998", "/nissansk")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Nissan Laurel, 1999", "/nissanla"),
                                                InlineKeyboardButton.WithCallbackData("Nissan Sunny, 2000", "/nissansu")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Nissan Sentra, 2002", "/nissanse"),
                                                InlineKeyboardButton.WithCallbackData("Nissan GT-R, 2008", "/nissangtr")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Самые популярные модели Nissan:</b>\n",

                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: nissan);
                                    return;
                                }

                            case "/mersedes":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mersedes = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mersedes E-Class, 1995", "/mersedese"),
                                                InlineKeyboardButton.WithCallbackData("Mersedes C-Class, 2003", "/mersedesc")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mersedes G-Class, 1985", "/mersedesg"),
                                                InlineKeyboardButton.WithCallbackData("Mersedes S-Class, 1992", "/mersedess")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mersedes M-Class, 2001", "/mersedesm"),
                                                InlineKeyboardButton.WithCallbackData("Mersedes W123, 1982", "/mersedesw")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Самые популярные модели Mersedes:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mersedes);
                                    return;
                                }
                            case "/bmw":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var bmw = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("BMW M5 E34, 1995", "/bmwe34"),
                                                InlineKeyboardButton.WithCallbackData("BMW M5 E39, 2000", "/bmwe39")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("BMW M5 E60, 2005", "/bmwe60"),
                                                InlineKeyboardButton.WithCallbackData("BMW M5 F10, 2012", "/bmwf10")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("BMW 3-Series, 2011", "/bmw3se"),
                                                InlineKeyboardButton.WithCallbackData("BMW 5-Series, 2001", "/bmw5se")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });


                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Самые популярные модели BMW:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: bmw);
                                    return;
                                }
                            case "/audi":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var audi = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Audi A4, 2004", "/audia4"),
                                                InlineKeyboardButton.WithCallbackData("Audi A5, 2009", "/audia5")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Audi A6, 2006", "/audia6"),
                                                InlineKeyboardButton.WithCallbackData("Audi V8, 1988", "/audiv8")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Audi A8, 2004", "/audia8"),
                                                InlineKeyboardButton.WithCallbackData("Audi 100, 1991", "/audi100")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Самые популярные модели Audi:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: audi);
                                    return;
                                }
                            case "/lexus":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var lexus = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Lexus IS200, 2000", "/lexusis200"),
                                                InlineKeyboardButton.WithCallbackData("Lexus IS300, 2019", "/lexusis300")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Lexus IS250, 2006", "/lexusis250"),
                                                InlineKeyboardButton.WithCallbackData("Lexus ES300h, 2019", "/lexuses300h")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Lexus LS460, 2008", "/lexusls460"),
                                                InlineKeyboardButton.WithCallbackData("Lexus GS300, 2005", "/lexusgs300")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Самые популярные модели Lexus:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lexus);
                                    return;
                                }
                            case "/ford":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var ford = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Ford Fokus, 2006", "/fordfo"),
                                                InlineKeyboardButton.WithCallbackData("Ford Mondeo, 2006", "/fordmo")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Ford Fiesta, 2015", "/fordfi"),
                                                InlineKeyboardButton.WithCallbackData("Ford Fusion, 2010", "/fordfu")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Ford Mustang, 1999", "/fordmu"),
                                                InlineKeyboardButton.WithCallbackData("Ford Thunderbird, 1989", "/fordthu")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Самые популярные модели Ford:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: ford);
                                    return;
                                }
                            case "/mazda":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mazda = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mazda RX-7, 2000", "/mazdarx7"),
                                                InlineKeyboardButton.WithCallbackData("Mazda Mazda6, 2005", "/mazda6")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mazda Atenza, 2008", "/mazdaat"),
                                                InlineKeyboardButton.WithCallbackData("Mazda Axela, 2016", "/mazdaax")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mazda Familia, 2003", "/mazdafa"),
                                                InlineKeyboardButton.WithCallbackData("Mazda Capella, 1998", "/mazdaca")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Самые популярные модели Mazda:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mazda);
                                    return;
                                }
                            case "/lada":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var lada = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Лада 2107, 2010", "/lada07"),
                                                InlineKeyboardButton.WithCallbackData("Лада 2105, 2007", "/lada05")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Лада 2114, 2010", "/lada14"),
                                                InlineKeyboardButton.WithCallbackData("Лада Приора, 2012", "/ladapr")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Лада 2112, 2008", "/lada12"),
                                                InlineKeyboardButton.WithCallbackData("Лада 2109, 2003", "/lada09")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Самые популярные модели Лада:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lada);
                                    return;
                                }
                            case "/volkswagen":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var volks = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Passat, 1998", "/volkswagenpa"),
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Polo, 2013", "/volkswagenpo")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Golf, 1992", "/volkswagengo"),
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Phaeton, 2008", "/volkswagenpha")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Bora, 2002", "/volkswagenbo"),
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Vento, 1993", "/volkswagenve")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Самые популярные модели Volkswagen:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: volks);
                                    return;
                                }
                            case "/honda":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var honda = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Honda Accord, 2006", "/hondaac"),
                                                InlineKeyboardButton.WithCallbackData("Honda Civic, 2006", "/hondaci")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Honda Torneo, 2001", "/hondato"),
                                                InlineKeyboardButton.WithCallbackData("Honda Legend, 2006", "/hondale")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Honda Prelude, 1999", "/hondapre"),
                                                InlineKeyboardButton.WithCallbackData("Honda Inspire, 2001", "/hondains")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Самые популярные модели Honda:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,

                                        replyMarkup: honda);
                                    return;
                                }
                            case "/hyundai":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var hyundai = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Hyundai Sonata, 2004", "/hyundaiso"),
                                                InlineKeyboardButton.WithCallbackData("Hyundai Avente, 2005", "/hyundaiav")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Hyundai Accent, 2008", "/hyundaiac"),
                                                InlineKeyboardButton.WithCallbackData("Hyundai Elantra, 2003", "/hyundaiela")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Hyundai NF, 2006", "/hyundainf"),
                                                InlineKeyboardButton.WithCallbackData("Hyundai Tiburon, 2006", "/hyundaiti")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Самые популярные модели Hyundai:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: hyundai);
                                    return;
                                }
                            case "/kia":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var kia = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Kia Spectra, 2006", "/kiaspe"),
                                                InlineKeyboardButton.WithCallbackData("Kia Cerato, 2009", "/kiace")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Kia Magentis, 2004", "/kiama"),
                                                InlineKeyboardButton.WithCallbackData("Kia Optima, 2012", "/kiaopt")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Kia Opirus, 2006", "/kiaopi"),
                                                InlineKeyboardButton.WithCallbackData("Kia Forte, 2010", "/kiafo")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Самые популярные модели Kia:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: kia);
                                    return;
                                }
                            case "/mitsubishi":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mitsi = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Lancer, 2007", "/mitsubishila"),
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Galant, 2000", "/mitsubishiga")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Mirage, 1998", "/mitsubishimi"),
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Diamante, 1997", "/mitsubishidi")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Eslipse, 2002", "/mitsubishiesl"),
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi FTO, 1996", "/mitsubishifto")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/autocatalog"),
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Самые популярные модели Mitsubishi:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mitsi);
                                    return;
                                }
                            case "/toyotamr":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var toyotamr = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/toyota"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }


                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://avatars.mds.yandex.net/get-autoru-vos/5859826/ab7c663b2b55686c5ed842611d86f584/1200x900", // Замените на действительный URL
                                        "🚗<b>Toyota Mark II (X90)</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 800.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.0 / 2.5 л\r\n<b>Мощность:</b> 140 – 280 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> правый\r\n<b>Модель двигателя:</b> 1JZ-GTE\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 8,5 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1725 кг\r\n🏁 <b>Максимальная скорость:</b> 180 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: toyotamr);
                                    return;
                                }
                            case "/toyotach":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var toyotach = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/toyota"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/UV4tpGG7_51aTIQV4hDXyy8fLSh1zqSmH6U57yX_SIVxXsR6QxOA18uWGUiyBW_3uZ1TqDWchdJq4maC/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Toyota Chaser (X100), 1999</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 900.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.0 / 2.5 л\r\n<b>Мощность:</b> 150 – 280 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> правый\r\n<b>Модель двигателя:</b> 1JZ-GTE\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 65 л\r\n⛽️ <b>Расход топлива (средний):</b> 9,0 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1700 кг\r\n🏁 <b>Максимальная скорость:</b> 190 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: toyotach);
                                    return;
                                }
                            case "/toyotacr":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var toyotacr = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/toyota"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/1kzw1Ru4YGfZe5Ud3r-PoxX3e0hAtCcfaSZp8hTJSM-uRlA1w63gyuXI8zRj6da6ggWbTKm2P3rGSAmu/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Toyota Cresta JZD100, 2000</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 850.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.5 л\r\n<b>Мощность:</b> 200 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> правый\r\n<b>Модель двигателя:</b> 1JZ-GTE\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 9,0 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1750 кг\r\n🏁 <b>Максимальная скорость:</b> 195 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: toyotacr);
                                    return;
                                }
                            case "/toyotaca":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var toyotaca = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/toyota"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/AnuJHxP9sYRJlXlqQBsZbdp2ZOaDlj3loue7cJLxcUNcslTW22nm8GACVUUjSDUGMyFXxYdl88eovMbV/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Toyota Camry 3.5, 2015</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 1.500.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 3.5 л\r\n<b>Мощность:</b> 249 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n<b>Модель двигателя:</b> 2GR-FE\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 9,5 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1650 кг\r\n🏁 <b>Максимальная скорость:</b> 210 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: toyotaca);
                                    return;
                                }
                            case "/toyotacri":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var toyotacri = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/toyota"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/OHA7WXO1TqOey-SYMrpBFGa9H63ircpGLGZJetcINXCgWliMR3TbNlyj8NFuHm9LEaYAiFu9pZbhCG0H/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Toyota Land Cruiser 4.7, 1985</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 1.200.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 4.7 л\r\n<b>Мощность:</b> 210 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> полный\r\n<b>Тип кузова:</b> внедорожник\r\n<b>Руль:</b> правый\r\n<b>Модель двигателя:</b> 2UZ-FE\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 95 л\r\n⛽️ <b>Расход топлива (средний):</b> 15 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 2600 кг\r\n🏁 <b>Максимальная скорость:</b> 175 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: toyotacri);
                                    return;
                                }
                            case "/toyotacrpr":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var toyotacrpr = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/toyota"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/DxMbee_Oh1Oiuq4B5q7yvNKbjFM3p43p7hf10Xr2UJngst-Y28lASTfidAEUUZ6AsLOZimrJyUtjc0Op/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Toyota Land Cruiser Prado 4.0, 2010</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 1.800.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 4.0 л\r\n<b>Мощность:</b> 240 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> полный\r\n<b>Тип кузова:</b> внедорожник\r\n<b>Руль:</b> правый\r\n<b>Модель двигателя:</b> 1GR-FE\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 87 л\r\n⛽️ <b>Расход топлива (средний):</b> 12 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 2300 кг\r\n🏁 <b>Максимальная скорость:</b> 185 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: toyotacrpr);
                                    return;
                                }
                            case "/nissansi":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var nissansi = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/nissan"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/tdd9FemxcURgoivEVzLH_qXBeswVjwpC18NRXJGPZo9MpEHW3bdYzZrf3lns-L9xEeq0bn-RluET/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Nissan Silvia S15, 2002</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 1.200.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.0 л\r\n<b>Мощность:</b> 250 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> купе\r\n<b>Руль:</b> правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 50 л\r\n⛽️ <b>Расход топлива (средний):</b> 10 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1300 кг\r\n🏁 <b>Максимальная скорость:</b> 240 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: nissansi);
                                    return;
                                }
                            case "/nissansk":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var nissansk = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/nissan"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/JZ5PEnczsFEnwFH64Lzerbko19h4T623-h34brf4EdpQm4wa2H7Ue1wATaiZVpWEk3HmT5-097L_uqMn/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Nissan Skyline R34, 1998</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 2.500.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.6 л\r\n<b>Мощность:</b> 280 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> полный\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 65 л\r\n⛽️ <b>Расход топлива (средний):</b> 12 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1500 кг\r\n🏁 <b>Максимальная скорость:</b> 250 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: nissansk);
                                    return;
                                }
                            case "/nissanla":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var nissanla = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/nissan"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/XqOKXUlW0WqCjT5mmdIdIz4NlcqX6pOGEgcap56civPGj_RUbDvqUQRGuntwcrw9wgJcAg275izAqTSq/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Nissan Laurel C34, 1999</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 800.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.0 л\r\n<b>Мощность:</b> 155 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 9 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1450 кг\r\n🏁 <b>Максимальная скорость:</b> 210 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: nissanla);
                                    return;
                                }
                            case "/nissansu":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var nissansu = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/nissan"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/WGAPWIMT8NZtw7cE2msXaM44YqywblvYKymfu_oiAdG5LFO69hGgnjksnAJSXXiHIIZCHu50TyqdPylY/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Nissan Sunny B15, 2000</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 500.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.6 л\r\n<b>Мощность:</b> 110 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92\r\n🛢 <b>Объём топливного бака:</b> 50 л\r\n⛽️ <b>Расход топлива (средний):</b> 7 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1200 кг\r\n🏁 <b>Максимальная скорость:</b> 180 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: nissansu);
                                    return;
                                }
                            case "/nissanse":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var nissanse = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/nissan"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/OV4eONz7NXF4YM_EQpj29wMr3y2hnyGdk7_A6iHIp_cJiqysTJF90uPFTwFgy4PsaEXRno2K5U6d3jeg/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Nissan Sentra B15, 2002</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 600.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.8 л\r\n<b>Мощность:</b> 126 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92\r\n🛢 <b>Объём топливного бака:</b> 50 л\r\n⛽️ <b>Расход топлива (средний):</b> 8 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1300 кг\r\n🏁 <b>Максимальная скорость:</b> 190 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: nissanse);
                                    return;
                                }
                            case "/nissangtr":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var nissangtr = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/nissan"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/RXmLlcA9uvABkwjZP1L4iodNAGCySovJu9dt2BAQyCPZkzbB8OuXVsVeNG4Di7w-kOhn6gTqPvVa/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Nissan GT-R R35, 2008</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 6.000.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 3.8 л\r\n<b>Мощность:</b> 480 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> полный\r\n<b>Тип кузова:</b> купе\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-98\r\n🛢 <b>Объём топливного бака:</b> 65 л\r\n⛽️ <b>Расход топлива (средний):</b> 11 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1740 кг\r\n🏁 <b>Максимальная скорость:</b> 315 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: nissangtr);
                                    return;
                                }
                            case "/mersedese":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mersedese = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mersedes"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/6MCcDvNANVDtC4ufBdk1d5PTpmI4UgNhncv_gADZvRac3gbo8FJXgC9_XMulJEqxdHAMSbPqc49JB9at/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mercedes E-Class W210, 1995</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 800.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 2.0 / 2.5 л\r\n<b>Мощность:</b> 136 – 193 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> задний / полный\r\n<b>Тип кузова:</b> седан / универсал\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95 / ДТ\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 9 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1700 кг\r\n🏁 <b>Максимальная скорость:</b> 210 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mersedese);
                                    return;
                                }
                            case "/mersedesc":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mersedesc = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mersedes"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/zxASo_ypIvYKQOdd30P4n63Hk7md0gbOEXV6eSvFx9uzUcs_Tncnmj9QXn_G0zb-huVV2Q05AsimNyek/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mercedes C-Class W203, 2003</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 600.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 1.8 / 2.2 л\r\n<b>Мощность:</b> 143 – 204 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> задний / полный\r\n<b>Тип кузова:</b> седан / универсал\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95 / ДТ\r\n🛢 <b>Объём топливного бака:</b> 60 л\r\n⛽️ <b>Расход топлива (средний):</b> 8 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1500 кг\r\n🏁 <b>Максимальная скорость:</b> 220 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mersedesc);
                                    return;
                                }
                            case "/mersedesg":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mersedesg = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mersedes"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/JZI4wzOugdbimszLpYSmb8Hk4eGMBFmSIwdMCOqfRaRA912tTqcfEgV_BZVfJ-f6hatG3P4GPJRje3JJ/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mercedes G-Class W460, 1985</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 1.500.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 2.3 / 2.9 л\r\n<b>Мощность:</b> 95 – 200 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> полный\r\n<b>Тип кузова:</b> внедорожник\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95 / ДТ\r\n🛢 <b>Объём топливного бака:</b> 90 л\r\n⛽️ <b>Расход топлива (средний):</b> 12 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 2500 кг\r\n🏁 <b>Максимальная скорость:</b> 150 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mersedesg);
                                    return;
                                }
                            case "/mersedess":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mersedess = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mersedes"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/TcX8iao7Vc6ftX47C5IP89fC3YxqjHaM7xQB9_eQOi7qqGppX8G6ATtaJB3tk59NQKdBGe8ip7XaNdvz/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mercedes S-Class W140, 1992</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 1.200.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 3.2 / 5.0 л\r\n<b>Мощность:</b> 220 – 400 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 100 л\r\n⛽️ <b>Расход топлива (средний):</b> 12 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 2000 кг\r\n🏁 <b>Максимальная скорость:</b> 250 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mersedess);
                                    return;
                                }
                            case "/mersedesm":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mersedesm = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mersedes"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/B5P0RXIj8Z9Cu6dmDAjWJngMq5romliQ6HIZ9LTeBMYVl6R-S4_1BW23RQjj38Le6FgPgNrMiemhAvtQ/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mercedes M-Class W163, 2001</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 800.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 3.2 / 2.7 л\r\n<b>Мощность:</b> 215 – 184 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> полный\r\n<b>Тип кузова:</b> внедорожник\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95 / ДТ\r\n🛢 <b>Объём топливного бака:</b> 75 л\r\n⛽️ <b>Расход топлива (средний):</b> 10 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 2200 кг\r\n🏁 <b>Максимальная скорость:</b> 200 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mersedesm);
                                    return;
                                }
                            case "/mersedesw":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mersedesw = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mersedes"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/aOcdUe3W09qw3rBPbV54ciNIMeDMhufhynE_EGCHU6iuIiQoZd1-FK_3ffC6pRQdHB_4RcSIe1eAou5D/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mercedes W123, 1982</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 500.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 2.0 / 2.4 л\r\n<b>Мощность:</b> 90 – 136 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> седан / универсал\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92 / ДТ\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 8 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1600 кг\r\n🏁 <b>Максимальная скорость:</b> 180 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mersedesw);
                                    return;
                                }
                            case "/bmwe34":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var bmwe34 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/bmw"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://avatars.mds.yandex.net/i?id=dede398a8a314290b54ecc8dc52e525fd450aa9d-5205249-images-thumbs&n=13", // Замените на действительный URL
                                        "🚗<b>BMW M5 E34, 1995</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 1.200.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 3.8 л\r\n<b>Мощность:</b> 340 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> седан / универсал\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 12 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1750 кг\r\n🏁 <b>Максимальная скорость:</b> 250 км/ч (ограничена)\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: bmwe34);
                                    return;
                                }
                            case "/bmwe39":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var bmwe39 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/bmw"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/Gmb5hVXklp3vfKtD3FCjK-1L6KQ4yurHyOK1HnkYzX3jfBVF7pzApED1NRHq58qczxN0DKQzi5J_KiZ8/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>BMW M5 E39, 2000</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 2.000.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 4.9 л V8\r\n<b>Мощность:</b> 400 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 14 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1800 кг\r\n🏁 <b>Максимальная скорость:</b> 250 км/ч (ограничена)\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: bmwe39);
                                    return;
                                }
                            case "/bmwe60":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var bmwe60 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/bmw"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/j3Ijf0YFdEFrorc9XG0Lezz55MDmwRf3ZJ5JDBWYU8rYs7HqTGgbzVb8-J-gaeyfkTc90_BONDxxquXU/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>BMW M5 E60, 2005</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 2.800.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 5.0 л V10\r\n<b>Мощность:</b> 507 л.с.\r\n<b>Коробка передач:</b> механическая / автомат (SMG)\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-98\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 14 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1850 кг\r\n🏁 <b>Максимальная скорость:</b> 250 км/ч (ограничена)",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: bmwe60);
                                    return;
                                }
                            case "/bmwf10":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var bmwf10 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/bmw"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/81tZe9tb6ystpyDv6Y3HmiLERWePeEftQ1TGHv8yHkaJhoAOPewCb0m8RLd5M0meOuf0gwKpXfAGpGAt/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>BMW M5 F10, 2012</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 4.500.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 4.4 л V8 с турбонаддувом\r\n<b>Мощность:</b> 560 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-98\r\n🛢 <b>Объём топливного бака:</b> 68 л\r\n⛽️ <b>Расход топлива (средний):</b> 11 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1900 кг\r\n🏁 <b>Максимальная скорость:</b> 250 км/ч (ограничена)\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: bmwf10);
                                    return;
                                }
                            case "/bmw3se":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var bmw3se = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/bmw"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/6Utx0Muo8JSqzFcCB7n2xMDMXblIBq3M0WyGrlYcSEheX5kS3cIltMRKOcfusX2wbqfEijYIec_7xFJn/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>BMW 3-Series E90, 2011</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 1.200.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 2.0 – 3.0 л\r\n<b>Мощность:</b> 143 – 306 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> задний / полный\r\n<b>Тип кузова:</b> седан / универсал / купе\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95 / ДТ\r\n🛢 <b>Объём топливного бака:</b> 60 л\r\n⛽️ <b>Расход топлива (средний):</b> 6 – 9 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1600 – 1750 кг\r\n🏁 <b>Максимальная скорость:</b> 230 – 250 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: bmw3se);
                                    return;
                                }
                            case "/bmw5se":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var bmw5se = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/bmw"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/ZF6j9gtfL9a381FDlTd5gJLoZMcQLtgC--xx6PU_u1z9k2SqhisPkjL-2VG5hgTjGKdkREbmsb1prBb5/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>BMW 5-Series E39, 2001</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 1.000.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 2.0 – 4.4 л\r\n<b>Мощность:</b> 150 – 400 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> задний / полный\r\n<b>Тип кузова:</b> седан / универсал\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95 / ДТ\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 8 – 12 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1700 – 1900 кг\r\n🏁 <b>Максимальная скорость:</b> 210 – 250 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: bmw5se);
                                    return;
                                }
                            case "/lada07":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var lada07 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/lada"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/kvDioFBviBVZ0Lx94o8UzLN7ARNzx8eydLClGLHQSGuFpSYP20DBv6SR_JVJyVn_f1Yf4a1w_MyzpzVS/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Лада 2107, 2010</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 300.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.6 л\r\n<b>Мощность:</b> 80 л.с.\r\n<b>Коробка передач:</b> механическая\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92\r\n🛢 <b>Объём топливного бака:</b> 40 л\r\n⛽️ <b>Расход топлива (средний):</b> 9 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1300 кг\r\n🏁 <b>Максимальная скорость:</b> 150 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lada07);
                                    return;
                                }
                            case "/lada05":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var lada05 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/lada"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/isg5yIEsoJV3VACIj0XQHS-He8G4Q23xMIsju-mvgATG1k0jkIOqVRMv6bqeDILDO9EqRFrVorVSBBtC/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Лада 2105, 2007</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 250.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.5 л\r\n<b>Мощность:</b> 75 л.с.\r\n<b>Коробка передач:</b> механическая\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92\r\n🛢 <b>Объём топливного бака:</b> 40 л\r\n⛽️ <b>Расход топлива (средний):</b> 8 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1200 кг\r\n🏁 <b>Максимальная скорость:</b> 140 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lada05);
                                    return;
                                }
                            case "/lada14":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var lada14 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/lada"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/VfQrizrzneUngw86bnFQZvzrTu6uHT7XgKWVfyCOeAof8gOOgXCYrMAKKC52z241qqeMUwbfSan4PKuq/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Лада 2114, 2010</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 350.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.6 л\r\n<b>Мощность:</b> 90 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> хэтчбек\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92\r\n🛢 <b>Объём топливного бака:</b> 45 л\r\n⛽️ <b>Расход топлива (средний):</b> 7 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1300 кг\r\n🏁 <b>Максимальная скорость:</b> 160 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lada14);
                                    return;
                                }
                            case "/ladapr":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var ladapr = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/lada"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/O_smO-TdGKtsxAPoMl4nSTjl-c4uyejeqGAUjTLxmMAcmwKKepjMHcqiKbX1--8Wpn5thVdn0819EcYC/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Лада Приора, 2012</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 400.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.6 л\r\n<b>Мощность:</b> 98 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан / хэтчбек / универсал\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92\r\n🛢 <b>Объём топливного бака:</b> 50 л\r\n⛽️ <b>Расход топлива (средний):</b> 8 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1400 кг\r\n🏁 <b>Максимальная скорость:</b> 180 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: ladapr);
                                    return;
                                }
                            case "/lada12":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var lada12 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/lada"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/j2pOtY3yl82ke_Lh8chsk9Ew3nF73sXLjkkPCBkTpXDOW8jH0Y_0eCO7TKooBzvBrIwwYiv8kuHTYNau/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Лада 2112, 2008</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 300.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.6 л\r\n<b>Мощность:</b> 90 л.с.\r\n<b>Коробка передач:</b> механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> хэтчбек\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92\r\n🛢 <b>Объём топливного бака:</b> 45 л\r\n⛽️ <b>Расход топлива (средний):</b> 7.5 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1300 кг\r\n🏁 <b>Максимальная скорость:</b> 165 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lada12);
                                    return;
                                }
                            case "/lada09":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var lada09 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/lada"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/FsWmXrYk5Sk-uGkdh0NcKRrAIqrrgXvJXLDkLt1FkBCG_v5PHMu1q0AGbuzfnIaD3sEsjUmCH6qb1FZd/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Лада 2109, 2003</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 150.000 – 200.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.3 – 1.5 л\r\n<b>Мощность:</b> 70 – 78 л.с.\r\n<b>Коробка передач:</b> механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> хэтчбек\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92\r\n🛢 <b>Объём топливного бака:</b> 43 л\r\n⛽️ <b>Расход топлива (средний):</b> 7 – 8 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1200 кг\r\n🏁 <b>Максимальная скорость:</b> до 160 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lada09);
                                    return;

                                }
                            case "/lexusis200":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var lexusis200 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/lexus"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/bx0_9aGoNGSg4Nwjl8pRFwkZpAsZ1LUTbqZajO1bXPKyPrNrpvQPx1lcYl3ltLnfJGJztXSqtZAKsFNz/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Lexus IS200, 2000</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 600.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.0 л\r\n<b>Мощность:</b> 155 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 60 л\r\n⛽️ <b>Расход топлива (средний):</b> 10 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1500 кг\r\n🏁 <b>Максимальная скорость:</b> 210 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lexusis200);
                                    return;

                                }
                            case "/lexusis300":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var lexusis300 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/lexus"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/pdjRXCX5LccUN9XHuKHdn57Vsav4I41u2OXz5-vUcUxVIEcPjxfgud3q269Bbo2ZBk8SHrgS7FPp448T/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Lexus IS300, 2019</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 3.000.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 3.5 л\r\n<b>Мощность:</b> 260 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> задний / полный\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 66 л\r\n⛽️ <b>Расход топлива (средний):</b> 8.5 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1800 кг\r\n🏁 <b>Максимальная скорость:</b> 230 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lexusis300);
                                    return;

                                }
                            case "/lexusis250":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var lexusis250 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/lexus"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/PPCzu4Ttl9gVwN5ss1QsC0EHPnnSXcSledBCntdEaBqK6yvtUr9yKdqfD-BWQ0XPqUVITlcI_myZ23Zz/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Lexus IS250, 2006</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 1.300.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.5 л\r\n<b>Мощность:</b> 204 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> задний / полный\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 60 л\r\n⛽️ <b>Расход топлива (средний):</b> 9.5 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1600 кг\r\n🏁 <b>Максимальная скорость:</b> 220 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lexusis250);
                                    return;

                                }
                            case "/lexuses300h":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var lexuses300h = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/lexus"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/0JFbTf3q_3bI483jz3JqhKvx4HoxCrNFjEwzX01n8DJUfJyD4VtU_d6XvMsXMqa2iT-GPpRWv7no13Sl/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Lexus ES300h, 2019</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 3.500.000 ₽\r\n\r\n<b>Двигатель:</b> гибрид, 2.5 л\r\n<b>Мощность:</b> 215 л.с. (суммарно)\r\n<b>Коробка передач:</b> вариатор\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 50 л\r\n⛽️ <b>Расход топлива (средний):</b> 5.5 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1800 кг\r\n🏁 <b>Максимальная скорость:</b> 180 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lexuses300h);
                                    return;

                                }
                            case "/lexusls460":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var lexusls460 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/lexus"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/hs0NvLI1jZDLKD-DNY_IXG6OzL2Ao7tzYbaH-BtOJQvP88DWsfok975TYtaf6yy0bTHaO3LwkkM1RJS8/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Lexus LS460, 2008</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 2.500.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 4.6 л\r\n<b>Мощность:</b> 380 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> полный\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 82 л\r\n⛽️ <b>Расход топлива (средний):</b> 12 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 2300 кг\r\n🏁 <b>Максимальная скорость:</b> 250 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lexusls460);
                                    return;

                                }
                            case "/lexusgs300":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var lexusgs300 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/lexus"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/KtTmAmlcf-UDXC7Uh4y2Qi0Mrv_qz9TQShJYsxuQTR0N0E2HCG_JFfIvdzuOQWRQ1d4c7txOf90QhtyV/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Lexus GS300, 2005</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 1.500.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 3.0 л\r\n<b>Мощность:</b> 245 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> задний / полный\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 10.5 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1800 кг\r\n🏁 <b>Максимальная скорость:</b> 230 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lexusgs300);
                                    return;

                                }
                            case "/fordfo":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var fordfo = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/ford"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/qImNpUFXJ0y1ygDkQdxMhNwXif8_JW-N-eKHAAI7QezUXC52vAIogb_gyh5gjB9AqgK5vxkdPKDp1lE2/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Ford Focus, 2006</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 400.000 – 600.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.6 – 2.0 л\r\n<b>Мощность:</b> 100 – 145 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан / хэтчбек / универсал\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 55 л\r\n⛽️ <b>Расход топлива (средний):</b> 7 – 9 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1600 кг\r\n🏁 <b>Максимальная скорость:</b> до 200 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: fordfo);
                                    return;

                                }
                            case "/fordmo":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var fordmo = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/ford"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/DDFs2GOwuS8u4dRNRoYfCemr-eiM8LC8AumQ3akHtxWrWMHfTVvMwxhDyuITVleJAsM2hbCd-Y4iioAr/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Ford Mondeo, 2006</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 500.000 – 700.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.0 – 2.5 л\r\n<b>Мощность:</b> 130 – 170 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> передний / полный\r\n<b>Тип кузова:</b> седан / универсал\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 62 л\r\n⛽️ <b>Расход топлива (средний):</b> 8 – 10 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1800 кг\r\n🏁 <b>Максимальная скорость:</b> до 210 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: fordmo);
                                    return;

                                }
                            case "/fordfi":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var fordfi = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/ford"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/SoQRKSJ0R7tykeWGzgS7PmOPgTHW8qba_TPB4PWfgmZvKOTfjjZZpmKWdWVi2FrZHZuP33vDrJI3IZ0w/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Ford Fiesta, 2015</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 700.000 – 900.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.0 – 1.6 л\r\n<b>Мощность:</b> 85 – 120 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> хэтчбек / седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 42 л\r\n⛽️ <b>Расход топлива (средний):</b> 5 – 7 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1200 кг\r\n🏁 <b>Максимальная скорость:</b> до 185 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: fordfi);
                                    return;

                                }
                            case "/fordfu":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var fordfu = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/ford"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/AhybMdJfk_GnJz8MjcOoP0YbsH3S8KR49gxIU8ZrL5-Bjo-T1R_ICRzqhg8grgnIBe2OuIMYmvjpk_3l/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Ford Fusion, 2010</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 500.000 – 700.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.4 – 2.5 л\r\n<b>Мощность:</b> 90 – 170 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан / хэтчбек\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 53 л\r\n⛽️ <b>Расход топлива (средний):</b> 6 – 9 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1400 кг\r\n🏁 <b>Максимальная скорость:</b> до 200 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: fordfu);
                                    return;

                                }
                            case "/fordmu":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var fordmu = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/ford"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/pVN4oTbVF7FVy_AoPOMNqtAZn3rJZxtVE1O_bjxiknDkWctIL7GCtXXU3H7De-C4PosM_qj6UDJj/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Ford Mustang, 1999</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 1.200.000 – 1.800.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 4.6 л V8\r\n<b>Мощность:</b> 225 – 305 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> купе / кабриолет\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 61 л\r\n⛽️ <b>Расход топлива (средний):</b> 13 – 15 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1700 кг\r\n🏁 <b>Максимальная скорость:</b> до 250 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: fordmu);
                                    return;

                                }
                            case "/fordthu":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var fordthu = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/ford"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/mswyb_Ynep1OcbBUW21kuUQqK32cJm2SiYutVH0P3emqjzyYTLV-SmsQaCJYgXDb0GsoK1PQZMQbyBvt/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Ford Thunderbird, 1989</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 1.000.000 – 1.500.000 ₽ (зависит от состояния и комплектации)\r\n\r\n<b>Двигатель:</b> бензин, 3.8 – 5.0 л V6 / V8\r\n<b>Мощность:</b> 140 – 220 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> купе / кабриолет\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> около 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 12 – 16 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1800 кг\r\n🏁 <b>Максимальная скорость:</b> до 210 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: fordthu);
                                    return;


                                }
                            case "/mazdarx7":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mazdarx7 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mazda"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/K6hQt72YRe_uTIyFlDNPqanFZWk4j8KIk9X2NYbxPVULvLFTSUKfYl4bfdno10k6SmPocwaW8XRyJcWx/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mazda RX-7, 2000</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 1.200.000 – 1.800.000 ₽\r\n\r\n<b>Двигатель:</b> роторный, 1.3 л\r\n<b>Мощность:</b> 280 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> купе\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-98\r\n🛢 <b>Объём топливного бака:</b> 50 л\r\n⛽️ <b>Расход топлива (средний):</b> 10 – 12 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1300 кг\r\n🏁 <b>Максимальная скорость:</b> до 250 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mazdarx7);
                                    return;

                                }
                            case "/mazda6":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mazda6 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mazda"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/Il7mCKU1h9_EEIa1LNwMlPRYAaGqyXNETaeuEciXZhTtUe19VAxYgIDzbUfNBq0Zr1eKdIAk4HzXEhK4/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mazda Mazda6, 2005</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 500.000 – 700.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.0 – 2.3 л\r\n<b>Мощность:</b> 145 – 190 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> передний / полный\r\n<b>Тип кузова:</b> седан / универсал\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 64 л\r\n⛽️ <b>Расход топлива (средний):</b> 8 – 10 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1600 кг\r\n🏁 <b>Максимальная скорость:</b> до 210 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mazda6);
                                    return;

                                }
                            case "/mazdaat":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mazdaat = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mazda"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/UbcqPR00ps_eUTIXV_MmqEwyDW7GkJjzRJFHaOiZaOEeE5MOCAkIlwSwymmfDjS-Zo3ps6QlgMn2AW9j/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mazda Atenza, 2008</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 600.000 – 800.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.0 – 2.5 л\r\n<b>Мощность:</b> 147 – 170 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> передний / полный\r\n<b>Тип кузова:</b> седан / универсал\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 64 л\r\n⛽️ <b>Расход топлива (средний):</b> 7 – 9 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1600 кг\r\n🏁 <b>Максимальная скорость:</b> до 210 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mazdaat);
                                    return;

                                }
                            case "/mazdaax":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mazdaax = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mazda"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/Llrco2JSqpX-NPK8s0Zo66vUsCCWUQHeC1HdQPsNjUSc2_hP1GDUBJOOhOQoqScf6uYj0wRCyuBKzQnH/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mazda Axela, 2016</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 900.000 – 1.200.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.5 – 2.0 л\r\n<b>Мощность:</b> 105 – 165 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> хэтчбек / седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 51 л\r\n⛽️ <b>Расход топлива (средний):</b> 5 – 7 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1300 кг\r\n🏁 <b>Максимальная скорость:</b> до 200 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mazdaax);
                                    return;

                                }
                            case "/mazdafa":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mazdafa = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mazda"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/Z8JurZXZ7_YPTMT6rROtGjsmp4IvD1pe9TKILZk424eTa6gci7HMKTeQiA1Ej5VvxLNQWQp31jEDdlVE/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mazda Familia, 2003</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 300.000 – 500.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.3 – 1.6 л\r\n<b>Мощность:</b> 75 – 110 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан / хэтчбек / универсал\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92/95\r\n🛢 <b>Объём топливного бака:</b> 50 л\r\n⛽️ <b>Расход топлива (средний):</b> 6 – 8 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1200 кг\r\n🏁 <b>Максимальная скорость:</b> до 180 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mazdafa);
                                    return;

                                }
                            case "/mazdaca":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mazdaca = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mazda"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/zd92KihgI3JrjR9V7bZb8eQ1bo2ZFQBEEZLTL3zFPAciVyZ8LCnKIqi8-HZgB8ucL85Wyj9Ag8iKgFj_/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mazda Capella, 1998</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 200.000 – 400.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.8 – 2.0 л\r\n<b>Мощность:</b> 110 – 140 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> передний / полный\r\n<b>Тип кузова:</b> седан / универсал\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92/95\r\n🛢 <b>Объём топливного бака:</b> 60 л\r\n⛽️ <b>Расход топлива (средний):</b> 7 – 9 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1400 кг\r\n🏁 <b>Максимальная скорость:</b> до 200 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mazdaca);
                                    return;

                                }
                            case "/audia4":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var audia4 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/audi"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/I2DgsnsEoTElbSHTeoSU6Wj44EFl9IlnSR1kXWRGz3tJpfJWuxVhT3UfCpw52JioYYfxsrcZ7JP5eeCA/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Audi A4, 2004</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 400.000 – 600.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 1.8 – 3.0 л\r\n<b>Мощность:</b> 150 – 250 л.с.\r\n<b>Коробка передач:</b> механическая / автомат (вариатор)\r\n<b>Привод:</b> передний / полный (quattro)\r\n<b>Тип кузова:</b> седан / универсал\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95 / дизель\r\n🛢 <b>Объём топливного бака:</b> 65 л\r\n⛽️ <b>Расход топлива (средний):</b> 7 – 10 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1600 кг\r\n🏁 <b>Максимальная скорость:</b> до 240 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: audia4);
                                    return;

                                }
                            case "/audia5":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var audia5 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/audi"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/gLP1m8uK4vNLwrsDUUVBW7AgjIBOojbpnE_5xoBRKcpAOv39lU1Z--2_ViA9sE_4W0JOnFO1QTs0HaRR/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Audi A5, 2009</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 800.000 – 1.200.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 1.8 – 3.2 л\r\n<b>Мощность:</b> 170 – 265 л.с.\r\n<b>Коробка передач:</b> механическая / автомат (S-tronic)\r\n<b>Привод:</b> передний / полный (quattro)\r\n<b>Тип кузова:</b> купе / кабриолет / спортбек\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95 / дизель\r\n🛢 <b>Объём топливного бака:</b> 64 л\r\n⛽️ <b>Расход топлива (средний):</b> 7 – 10 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1700 кг\r\n🏁 <b>Максимальная скорость:</b> до 250 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: audia5);
                                    return;

                                }
                            case "/audia6":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var audia6 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/audi"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/wIdcgtlNXVA_iuzWF3LQG9ZMsh-3Jj5OiJ27pFzzGZ_rA_5YlQZVvUqlp22C8WvM2UNtA--gDbnN0WLQ/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Audi A6, 2006</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 600.000 – 900.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 2.0 – 3.2 л\r\n<b>Мощность:</b> 165 – 255 л.с.\r\n<b>Коробка передач:</b> механическая / автомат (Tiptronic)\r\n<b>Привод:</b> передний / полный (quattro)\r\n<b>Тип кузова:</b> седан / универсал\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95 / дизель\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 7 – 11 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1800 кг\r\n🏁 <b>Максимальная скорость:</b> до 240 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: audia6);
                                    return;

                                }
                            case "/audiv8":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var audiv8 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/audi"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/kAgmPfIbXlPmYOJkfroXQRwDdWkXZfUsxX1hlWocHn0-svBvM7Oh6JQwvXhpgMZPgRDsWVwrnFCrFirQ/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Audi V8, 1988</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 700.000 – 1.200.000 ₽ (зависит от состояния)\r\n\r\n<b>Двигатель:</b> бензин, 3.6 – 4.2 л V8\r\n<b>Мощность:</b> 280 – 290 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> задний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 80 л\r\n⛽️ <b>Расход топлива (средний):</b> 15 – 18 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1800 кг\r\n🏁 <b>Максимальная скорость:</b> до 240 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: audiv8);
                                    return;

                                }
                            case "/audia8":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var audia8 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/audi"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/YZL03S-me-0nSGovlWv1R74pozVsvL__C1dxHYQ3811H2-Cw8WKRMhwAW5lW9USN23ky5ywMDQxveYsq/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Audi A8, 2004</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 900.000 – 1.500.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 3.0 – 4.2 л\r\n<b>Мощность:</b> 220 – 335 л.с.\r\n<b>Коробка передач:</b> автомат (Tiptronic)\r\n<b>Привод:</b> полный (quattro)\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95 / дизель\r\n🛢 <b>Объём топливного бака:</b> 90 л\r\n⛽️ <b>Расход топлива (средний):</b> 10 – 14 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 2100 кг\r\n🏁 <b>Максимальная скорость:</b> до 250 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: audia8);
                                    return;

                                }
                            case "/audi100":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var audi100 = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/audi"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/CjdWh82qxqR5gPytV3b9D32mqnYWYC5gb1TMlcgR4EzFi0xcLdXGVI3GCsNH8SoKFWKiX54gbtmTotBy/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Audi 100, 1991</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 150.000 – 300.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 2.0 – 2.8 л\r\n<b>Мощность:</b> 115 – 170 л.с.\r\n<b>Коробка передач:</b> механическая / автомат\r\n<b>Привод:</b> передний / полный (quattro)\r\n<b>Тип кузова:</b> седан / универсал\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92 / дизель\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 8 – 12 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1600 кг\r\n🏁 <b>Максимальная скорость:</b> до 210 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: audi100);
                                    return;

                                }
                            case "/volkswagenpa":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var volkswagenpa = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/volkswagen"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/UYgR5jIOb48orjVT2QLLXr2GDNnIzDfIWZ1-JrGoKuMHmZtz6dXfVSs449LBQPIpovVSzLs-7nyycd5m/gen1200.jpg", // Замените на действительный URL
                                        "🚗 <b>Volkswagen Passat (1998)</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 400.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 1.6 / 2.0 / 1.9 TDI л\r\n<b>Мощность:</b> 90 – 150 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний / полный\r\n<b>Тип кузова:</b> седан / универсал\r\n<b>Руль:</b> левый\r\n<b>Модель двигателя:</b> 1.8T / 1.9 TDI\r\n\r\n⛽️ <b>Топливо:</b> АИ-92 / ДТ\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 7,5 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1500 кг\r\n🏁 <b>Максимальная скорость:</b> 200 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: volkswagenpa);
                                    return;

                                }
                            case "/volkswagenpo":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var volkswagenpo = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/volkswagen"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/GlC2WE3Q5PxNNHPPcwk2RY9MgdMozEmg4TVO9i5vv3DMwuX7vbYlrIUjZomEPLkt6kCc8MCBuZo1gWFN/gen1200.jpg", // Замените на действительный URL
                                        "🚗 <b>Volkswagen Polo (2013)</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 600.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 1.2 / 1.6 л\r\n<b>Мощность:</b> 60 – 110 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> хэтчбек / седан\r\n<b>Руль:</b> левый\r\n<b>Модель двигателя:</b> 1.2 TSI / 1.6 TDI\r\n\r\n⛽️ <b>Топливо:</b> АИ-95 / ДТ\r\n🛢 <b>Объём топливного бака:</b> 45 л\r\n⛽️ <b>Расход топлива (средний):</b> 5,5 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1300 кг\r\n🏁 <b>Максимальная скорость:</b> 190 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: volkswagenpo);
                                    return;

                                }
                            case "/volkswagengo":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var volkswagengo = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/volkswagen"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/Xs2h9clnGdp49Uuz6M4IJSxpy9Z5kLhnPcF2h4fmpYzBEC-pGMf41Ejvm9rnYFCdzZNZIOwiwFpszqhd/gen1200.jpg", // Замените на действительный URL
                                        "🚗 <b>Volkswagen Golf (1992)</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 300.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 1.6 / 1.9 TDI л\r\n<b>Мощность:</b> 75 – 115 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> хэтчбек / седан\r\n<b>Руль:</b> левый\r\n<b>Модель двигателя:</b> 1.8 / 1.9 TDI\r\n\r\n⛽️ <b>Топливо:</b> АИ-92 / ДТ\r\n🛢 <b>Объём топливного бака:</b> 50 л\r\n⛽️ <b>Расход топлива (средний):</b> 6,5 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1200 кг\r\n🏁 <b>Максимальная скорость:</b> 180 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: volkswagengo);
                                    return;

                                }
                            case "/volkswagenpha":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var volkswagenpha = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/volkswagen"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/DFQaf6fDg6zOnIXp60plunZe2_gFbrX76Y1E4baYJGSpx1uHrDCp5ovNDZsVv3gKxtycnbxLiHMBwg6b/gen1200.jpg", // Замените на действительный URL
                                        "🚗 <b>Volkswagen Phaeton (2008)</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 1.500.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 3.0 / 4.2 л\r\n<b>Мощность:</b> 240 – 450 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> полный\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n<b>Модель двигателя:</b> 3.0 TDI / 4.2 V8\r\n\r\n⛽️ <b>Топливо:</b> АИ-95 / ДТ\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 10,0 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 2200 кг\r\n🏁 <b>Максимальная скорость:</b> 250 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: volkswagenpha);
                                    return;

                                }
                            case "/volkswagenbo":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var volkswagenbo = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/volkswagen"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/V5-4n652vvTti3PTlCG7CvLcwB6GnGk7cUajWYdwkbJwOgwF79q8HfyKg93697QoMsJW7WkTJfXrKXpq/gen1200.jpg", // Замените на действительный URL
                                        "🚗 <b>Volkswagen Bora (2002)</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 500.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 1.6 / 1.9 TDI л\r\n<b>Мощность:</b> 100 – 130 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n<b>Модель двигателя:</b> 1.6 / 1.9 TDI\r\n\r\n⛽️ <b>Топливо:</b> АИ-92 / ДТ\r\n🛢 <b>Объём топливного бака:</b> 55 л\r\n⛽️ <b>Расход топлива (средний):</b> 6,8 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1400 кг\r\n🏁 <b>Максимальная скорость:</b> 190 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: volkswagenbo);
                                    return;

                                }
                            case "/volkswagenve":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var volkswagenve = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/volkswagen"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/-zTW-ejWImL3nP_qAU_G6kdk_yAbxKoXqa7CwhNpD9Zu34Txcs22hGnESC2NSRhDK33N_WBv4FigAZuf/gen1200.jpg", // Замените на действительный URL
                                        "🚗 <b>Volkswagen Vento (1993)</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 350.000 ₽\r\n\r\n<b>Двигатель:</b> бензин / дизель, 1.6 / 1.8 л\r\n<b>Мощность:</b> 75 – 115 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n<b>Модель двигателя:</b> 1.6 / 1.8\r\n\r\n⛽️ <b>Топливо:</b> АИ-92 / ДТ\r\n🛢 <b>Объём топливного бака:</b> 50 л\r\n⛽️ <b>Расход топлива (средний):</b> 7,0 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1300 кг\r\n🏁 <b>Максимальная скорость:</b> 180 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: volkswagenve);
                                    return;

                                }
                            case "/hondaac":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var hondaac = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/honda"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/y134iIyCKxb0fsh_TxchjWE6k89103eukryxaGcMHfb2SYtcGLoZCpTFWeci1xVpBIryFtv1RhFk3B2-/gen1200.jpg", // Замените на действительный URL
                                        "🚗 <b>Honda Accord (2006)</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 800.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.4 л / 3.0 л\r\n<b>Мощность:</b> 190 – 251 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92\r\n🛢 <b>Объём топливного бака:</b> 65 л\r\n⛽️ <b>Расход топлива (средний):</b> 8,5 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1500 кг\r\n🏁 <b>Максимальная скорость:</b> 210 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: hondaac);
                                    return;

                                }
                            case "/hondaci":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var hondaci = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/honda"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/cPAgl9vmQjc9_BIBCpssVrCVj5SGLqm3r0-qH4lp53GRBkAFGyasDOAFJ9HybEBYy445LmxPVKPCoA6r/gen1200.jpg", // Замените на действительный URL
                                        "🚗 <b>Honda Civic (2006)</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 600.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.8 л / 2.0 л\r\n<b>Мощность:</b> 140 – 197 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> хэтчбек / седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92\r\n🛢 <b>Объём топливного бака:</b> 50 л\r\n⛽️ <b>Расход топлива (средний):</b> 7,0 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1300 кг\r\n🏁 <b>Максимальная скорость:</b> 200 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: hondaci);
                                    return;

                                }
                            case "/hondato":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var hondato = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/honda"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/hSw3rK8mOvlkQboo5-nRDSzQhTw5hqMHqmX4UI5wEB5XuN00W6mfIOMcsO9nM93c7CGFiTz5wSuUnvUn/gen1200.jpg", // Замените на действительный URL
                                        "🚗 <b>Honda Torneo (2001)</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 500.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.0 л\r\n<b>Мощность:</b> 150 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92\r\n🛢 <b>Объём топливного бака:</b> 50 л\r\n⛽️ <b>Расход топлива (средний):</b> 8,0 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1400 кг\r\n🏁 <b>Максимальная скорость:</b> 190 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: hondato);
                                    return;

                                }
                            case "/hondale":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var hondale = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/honda"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/9_FzjOr-vPQIPjRoYJOZ9Ti-aevna4v_rj6uXSwh5bS6nzkbA4E9dDae0mcaAQ9AKq93CCHNrJdMl5ZV/gen1200.jpg", // Замените на действительный URL
                                        "🚗 <b>Honda Legend (2006)</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 1.200.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 3.5 л\r\n<b>Мощность:</b> 300 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> полный\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 10,0 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1800 кг\r\n🏁 <b>Максимальная скорость:</b> 250 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: hondale);
                                    return;

                                }
                            case "/hondapre":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var hondapre = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/honda"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/Q621GmFhgfKwFhbAbnbxRqr5TT-G4Q5MOX3J3ANqOmSbaHxvnh_kQ0a4bxZmVMPp9KVhlycBcDEfptCU/gen1200.jpg", // Замените на действительный URL
                                        "🚗 <b>Honda Prelude (1999)</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 400.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.2 л\r\n<b>Мощность:</b> 200 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> купе\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 50 л\r\n⛽️ <b>Расход топлива (средний):</b> 9,0 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1300 кг\r\n🏁 <b>Максимальная скорость:</b> 220 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: hondapre);
                                    return;

                                }
                            case "/hondains":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var hondains = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/honda"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/gOSRPmZHmsMjVk6wAIGeUNJK83CucuGTRF9b6v9mGY-05QVLzQdu0q53UZ8if-bJutRDeMUaz7j-GLAU/gen1200.jpg", // Замените на действительный URL
                                        "🚗 <b>Honda Inspire (2001)</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 600.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 3.0 л\r\n<b>Мощность:</b> 250 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 65 л\r\n⛽️ <b>Расход топлива (средний):</b> 9,5 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> 1600 кг\r\n🏁 <b>Максимальная скорость:</b> 230 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: hondains);
                                    return;

                                }
                            case "/hyundaiso":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var hyundaiso = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/hyundai"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/mnAyhB3IiWCVHWG2UuQbOMiKN-dvrkrRlokKIn1tK4Fh5eVAzIUCqBDhDoA3XRVuJzkoOZ8pTqRYngTG/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Hyundai Sonata 2004</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 350.000 – 450.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.0 / 2.4 л\r\n<b>Мощность:</b> 140 – 162 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92 / АИ-95\r\n🛢 <b>Объём топливного бака:</b> 65 л\r\n⛽️ <b>Расход топлива (средний):</b> 7,5 – 9 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1600 кг\r\n🏁 <b>Максимальная скорость:</b> 190 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: hyundaiso);
                                    return;

                                }
                            case "/hyundaiav":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var hyundaiav = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/hyundai"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/tjzyJVm8pX-OtzfVGP9s8zhf7GJuVfom1VA39pM8Ym-4AlzjrkG7QeKKTfPHJTXw71ACx8adKce858bx/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Hyundai Avante 2005</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 300.000 – 400.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.6 / 2.0 л\r\n<b>Мощность:</b> 105 – 140 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан / хэтчбек\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92 / АИ-95\r\n🛢 <b>Объём топливного бака:</b> 55 л\r\n⛽️ <b>Расход топлива (средний):</b> 6,5 – 8 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1400 кг\r\n🏁 <b>Максимальная скорость:</b> 180 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: hyundaiav);
                                    return;

                                }
                            case "/hyundaiac":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var hyundaiac = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/hyundai"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/jx-gwI7eBFFNPpKunH-YRcA8N-fyZa1cvxtxd4V0AFStmUp4qIwyjITcUlqIL3HaLrIQqkYHe5OKdJvV/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Hyundai Accent 2008</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 250.000 – 350.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.4 / 1.6 л\r\n<b>Мощность:</b> 97 – 110 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан / хэтчбек\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92 / АИ-95\r\n🛢 <b>Объём топливного бака:</b> 43 л\r\n⛽️ <b>Расход топлива (средний):</b> 5,5 – 7 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1200 кг\r\n🏁 <b>Максимальная скорость:</b> 175 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: hyundaiac);
                                    return;

                                }
                            case "/hyundaiela":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var hyundaiela = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/hyundai"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/VmzbrBBtWt9bgCiacMlISJGC-N6CGc1FsWzkVZNoOwSsFEjaFI6fuz3Zcepfe7RTEImVG0uXjW-7vcEi/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Hyundai Elantra 2003</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 300.000 – 400.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.6 / 2.0 л\r\n<b>Мощность:</b> 105 – 140 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92 / АИ-95\r\n🛢 <b>Объём топливного бака:</b> 55 л\r\n⛽️ <b>Расход топлива (средний):</b> 6,5 – 8 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1400 кг\r\n🏁 <b>Максимальная скорость:</b> 185 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: hyundaiela);
                                    return;

                                }
                            case "/hyundainf":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var hyundainf = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/hyundai"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/X4X-EjLqnelCrJ_ZO9dAoNzC8t1LBoUTjusCaIwNcBR_d16xNaZasenEZd1VRLPTedpS9XdEC6GvYehk/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Hyundai NF 2006</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 400.000 – 550.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.0 / 2.4 л\r\n<b>Мощность:</b> 140 – 170 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 65 л\r\n⛽️ <b>Расход топлива (средний):</b> 7,5 – 9 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1600 кг\r\n🏁 <b>Максимальная скорость:</b> 195 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: hyundainf);
                                    return;

                                }
                            case "/hyundaiti":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var hyundaiti = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/hyundai"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/ImoHrlFep68i7pEnXtv_BjAk_4mHZaocBa7jzzfwgANrsmUx8rbeeFwcDb0OfkemtaHZMrPLojgknlpH/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Hyundai Tiburon 2006</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 450.000 – 600.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.0 / 2.7 л\r\n<b>Мощность:</b> 140 – 172 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний / задний (зависит от модификации)\r\n<b>Тип кузова:</b> купе\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 55 л\r\n⛽️ <b>Расход топлива (средний):</b> 7 – 9 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1400 кг\r\n🏁 <b>Максимальная скорость:</b> 210 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: hyundaiti);
                                    return;

                                }
                            case "/kiaspe":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var kiaspe = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/kia"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/eM5YtD2AxrRb6xY47deUswRCEeTFxenswAwbT6GGURaOlXRxUbBFgemNTw4buw6v48B9gE0FdpFITqfS/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Kia Spectra 2006</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 200.000 – 300.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.6 / 2.0 л\r\n<b>Мощность:</b> 110 – 143 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92 / АИ-95\r\n🛢 <b>Объём топливного бака:</b> 50 л\r\n⛽️ <b>Расход топлива (средний):</b> 7 – 9 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1400 кг\r\n🏁 <b>Максимальная скорость:</b> 180 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: kiaspe);
                                    return;

                                }
                            case "/kiace":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var kiace = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/kia"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/bo8zIiO8z2v2Rwy6SoN9l7nV_HQtU0SKa_REK3M7-taxeh2_uQlDo_IWOH1RfXgfDA0K9JQAuZGctAJg/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Kia Cerato 2009</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 300.000 – 400.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.6 / 2.0 л\r\n<b>Мощность:</b> 122 – 143 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан / хэтчбек\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92 / АИ-95\r\n🛢 <b>Объём топливного бака:</b> 50 л\r\n⛽️ <b>Расход топлива (средний):</b> 6,5 – 8 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1400 кг\r\n🏁 <b>Максимальная скорость:</b> 190 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: kiace);
                                    return;

                                }
                            case "/kiama":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var kiama = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/kia"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/CBWtwndlcyE6tcDLrAEYmXNiqfGG1mz1KISWnk4IrfE9OOPjpfxzals3YD8UhS8loLtFZIJme_DSMcFk/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Kia Magentis 2004</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 250.000 – 350.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.0 / 2.7 л\r\n<b>Мощность:</b> 136 – 192 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92 / АИ-95\r\n🛢 <b>Объём топливного бака:</b> 65 л\r\n⛽️ <b>Расход топлива (средний):</b> 8 – 10 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1600 кг\r\n🏁 <b>Максимальная скорость:</b> 200 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: kiama);
                                    return;

                                }
                            case "/kiaopt":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var kiaopt = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/kia"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/DR9dp9rUbr6FLdPNTvkHy0RuKASYV7G2Fg1Yoxj21a0tbs7kd16EWo0yt86RwhgbeZebvpJIIGawVbRC/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Kia Optima 2012</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 500.000 – 700.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.0 / 2.4 л\r\n<b>Мощность:</b> 150 – 200 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 7 – 9 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1600 кг\r\n🏁 <b>Максимальная скорость:</b> 210 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: kiaopt);
                                    return;

                                }
                            case "/kiaopi":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var kiaopi = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/kia"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/_17ct8OwzxhgwuBpGfMDm0uERYiwfwz7T9o4QKBXGOT6ZoFbPa07i06K_x1tiGxjlIbk8DCKmFadeKy6/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Kia Opirus 2006</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 400.000 – 600.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 3.5 л\r\n<b>Мощность:</b> 250 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 75 л\r\n⛽️ <b>Расход топлива (средний):</b> 10 – 12 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1800 кг\r\n🏁 <b>Максимальная скорость:</b> 220 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: kiaopi);
                                    return;

                                }
                            case "/kiafo":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var kiafo = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/kia"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/YhAwJGZpn1DoCxNt8wONPaOr581dIqSJGBZ8d8V7CDrbq_2c3Mc7kX6YR_pkj1mH59UG1zJ2iNOXZihq/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Kia Forte 2010</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 300.000 – 400.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.6 / 2.0 л\r\n<b>Мощность:</b> 122 – 156 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан / хэтчбек\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92 / АИ-95\r\n🛢 <b>Объём топливного бака:</b> 50 л\r\n⛽️ <b>Расход топлива (средний):</b> 7 – 9 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1400 кг\r\n🏁 <b>Максимальная скорость:</b> 190 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: kiafo);
                                    return;

                                }
                            case "/mitsubishila":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mitsubishila = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mitsubishi"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/bS37M4fwrZLkam0RMxbje8fw_y_3q6KQtcFI_ovCS3bUMONiljEHrF82AJiF2bPMf1o3BVn1NH863tmI/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mitsubishi Lancer 2007</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 400.000 – 600.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.5 / 2.0 л\r\n<b>Мощность:</b> 109 – 150 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан / хэтчбек\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92 / АИ-95\r\n🛢 <b>Объём топливного бака:</b> 50 л\r\n⛽️ <b>Расход топлива (средний):</b> 8 – 10 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1400 кг\r\n🏁 <b>Максимальная скорость:</b> 200 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mitsubishila);
                                    return;

                                }
                            case "/mitsubishiga":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mitsubishiga = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mitsubishi"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/x5HXFDGFFb2b-61f8DbBZ-VwGdWdbEb9957p25pyH1_X7pctn0NfWxwlknMcW5WiaR2Hy0ONip8p/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mitsubishi Galant 2000</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 300.000 – 500.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.4 л\r\n<b>Мощность:</b> 150 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 60 л\r\n⛽️ <b>Расход топлива (средний):</b> 9 – 11 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1500 кг\r\n🏁 <b>Максимальная скорость:</b> 210 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mitsubishiga);
                                    return;

                                }
                            case "/mitsubishimi":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mitsubishimi = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mitsubishi"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/iLX7UC3avFGWwxXRVap-78tGuxDrfLH5_LdQ3oUuD8l28Uc6aiFCayXPAqxBfv_TLFsLMfxOFUrAmMjg/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mitsubishi Mirage 1998</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 150.000 – 250.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 1.5 л\r\n<b>Мощность:</b> 92 л.с.\r\n<b>Коробка передач:</b> механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> хэтчбек\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-92\r\n🛢 <b>Объём топливного бака:</b> 45 л\r\n⛽️ <b>Расход топлива (средний):</b> 6 – 8 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1100 кг\r\n🏁 <b>Максимальная скорость:</b> 180 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mitsubishimi);
                                    return;

                                }
                            case "/mitsubishidi":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mitsubishidi = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mitsubishi"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/K_PLbV8JEgKZlz2pIzwHCPvjzHDB3_EOWe_2o1TwQHpBQPG-J3OdYIsQ1fPDyGIauvAkg2lefH0gFnYs/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mitsubishi Diamante 1997</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 200.000 – 350.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 3.5 л\r\n<b>Мощность:</b> 200 л.с.\r\n<b>Коробка передач:</b> автомат\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> седан\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 70 л\r\n⛽️ <b>Расход топлива (средний):</b> 11 – 13 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1600 кг\r\n🏁 <b>Максимальная скорость:</b> 220 км/ч",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mitsubishidi);
                                    return;

                                }
                            case "/mitsubishiesl":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mitsubishiesl = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mitsubishi"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/XIRvIIhLYg28ujVagC-KmxANrYoAqXLU7rnMp94JU1a8mjJZHQC001KNNYPECYLOcxtoY_2ViGbbdIHk/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mitsubishi Eclipse 2002</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 250.000 – 400.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.0 / 2.4 л\r\n<b>Мощность:</b> 147 – 200 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> купе\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 55 л\r\n⛽️ <b>Расход топлива (средний):</b> 8 – 11 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1350 кг\r\n🏁 <b>Максимальная скорость:</b> 220 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mitsubishiesl);
                                    return;

                                }
                            case "/mitsubishifto":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mitsubishi = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Назад", "/mitsubishi"),

                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start"),
                                            }

                                                    });
                                    await botClient.SendPhoto(
                                        chat.Id,
                                        "https://s12.auto.drom.ru/photo/v2/aWmp_yL_S3NmcHNAzFEvR7BnnnXL6jq9q_FGXI040GDo5ft9RpozMjX-5g9gQ2plbYMZ9lVGRX1DQEQ0/gen1200.jpg", // Замените на действительный URL
                                        "🚗<b>Mitsubishi FTO 1996</b>\r\n💸 <b>Примерная стоимость:</b> ≈ 300.000 – 500.000 ₽\r\n\r\n<b>Двигатель:</b> бензин, 2.0 л\r\n<b>Мощность:</b> 200 л.с.\r\n<b>Коробка передач:</b> автомат / механическая\r\n<b>Привод:</b> передний\r\n<b>Тип кузова:</b> купе\r\n<b>Руль:</b> левый / правый\r\n\r\n⛽️ <b>Топливо:</b> АИ-95\r\n🛢 <b>Объём топливного бака:</b> 50 л\r\n⛽️ <b>Расход топлива (средний):</b> 9 – 11 л / 100 км\r\n\r\n⚖️ <b>Полная масса авто:</b> около 1300 кг\r\n🏁 <b>Максимальная скорость:</b> 230 км/ч\r\n\r\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mitsubishi);
                                    return;

                                }
                            case "/autocatalog":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку

                                    var inlineKeyboard = new InlineKeyboardMarkup(
                                            new List<InlineKeyboardButton[]>() // здесь создаем лист (массив), который содрежит в себе массив из класса кнопок
                                            {
                                        // Каждый новый массив - это дополнительные строки,
                                        // а каждая дополнительная кнопка в массиве - это добавление ряда

                                        new InlineKeyboardButton[] // тут создаем массив кнопок
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Toyota", "/toyota"),
                                            InlineKeyboardButton.WithCallbackData("🔗Nissan", "/nissan"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Mersedes", "/mersedes"),
                                            InlineKeyboardButton.WithCallbackData("🔗BMW", "/bmw"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Лада", "/lada"),
                                            InlineKeyboardButton.WithCallbackData("🔗Lexus", "/lexus"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Ford", "/ford"),
                                            InlineKeyboardButton.WithCallbackData("🔗Mazda", "/mazda"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Audi", "/audi"),
                                            InlineKeyboardButton.WithCallbackData("🔗Volkswagen", "/volkswagen"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Honda", "/honda"),
                                            InlineKeyboardButton.WithCallbackData("🔗Hyundai", "/hyundai"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Kia", "/kia"),
                                            InlineKeyboardButton.WithCallbackData("🔗Mitsubishi", "/mitsubishi"),
                                        },

                                        new InlineKeyboardButton []
                                        {
                                            InlineKeyboardButton.WithCallbackData("Назад", "/start")
                                        }
                                            });

                                    await botClient.SendMessage(
                                        chat.Id,
                                        "<b>🚗✨ Список самых популярных марок автомобилей:</b>", // Жирный текст без экранирования
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: inlineKeyboard
                                        );

                                    return;

                                }
                            case "/toyotamr1":
                            case "/toyotach1":
                           
                            case "/toyotacr1":
                            case "/toyotaca1":
                            case "/toyotacri1":
                            case "/toyotacrpr1":

                            case "/nissansi1":
                            case "/nissansk1":
                            case "/nissanla1":
                            case "/nissansu1":
                            case "/nissanse1":
                            case "/nissangtr1":

                            case "/mersedese1":
                            case "/mersedesc1":
                            case "/mersedesg1":
                            case "/mersedess1":
                            case "/mersedesm1":
                            case "/mersedesw1":

                            case "/bmwe341":
                            case "/bmwe391":
                            case "/bmwe601":
                            case "/bmwf101":
                            case "/bmw3se1":
                            case "/bmw5se1":

                            case "/lada071":
                            case "/lada051":
                            case "/lada141":
                            case "/lada121":
                            case "/ladapr1":
                            case "/lada091":

                            case "/lexusis2001":
                            case "/lexusis3001":
                            case "/lexusis2501":
                            case "/lexuses300h1":
                            case "/lexusks4601":
                            case "/lexusgs3001":

                            case "/fordfo1":
                            case "/fordmo1":
                            case "/fordfi1":
                            case "/fordfu1":
                            case "/fordmu1":
                            case "/fordthu1":

                            case "/mazdarx71":
                            case "/mazda61":
                            case "/mazdaat1":
                            case "/mazdaax1":
                            case "/mazdafa1":
                            case "/mazdaca1":

                            case "/audia41":
                            case "/audia51":
                            case "/audia61":
                            case "/audiv81":
                            case "/audia81":
                            case "/audi1001":

                            case "/volkswagenpa1":
                            case "/volkswagenpo1":
                            case "/volkswagengo1":
                            case "/volkswagenpha1":
                            case "/volkswagenbo1":
                            case "/volkswagenve1":

                            case "/hondaacc1":
                            case "/hondaci1":
                            case "/hondato1":
                            case "/hondale1":
                            case "/hondapre1":
                            case "/hondains1":

                            case "/hyundaiso1":
                            case "/hyundaiav1":
                            case "/hyundaiacc1":
                            case "/hyundaiela1":
                            case "/hyundainf1":
                            case "/hyundaiti1":

                            case "/kiaspe1":
                            case "/kiace1":
                            case "/kiama1":
                            case "/kiaopt1":
                            case "/kiaopi1":
                            case "/kiafo1":

                            case "/mitsubishila1":
                            case "/mitsubishiga1":
                            case "/mitsubishimi1":
                            case "/mitsubishidi1":
                            case "/mitsubishiesl1":
                            case "/mitsubishifto1":
                                {
                                    string carKey = callbackQuery.Data.TrimStart('/'); // Убираем слэш
                                    if (!cars.ContainsKey(carKey))
                                    {
                                        await botClient.SendMessage(chat.Id, "Машина не найдена. Пожалуйста, выберите корректную команду.");
                                        return; // break удален
                                    }

                                    if (selectedCar1 == null)
                                    {
                                        selectedCar1 = carKey;
                                        // Улучшенный вывод списка машин с использованием InlineKeyboard
                                        var inlineKeyboard3 = new InlineKeyboardMarkup(
                                           new List<InlineKeyboardButton[]>() // здесь создаем лист (массив), который содрежит в себе массив из класса кнопок
                                           {
                                        // Каждый новый массив - это дополнительные строки,
                                        // а каждая дополнительная кнопка в массиве - это добавление ряда

                                        new InlineKeyboardButton[] // тут создаем массив кнопок
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Toyota", "/toyota2"),
                                            InlineKeyboardButton.WithCallbackData("🔗Nissan", "/nissan2"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Mersedes", "/mersedes2"),
                                            InlineKeyboardButton.WithCallbackData("🔗BMW", "/bmw2"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Лада", "/lada2"),
                                            InlineKeyboardButton.WithCallbackData("🔗Lexus", "/lexus2"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Ford", "/ford2"),
                                            InlineKeyboardButton.WithCallbackData("🔗Mazda", "/mazda2"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Audi", "/audi2"),
                                            InlineKeyboardButton.WithCallbackData("🔗Volkswagen", "/volkswagen2"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Honda", "/honda2"),
                                            InlineKeyboardButton.WithCallbackData("🔗Hyundai", "/hyundai2"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("🔗Kia", "/kia2"),
                                            InlineKeyboardButton.WithCallbackData("🔗Mitsubishi", "/mitsubishi2"),
                                        },
                                        new InlineKeyboardButton[]
                                        {
                                            InlineKeyboardButton.WithCallbackData("Главное меню", "/start")
                                        }



                                           });

                                        await botClient.SendMessage(chat.Id, $" 🚙✨ <b>Вы выбрали {cars[selectedCar1].Name}. Теперь выберите марку второй машины:</b>", 
                                            replyMarkup: inlineKeyboard3,
                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                                    }
                                    else if (selectedCar2 == null)
                                    {
                                        selectedCar2 = carKey;
                                        var comparisonResult = CompareCars(cars[selectedCar1], cars[selectedCar2]);

                                        var inlineKeyboard1 = new InlineKeyboardMarkup(new[]
                                        {
                                            new []
                                                    {
                                                              InlineKeyboardButton.WithCallbackData("Вернуться в главное меню", "/start")
                                                    }
                                                    });

                                        await botClient.SendMessage(chat.Id, comparisonResult, replyMarkup: inlineKeyboard1, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                                        selectedCar1 = null;
                                        selectedCar2 = null;
                                    }
                                    return; // break удален
                                }
                            case "/toyota1":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);

                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var toyota = new InlineKeyboardMarkup(
                                                new List<InlineKeyboardButton[]>()
                                                {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Toyota Mark II (x90), 1993", "/toyotamr1"),
                                                InlineKeyboardButton.WithCallbackData("Toyota Chaser (x100), 1999", "/toyotach1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Toyota Cresta JZD100, 2000", "/toyotacr1"),
                                                InlineKeyboardButton.WithCallbackData("Toyota Camry 3.5, 2015", "/toyotaca1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Toyota Land Cruiser 4.7, 1985", "/toyotacri1"),
                                                InlineKeyboardButton.WithCallbackData("Toyota Land Cruiser Prado 4.0, 2010", "/toyotacrpr1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")
                                            }
                                                });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель первой машины: </b>\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: toyota);
                                    return;

                                }

                            case "/nissan1":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var nissan1 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Nissan Silvia, 2002", "/nissansi1"),
                                                InlineKeyboardButton.WithCallbackData("Nissan SkyLine, 1998", "/nissansk1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Nissan Laurel, 1999", "/nissanla1"),
                                                InlineKeyboardButton.WithCallbackData("Nissan Sunny, 2000", "/nissansu1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Nissan Sentra, 2002", "/nissanse1"),
                                                InlineKeyboardButton.WithCallbackData("Nissan GT-R, 2008", "/nissangtr1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель первой машины:</b>\n",

                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: nissan1);
                                    return;
                                }

                            case "/mersedes1":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mersedes1 = new InlineKeyboardMarkup(
                                       new List<InlineKeyboardButton[]>()
                                       {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mersedes E-Class, 1995", "/mersedese1"),
                                                InlineKeyboardButton.WithCallbackData("Mersedes C-Class, 2003", "/mersedesc1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mersedes G-Class, 1985", "/mersedesg1"),
                                                InlineKeyboardButton.WithCallbackData("Mersedes S-Class, 1992", "/mersedess1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mersedes M-Class, 2001", "/mersedesm1"),
                                                InlineKeyboardButton.WithCallbackData("Mersedes W123, 1982", "/mersedesw1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                       });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель первой машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mersedes1);
                                    return;
                                }
                            case "/bmw1":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var bmw1 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("BMW M5 E34, 1995", "/bmwe341"),
                                                InlineKeyboardButton.WithCallbackData("BMW M5 E39, 2000", "/bmwe391")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("BMW M5 E60, 2005", "/bmwe601"),
                                                InlineKeyboardButton.WithCallbackData("BMW M5 F10, 2012", "/bmwf101")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("BMW 3-Series, 2011", "/bmw3se1"),
                                                InlineKeyboardButton.WithCallbackData("BMW 5-Series, 2001", "/bmw5se1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });


                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель первой машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: bmw1);
                                    return;
                                }
                            case "/audi1":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var audi1 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Audi A4, 2004", "/audia41"),
                                                InlineKeyboardButton.WithCallbackData("Audi A5, 2009", "/audia51")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Audi A6, 2006", "/audia61"),
                                                InlineKeyboardButton.WithCallbackData("Audi V8, 1988", "/audiv81")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Audi A8, 2004", "/audia81"),
                                                InlineKeyboardButton.WithCallbackData("Audi 100, 1991", "/audi1001")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель первой машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: audi1);
                                    return;
                                }
                            case "/lexus1":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var lexus1 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Lexus IS200, 2000", "/lexusis2001"),
                                                InlineKeyboardButton.WithCallbackData("Lexus IS300, 2019", "/lexusis3001")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Lexus IS250, 2006", "/lexusis2501"),
                                                InlineKeyboardButton.WithCallbackData("Lexus ES300h, 2019", "/lexuses300h1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Lexus LS460, 2008", "/lexusls4601"),
                                                InlineKeyboardButton.WithCallbackData("Lexus GS300, 2005", "/lexusgs3001")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель первой машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lexus1);
                                    return;
                                }
                            case "/ford1":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var ford1 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Ford Fokus, 2006", "/fordfo1"),
                                                InlineKeyboardButton.WithCallbackData("Ford Mondeo, 2006", "/fordmo1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Ford Fiesta, 2015", "/fordfi1"),
                                                InlineKeyboardButton.WithCallbackData("Ford Fusion, 2010", "/fordfu1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Ford Mustang, 1999", "/fordmu1"),
                                                InlineKeyboardButton.WithCallbackData("Ford Thunderbird, 1989", "/fordthu1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель первой машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: ford1);
                                    return;
                                }
                            case "/mazda1":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mazda1 = new InlineKeyboardMarkup(
                                       new List<InlineKeyboardButton[]>()
                                       {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mazda RX-7, 2000", "/mazdarx71"),
                                                InlineKeyboardButton.WithCallbackData("Mazda Mazda6, 2005", "/mazda61")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mazda Atenza, 2008", "/mazdaat1"),
                                                InlineKeyboardButton.WithCallbackData("Mazda Axela, 2016", "/mazdaax1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mazda Familia, 2003", "/mazdafa1"),
                                                InlineKeyboardButton.WithCallbackData("Mazda Capella, 1998", "/mazdaca1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                       });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель первой машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mazda1);
                                    return;
                                }
                            case "/lada1":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var lada1 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Лада 2107, 2010", "/lada071"),
                                                InlineKeyboardButton.WithCallbackData("Лада 2105, 2007", "/lada051")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Лада 2114, 2010", "/lada141"),
                                                InlineKeyboardButton.WithCallbackData("Лада Приора, 2012", "/ladapr1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Лада 2112, 2008", "/lada121"),
                                                InlineKeyboardButton.WithCallbackData("Лада 2109, 2003", "/lada091")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель первой машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lada1);
                                    return;
                                }
                            case "/volkswagen1":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var volks1 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Passat, 1998", "/volkswagenpa1"),
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Polo, 2013", "/volkswagenpo1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Golf, 1992", "/volkswagengo1"),
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Phaeton, 2008", "/volkswagenpha1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Bora, 2002", "/volkswagenbo1"),
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Vento, 1993", "/volkswagenve1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель первой машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: volks1);
                                    return;
                                }
                            case "/honda1":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var honda1 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Honda Accord, 2006", "/hondaac1"),
                                                InlineKeyboardButton.WithCallbackData("Honda Civic, 2006", "/hondaci1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Honda Torneo, 2001", "/hondato1"),
                                                InlineKeyboardButton.WithCallbackData("Honda Legend, 2006", "/hondale1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Honda Prelude, 1999", "/hondapre1"),
                                                InlineKeyboardButton.WithCallbackData("Honda Inspire, 2001", "/hondains1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель первой машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,

                                        replyMarkup: honda1);
                                    return;
                                }
                            case "/hyundai1":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var hyundai1 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Hyundai Sonata, 2004", "/hyundaiso1"),
                                                InlineKeyboardButton.WithCallbackData("Hyundai Avente, 2005", "/hyundaiav1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Hyundai Accent, 2008", "/hyundaiac1"),
                                                InlineKeyboardButton.WithCallbackData("Hyundai Elantra, 2003", "/hyundaiela1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Hyundai NF, 2006", "/hyundainf1"),
                                                InlineKeyboardButton.WithCallbackData("Hyundai Tiburon, 2006", "/hyundaiti1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                               
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель первой машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: hyundai1);
                                    return;
                                }
                            case "/kia1":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var kia1 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Kia Spectra, 2006", "/kiaspe1"),
                                                InlineKeyboardButton.WithCallbackData("Kia Cerato, 2009", "/kiace1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Kia Magentis, 2004", "/kiama1"),
                                                InlineKeyboardButton.WithCallbackData("Kia Optima, 2012", "/kiaopt1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Kia Opirus, 2006", "/kiaopi1"),
                                                InlineKeyboardButton.WithCallbackData("Kia Forte, 2010", "/kiafo1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                               
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель первой машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: kia1);
                                    return;
                                }
                            case "/mitsubishi1":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mitsi1 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Lancer, 2007", "/mitsubishila1"),
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Galant, 2000", "/mitsubishiga1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Mirage, 1998", "/mitsubishimi1"),
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Diamante, 1997", "/mitsubishidi1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Eslipse, 2002", "/mitsubishiesl1"),
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi FTO, 1996", "/mitsubishifto1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель первой машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mitsi1);
                                    return;
                                }
                            case "/toyota2":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);

                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var toyota2 = new InlineKeyboardMarkup(
                                                new List<InlineKeyboardButton[]>()
                                                {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Toyota Mark II (x90), 1993", "/toyotamr1"),
                                                InlineKeyboardButton.WithCallbackData("Toyota Chaser (x100), 1999", "/toyotach1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Toyota Cresta JZD100, 2000", "/toyotacr1"),
                                                InlineKeyboardButton.WithCallbackData("Toyota Camry 3.5, 2015", "/toyotaca1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Toyota Land Cruiser 4.7, 1985", "/toyotacri1"),
                                                InlineKeyboardButton.WithCallbackData("Toyota Land Cruiser Prado 4.0, 2010", "/toyotacrpr1")
                                            },
                                            new InlineKeyboardButton[]
                                            {

                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")
                                            }
                                                });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель второй машины: </b>\n",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: toyota2);
                                    return;

                                }

                            case "/nissan2":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var nissan2 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Nissan Silvia, 2002", "/nissansi1"),
                                                InlineKeyboardButton.WithCallbackData("Nissan SkyLine, 1998", "/nissansk1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Nissan Laurel, 1999", "/nissanla1"),
                                                InlineKeyboardButton.WithCallbackData("Nissan Sunny, 2000", "/nissansu1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Nissan Sentra, 2002", "/nissanse1"),
                                                InlineKeyboardButton.WithCallbackData("Nissan GT-R, 2008", "/nissangtr1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель второй машины:</b>\n",

                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: nissan2);
                                    return;
                                }

                            case "/mersedes2":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mersedes2 = new InlineKeyboardMarkup(
                                       new List<InlineKeyboardButton[]>()
                                       {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mersedes E-Class, 1995", "/mersedese1"),
                                                InlineKeyboardButton.WithCallbackData("Mersedes C-Class, 2003", "/mersedesc1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mersedes G-Class, 1985", "/mersedesg1"),
                                                InlineKeyboardButton.WithCallbackData("Mersedes S-Class, 1992", "/mersedess1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mersedes M-Class, 2001", "/mersedesm1"),
                                                InlineKeyboardButton.WithCallbackData("Mersedes W123, 1982", "/mersedesw1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                       });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель второй машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mersedes2);
                                    return;
                                }
                            case "/bmw2":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var bmw2 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("BMW M5 E34, 1995", "/bmwe341"),
                                                InlineKeyboardButton.WithCallbackData("BMW M5 E39, 2000", "/bmwe391")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("BMW M5 E60, 2005", "/bmwe601"),
                                                InlineKeyboardButton.WithCallbackData("BMW M5 F10, 2012", "/bmwf101")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("BMW 3-Series, 2011", "/bmw3se1"),
                                                InlineKeyboardButton.WithCallbackData("BMW 5-Series, 2001", "/bmw5se1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });

                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель второй машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: bmw2);
                                    return;
                                }
                            case "/audi2":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var audi2 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Audi A4, 2004", "/audia41"),
                                                InlineKeyboardButton.WithCallbackData("Audi A5, 2009", "/audia51")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Audi A6, 2006", "/audia61"),
                                                InlineKeyboardButton.WithCallbackData("Audi V8, 1988", "/audiv81")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Audi A8, 2004", "/audia81"),
                                                InlineKeyboardButton.WithCallbackData("Audi 100, 1991", "/audi1001")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель второй машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: audi2);
                                    return;
                                }
                            case "/lexus2":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var lexus2 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Lexus IS200, 2000", "/lexusis2001"),
                                                InlineKeyboardButton.WithCallbackData("Lexus IS300, 2019", "/lexusis3001")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Lexus IS250, 2006", "/lexusis2501"),
                                                InlineKeyboardButton.WithCallbackData("Lexus ES300h, 2019", "/lexuses300h1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Lexus LS460, 2008", "/lexusls4601"),
                                                InlineKeyboardButton.WithCallbackData("Lexus GS300, 2005", "/lexusgs3001")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель второй машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lexus2);
                                    return;
                                }
                            case "/ford2":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var ford2 = new InlineKeyboardMarkup(
                                       new List<InlineKeyboardButton[]>()
                                       {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Ford Fokus, 2006", "/fordfo1"),
                                                InlineKeyboardButton.WithCallbackData("Ford Mondeo, 2006", "/fordmo1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Ford Fiesta, 2015", "/fordfi1"),
                                                InlineKeyboardButton.WithCallbackData("Ford Fusion, 2010", "/fordfu1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Ford Mustang, 1999", "/fordmu1"),
                                                InlineKeyboardButton.WithCallbackData("Ford Thunderbird, 1989", "/fordthu1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                       });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель второй машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: ford2);
                                    return;
                                }
                            case "/mazda2":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mazda2 = new InlineKeyboardMarkup(
                                       new List<InlineKeyboardButton[]>()
                                       {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mazda RX-7, 2000", "/mazdarx71"),
                                                InlineKeyboardButton.WithCallbackData("Mazda Mazda6, 2005", "/mazda61")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mazda Atenza, 2008", "/mazdaat1"),
                                                InlineKeyboardButton.WithCallbackData("Mazda Axela, 2016", "/mazdaax1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mazda Familia, 2003", "/mazdafa1"),
                                                InlineKeyboardButton.WithCallbackData("Mazda Capella, 1998", "/mazdaca1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                       });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель второй машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mazda2);
                                    return;
                                }
                            case "/lada2":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var lada2 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Лада 2107, 2010", "/lada071"),
                                                InlineKeyboardButton.WithCallbackData("Лада 2105, 2007", "/lada051")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Лада 2114, 2010", "/lada141"),
                                                InlineKeyboardButton.WithCallbackData("Лада Приора, 2012", "/ladapr1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Лада 2112, 2008", "/lada121"),
                                                InlineKeyboardButton.WithCallbackData("Лада 2109, 2003", "/lada091")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель второй машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: lada2);
                                    return;
                                }
                            case "/volkswagen2":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var volks2 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Passat, 1998", "/volkswagenpa1"),
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Polo, 2013", "/volkswagenpo1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Golf, 1992", "/volkswagengo1"),
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Phaeton, 2008", "/volkswagenpha1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Bora, 2002", "/volkswagenbo1"),
                                                InlineKeyboardButton.WithCallbackData("Volkswagen Vento, 1993", "/volkswagenve1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель второй машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: volks2);
                                    return;
                                }
                            case "/honda2":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var honda2 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Honda Accord, 2006", "/hondaac1"),
                                                InlineKeyboardButton.WithCallbackData("Honda Civic, 2006", "/hondaci1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Honda Torneo, 2001", "/hondato1"),
                                                InlineKeyboardButton.WithCallbackData("Honda Legend, 2006", "/hondale1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Honda Prelude, 1999", "/hondapre1"),
                                                InlineKeyboardButton.WithCallbackData("Honda Inspire, 2001", "/hondains1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель второй машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,

                                        replyMarkup: honda2);
                                    return;
                                }
                            case "/hyundai2":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var hyundai2 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Hyundai Sonata, 2004", "/hyundaiso1"),
                                                InlineKeyboardButton.WithCallbackData("Hyundai Avente, 2005", "/hyundaiav1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Hyundai Accent, 2008", "/hyundaiac1"),
                                                InlineKeyboardButton.WithCallbackData("Hyundai Elantra, 2003", "/hyundaiela1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Hyundai NF, 2006", "/hyundainf1"),
                                                InlineKeyboardButton.WithCallbackData("Hyundai Tiburon, 2006", "/hyundaiti1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель второй машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: hyundai2);
                                    return;
                                }
                            case "/kia2":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var kia2 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Kia Spectra, 2006", "/kiaspe1"),
                                                InlineKeyboardButton.WithCallbackData("Kia Cerato, 2009", "/kiace1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Kia Magentis, 2004", "/kiama1"),
                                                InlineKeyboardButton.WithCallbackData("Kia Optima, 2012", "/kiaopt1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Kia Opirus, 2006", "/kiaopi1"),
                                                InlineKeyboardButton.WithCallbackData("Kia Forte, 2010", "/kiafo1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель второй машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: kia2);
                                    return;
                                }
                            case "/mitsubishi2":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку
                                    var mitsi2 = new InlineKeyboardMarkup(
                                        new List<InlineKeyboardButton[]>()
                                        {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Lancer, 2007", "/mitsubishila1"),
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Galant, 2000", "/mitsubishiga1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Mirage, 1998", "/mitsubishimi1"),
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Diamante, 1997", "/mitsubishidi1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi Eslipse, 2002", "/mitsubishiesl1"),
                                                InlineKeyboardButton.WithCallbackData("Mitsubishi FTO, 1996", "/mitsubishifto1")
                                            },
                                            new InlineKeyboardButton[]
                                            {
                                                
                                                InlineKeyboardButton.WithCallbackData("Главное меню", "/start")

                                            },

                                        });
                                    await botClient.SendMessage(
                                        chat.Id,
                                        $"<b>🚙✨ Теперь выберите модель второй машины:</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                        replyMarkup: mitsi2);
                                    return;
                                }
                            case "/start":
                                {
                                    // В этом типе клавиатуры обязательно нужно использовать следующий метод
                                    await botClient.AnswerCallbackQuery(callbackQuery.Id);
                                    // Для того, чтобы отправить телеграмму запрос, что мы нажали на кнопку

                                  

                                        var start = new InlineKeyboardMarkup(
                                                    new List<InlineKeyboardButton[]>()
                                                    {
                                            new InlineKeyboardButton[]
                                            {
                                                InlineKeyboardButton.WithCallbackData("Католог машин", "/autocatalog"),

                                            }

                                                    });
                                        await botClient.SendMessage(
                                            chat.Id,
                                            "🌟 <b>Главное меню:</b>\n" +
                                            "               \n" +
                                            "Каталог машин:\n" +
                                            "🔗 /autocatalog\n" +
                                            "Сравнить 2 разных автомобиля:\n" +
                                            "🔗 /compare ( Готово не до конца ) \n" +
                                            "Найти машину по ее году выпуска:\n" +
                                            "🔗 /search_year ( Готово не до конца )\n" +
                                            "Актуальный курс валют:\n" +
                                            "🔗 /currency ( Готово не до конца ) \n" +
                                            "Подписаться на рассылку бота:\n" +
                                            "🔗 /sub\n" +
                                            "Отписаться от рассылки бота:\n" +
                                            "🔗 /unsub\n" +
                                            "Прочие команды:\n" +
                                            "🔗 /commands\n" +
                                            "               \n" +
                                            "🚀 <b>Дополнительные упрощенные команды:</b>\n" +
                                            "" +
                                            "🔗 /add\n",




                                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                                            replyMarkup: start
                                        );
                                        return;
                                    }
                            case "/search_year":
                                {
                                    waitingForYear[chatId] = true; // Устанавливаем состояние ожидания
                                    await botClient.SendMessage(chatId, "🚗💬 <b>Пожалуйста, укажите год выпуска автомобиля, который вы хотите найти:</b>\r\n📅 Например: <b>2015</b>",
                                        parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                                    return; // Выходим, чтобы не обрабатывать другие команды
                                }

                            


                                
                            default:
                                // Неизвестная команда
                                await botClient.SendMessage(chat.Id, "НЕВЕРНАЯ КОМАНДА", cancellationToken: cancellationToken);
                                break;

                        }
                        await botClient.SendMessage(chat.Id, "НЕВЕРНАЯ КОМАНДА", cancellationToken: cancellationToken);

                        return;
                    }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
    //private static async Task ClearChat(long chatId)
    //{
    //    foreach (var messageId in sentMessageIds)
    //    {
    //        try
    //        {
    //            await _botClient.DeleteMessage(chatId, messageId);
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Ошибка при удалении сообщения: {ex.Message}");
    //        }
    //    }
    //    // Очищаем список после удаления
    //    sentMessageIds.Clear();
    //}

    private static Task ErrorHandler(ITelegramBotClient botClient, Exception error, CancellationToken cancellationToken)
    {
        // Тут создадим переменную, в которую поместим код ошибки и её сообщение 
        var ErrorMessage = error switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => error.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }
    public static void StopBot()
    {
        _cts.Cancel();
        Console.WriteLine("Бот остановлен.");
    }
    static Dictionary<string, Car> cars = new Dictionary<string, Car>
    {
        ["toyotamr1"] = new Car { Name = "Toyota Mark II (x90)", Year = 1993, Price = 800000, Engine = "2.0 бензин", Power = 140, MaxSpeed = 0, Massa = 0, Transmission = "автомат",  },
        ["toyotach1"] = new Car { Name = "Toyota Chaser (x100)", Year = 1999, Price = 850000, Engine = "2.5 бензин", Power = 200, MaxSpeed = 0, Massa = 0, Transmission = "механика" },
        ["toyotacr1"] = new Car { Name = "Toyota Cresta JZD100", Year = 2000, Price = 850000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["toyotaca1"] = new Car { Name = "Toyota Camry 3.5", Year = 2015, Price = 1500000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["toyotacri1"] = new Car { Name = "Toyota Land Cruiser 4.7", Year = 1985, Price = 1200000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["toyotacrpr1"] = new Car { Name = "Toyota Land Cruiser Prado", Year = 2010, Price = 1800000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },

        ["nissansi1"] = new Car { Name = "Nissan Silvia", Year = 2002, Price = 600000, Engine = "2.0 бензин", Power = 200, MaxSpeed = 0, Massa = 0, Transmission = "механика" },
        ["nissansk1"] = new Car { Name = "Nissan Skyline", Year = 1998, Price = 900000, Engine = "2.5 бензин", Power = 280, MaxSpeed = 0, Massa = 0, Transmission = "автомат" },
        ["nissanla1"] = new Car { Name = "Nissan Laurel C34", Year = 1999, Price = 800000, Engine = "2.0 бензин", Power = 155, MaxSpeed = 0, Massa = 0, Transmission = "автомат" },
        ["nissansu1"] = new Car { Name = "Nissan Sunny", Year = 2000, Price = 500000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["nissanse1"] = new Car { Name = "Nissan Sentra", Year = 2002, Price = 600000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["nissangtr1"] = new Car { Name = "Nissan GT-R", Year = 2008, Price = 6000000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },

        ["mersedese1"] = new Car { Name = "Mersedes E-Class", Year = 1995, Price = 800000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["mersedesc1"] = new Car { Name = "Mersedes C-Class", Year = 2003, Price = 600000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["mersedesg1"] = new Car { Name = "Mersedes G-Class", Year = 1985, Price = 1500000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["mersedess1"] = new Car { Name = "Mersedes S-Class", Year = 1992, Price = 1200000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["mersedesm1"] = new Car { Name = "Mersedes M-Class", Year = 2001, Price = 800000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["mersedesw1"] = new Car { Name = "Mersedes W123", Year = 1982, Price = 500000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },

        ["bmwe341"] = new Car { Name = "BMW M5 E34", Year = 1995, Price = 1200000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["bmwe391"] = new Car { Name = "BMW M5 E39", Year = 2000, Price = 1500000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["bmwe601"] = new Car { Name = "BMW M5 E60", Year = 2005, Price = 2800000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["bmwf101"] = new Car { Name = "BMW M5 F10", Year = 2012, Price = 4500000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["bmw3se1"] = new Car { Name = "BMW 3-Series", Year = 2011, Price = 1200000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["bmw5se1"] = new Car { Name = "BMW 5-Series", Year = 2001, Price = 1000000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },

        ["lada071"] = new Car { Name = "Лада 2107", Year = 2010, Price = 300000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["lada051"] = new Car { Name = "Лада 2105", Year = 2007, Price = 250000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["lada141"] = new Car { Name = "Лада 2114", Year = 2010, Price = 350000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["ladapr1"] = new Car { Name = "Лада Приора", Year = 2012, Price = 400000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["lada121"] = new Car { Name = "Лада 2112", Year = 2008, Price = 300000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["lada091"] = new Car { Name = "Лада 2109", Year = 2003, Price = 200000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },

        ["lexusis2001"] = new Car { Name = "Lexus IS200", Year = 2000, Price = 600000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["lexusis3001"] = new Car { Name = "Lexus IS300", Year = 2019, Price = 3000000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["lexusis2501"] = new Car { Name = "Lexus IS250", Year = 2006, Price = 1300000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["lexuses300h1"] = new Car { Name = "Lexus ES300h", Year = 2019, Price = 3500000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["lexusls4601"] = new Car { Name = "Lexus LS460", Year = 2008, Price = 2500000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["lexusgs3001"] = new Car { Name = "Lexus GS300", Year = 2005, Price = 1500000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },


        ["fordfo1"] = new Car { Name = "Ford Fokus", Year = 2006, Price = 600000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["fordmo1"] = new Car { Name = "Ford Mondeo", Year = 2006, Price = 700000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["fordfi1"] = new Car { Name = "Ford Fiesta", Year = 2015, Price = 700000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["fordfu1"] = new Car { Name = "Ford Fusion", Year = 2010, Price = 700000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["fordmu1"] = new Car { Name = "Ford Mustang", Year = 1999, Price = 2000000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["fordthu1"] = new Car { Name = "Ford Thunderbird", Year = 1989, Price = 1000000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },

        ["mazdarx71"] = new Car { Name = "Mazda RX-7", Year = 2000, Price = 1800000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["mazda61"] = new Car { Name = "Mazda Mazda6", Year = 2005, Price = 700000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["mazdaat1"] = new Car { Name = "Mazda Atenza", Year = 2008, Price = 800000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["mazdaax1"] = new Car { Name = "Mazda Axela", Year = 2016, Price = 1200000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["mazdafa1"] = new Car { Name = "Mazda Familia", Year = 2003, Price = 500000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["mazdaca1"] = new Car { Name = "Mazda Capella", Year = 1998, Price = 400000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },

        ["audia41"] = new Car { Name = "Audi A4", Year = 2004, Price = 600000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["audia51"] = new Car { Name = "Audi A5", Year = 2009, Price = 1200000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["audia61"] = new Car { Name = "Audi A6", Year = 2006, Price = 900000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["audiv81"] = new Car { Name = "Audi V8", Year = 1988, Price = 1100000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["audia81"] = new Car { Name = "Audi A8", Year = 2004, Price = 1500000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["audi1001"] = new Car { Name = "Audi 100", Year = 1991, Price = 300000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },


        ["volkswagenpa1"] = new Car { Name = "Volkswagen Passat", Year = 1998, Price = 400000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["volkswagenpo1"] = new Car { Name = "Volkswagen Polo", Year = 2013, Price = 600000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["volkswagengo1"] = new Car { Name = "Volkswagen Golf", Year = 1992, Price = 300000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["volkswagenpha1"] = new Car { Name = "Volkswagen Phaeton", Year = 2008, Price = 1500000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["volkswagenbo1"] = new Car { Name = "Volkswagen Bora", Year = 2002, Price = 500000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["volkswagenve1"] = new Car { Name = "Volkswagen Vento", Year = 1993, Price = 350000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },


        ["hondaacc1"] = new Car { Name = "Honda Accord", Year = 2006, Price = 800000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["hondaci1"] = new Car { Name = "Honda Civic", Year = 2006, Price = 600000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["hondato1"] = new Car { Name = "Honda Torneo", Year = 2001, Price = 500000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["hondale1"] = new Car { Name = "Honda Legend", Year = 2006, Price = 1200000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["hondapre1"] = new Car { Name = "Honda Prelude", Year = 1999, Price = 400000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["hondains1"] = new Car { Name = "Honda Inspire", Year = 2001, Price = 600000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },

        ["hyundaiso1"] = new Car { Name = "Hyundai Sonata", Year = 2004, Price = 450000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["hyundaiav1"] = new Car { Name = "Hyundai Avente", Year = 2005, Price = 400000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["hyundaiacc1"] = new Car { Name = "Hyundai Accent", Year = 2008, Price = 350000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["hyundaiela1"] = new Car { Name = "Hyundai Elantra", Year = 2003, Price = 400000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["hyundainf1"] = new Car { Name = "Hyundai NF", Year = 2006, Price = 500000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["hyundaiti1"] = new Car { Name = "Hyundai Tiburon", Year = 2006, Price = 600000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },

        ["kiaspe1"] = new Car { Name = "Kia Spectra", Year = 2006, Price = 300000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["kiace1"] = new Car { Name = "Kia Cerato", Year = 2009, Price = 400000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["kiama1"] = new Car { Name = "Kia Magentis", Year = 2004, Price = 350000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["kiaopt1"] = new Car { Name = "Kia Optima", Year = 2012, Price = 700000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["kiaopi1"] = new Car { Name = "Kia Opirus", Year = 2006, Price = 600000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["kiafo1"] = new Car { Name = "Kia Forte", Year = 2010, Price = 400000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },

        ["mitsubishila1"] = new Car { Name = "Mitsubishi Lancer", Year = 2007, Price = 600000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["mitsubishiga1"] = new Car { Name = "Mitsubishi Galant", Year = 2000, Price = 500000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["mitsubishimi1"] = new Car { Name = "Mitsubishi Mirage", Year = 1998, Price = 250000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["mitsubishidi1"] = new Car { Name = "Mitsubishi Diamante", Year = 1997, Price = 350000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["mitsubishiesl1"] = new Car { Name = "Mitsubishi Eslipse", Year = 2002, Price = 400000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },
        ["mitsubishifto1"] = new Car { Name = "Mitsubishi FTO", Year = 1996, Price = 500000, Engine = "", Power = 0, MaxSpeed = 0, Massa = 0, Transmission = "" },

    };
    private static string CompareCars(Car car1, Car car2)
    {




        return $"🏎️💨 <b>Сравнение основных характеристик:</b> 🌠 \n" +
            $"            \n" +
               $" 💨 <b>Название:</b>   {car1.Name}    /    {car2.Name}\n" +
               $" 🗓️ <b>Год выпуска:</b>   {car1.Year}    /    {car2.Year}\n" +
               $" 💰 <b>Примерная стоимость:</b>   {car1.Price}    /    {car2.Price} руб.\n" +
               $"              \n" +
               $" 🐎 <b>Мощность:</b>   {car1.Power}    /    {car2.Power} л.с\n" +
               $" ⚙️ <b>Двигатель:</b>   {car1.Engine}    /    {car2.Engine}\n" +
               $"        \n" +
               $" ⚖️ <b>Полная масса авто:</b>   {car1.Massa}    /    {car2.Massa} кг \n" +
               $" 🚀 <b>Максимальная скорость:</b>   {car1.MaxSpeed}    /    {car2.MaxSpeed} км/ч \n"
               ;
               
    
    }
    static async Task<string> GetCurrencyRatesAsync()
    {
        using var httpClient = new HttpClient();
        try
        {
            string url = "https://api.exchangerate.host/latest?base=USD&symbols=EUR,GBP,RUB";
            var response = await httpClient.GetStringAsync(url);

            Console.WriteLine(response); // для отладки

            var jsonDoc = JsonDocument.Parse(response);
            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("rates", out JsonElement rates))
                return "Ошибка: в ответе отсутствует поле 'rates'";

            string FormatRate(string currency)
            {
                if (rates.TryGetProperty(currency, out JsonElement rateElement))
                    return rateElement.GetDecimal().ToString();
                else
                    return "нет данных";
            }

            string eurStr = FormatRate("EUR");
            string gbpStr = FormatRate("GBP");
            string rubStr = FormatRate("RUB");

            return $"Курс валют по отношению к USD:\nEUR: {eurStr}\nGBP: {gbpStr}\nRUB: {rubStr}";
        }
        catch (Exception ex)
        {
            return $"Ошибка при получении курса валют: {ex.Message}";
        }
    }
    
}
