# maFileTool
[English Version](README.md) | [Русская Версия](README.ru.md)

![Icon](https://github.com/Riddler2077/maFileTool/blob/master/icon.ico)

Зачем это нужно:
- Данный софт отлично подойдёт для автоматической привязки Steam Guard к Вашим аккаунтам и создания мафайлов.

Возможности:
- <div>Автоматическое решение капчи через сервис rucaptcha/2capthca -  <a href="https://rucaptcha.com/?from=947328" target="_blank">регистрация</a></div>
- Использование 5 смс сервисов:
- GetSms - <a href="https://getsms.online/ru/reg.html" target="_blank">регистрация</a></div>
- GiveSms - <a href="https://give-sms.com/?ref=14040" target="_blank">регистрация</a></div>
- OnlineSim - <a href="https://onlinesim.io/?ref=40882" target="_blank">регистрация</a></div>
- SmsActivate - <a href="https://sms-activate.org/?ref=431207" target="_blank">регистрация</a></div>
- VakSms - <a href="https://vak-sms.com/accounts/registration/" target="_blank">регистрация</a></div>
- Автоматический переход на другой сервис если закончились номера или баланс
- Задание приоритетов
- Возможность изменить домен смс сервиса при блокировке РКН

Настройка:

Для настройки необходимо указать Ваши данные в файле Settings.json

Все настройки интуитивно понятны, но на всякий случай я расписал все ниже.
- Mode - Режим работы. Принимает одно из двух значений TXT или EXCEL
- BindingTimeout - Задержка между привязкой аккаунтов в минутах (по умолчанию 1 мин.)
- SMSTimeout - Время ожадания СМС кода в минутах (по умолчанию 1 мин.)
- CaptchaApiKey - api ключ сервиса <a href="https://rucaptcha.com/?from=947328" target="_blank">rucaptcha/2captcha</a></div>
- GetSmsApiKey - api ключ сервиса <a href="https://getsms.online/ru/reg.html" target="_blank">GetSms</a></div>
- GiveSmsApiKey - api ключ сервиса <a href="https://give-sms.com/?ref=14040" target="_blank">GiveSms</a></div>
- OnlineSimApiKey - api ключ сервиса <a href="https://onlinesim.io/?ref=40882" target="_blank">OnlineSim</a></div>
- SmsActivateApiKey - api ключ сервиса <a href="https://sms-activate.org/?ref=431207" target="_blank">SmsActivate</a></div>
- VakSmsApiKey - api ключ сервиса <a href="https://vak-sms.com/accounts/registration/" target="_blank">VakSms</a></div>
- GetSmsBaseUrl - домен GetSms. Меняйте только если не работает.
- GiveSmsBaseUrl - домен GiveSms. Меняйте только если не работает.
- OnlineSimBaseUrl - домен OnlineSim. Меняйте только если не работает.
- SmsActivateBaseUrl - домен SmsActivate. Меняйте только если не работает.
- VakSmsBaseUrl - домен VakSms. Меняйте только если не работает.
- Priority - система задания очередности использования смс сервисов.

Например по умолчанию установлено ["GetSms", "GiveSms", "OnlineSim", "SmsActivate", "VakSms"] - это значит, что первым будет использоваться GetSms, вторым GiveSms и т.д.
Можно поставить например ["OnlineSim", "SmsActivate", "VakSms"] - первым будет использоваться OnlineSim, вторым SmsActivate и т.д. Убранные сервисы использоваться не будут.
Переход на следующий сервис происходит если закончились номера или баланс.
Если вы не планируете пользваться каким либо сервисом - можно не указывать его api ключ.

- MailServer - Имя вашего email сервера. Например box.steamail.pro
- MailPort - Порт вашего email сервера.
- MailType - Протокол IMAP или POP3. Использование с POP3 не тестировалось.
