# OTGateway.Cli (`otg`)

CLI‑клиент для [OTGateway](https://github.com/Laxilef/OTGateway) — микроконтроллерного
устройства управления газовыми котлами по протоколу OpenTherm с встроенным HTTP/JSON API.

Утилита общается с устройством по HTTP (порт 80 по умолчанию), поддерживает необязательную
HTTP Basic авторизацию и умеет читать/менять настройки, опрашивать датчики, сканировать
Wi‑Fi, делать резервные копии, обновлять прошивку и файловую систему.

Собран на **.NET 8**, зависимости — только `Microsoft.Extensions.Logging[.Console]`.
Разбор аргументов — собственный, без сторонних библиотек.

## Сборка

```bash
cd OTGateway.Cli
dotnet build -c Release
dotnet run -- --help
```

Исполняемый файл называется `otg` (`AssemblyName=otg`):

```bash
dotnet publish -c Release -r win-x64 --self-contained false -o out
# затем out\otg.exe
```

## Глобальные параметры

| Параметр | Кратко | Значение |
|---|---|---|
| `--address` | `-a` | адрес устройства `ip` / `host` / `host:port` / `http://...`. **Обязателен** для большинства команд |
| `--port` | | HTTP‑порт (по умолчанию `80`). Игнорируется, если порт уже указан в `--address` |
| `--user` | | логин для Basic‑авторизации |
| `--password` | | пароль |
| `--token` | | учётные данные одной строкой `login:password` |
| `--timeout` | | таймаут HTTP‑запросов в секундах (по умолчанию `30`) |
| `--proxy` | | прокси для запросов: `socks5://host:port` или `http://host:port` (можно `socks5://логин:пароль@host:port`) |
| `--json` | | выводить сырой JSON вместо форматированного |
| `--yes` | `-y` | без интерактивных подтверждений (работает и после имени команды) |
| `--verbose` | `-v` | подробное логирование (уровень Debug) |
| `--help` | `-h` | справка |

## Переменные окружения

Если соответствующий флаг не задан, клиент подставит значение из переменной окружения
(переданные флаги всегда главнее):

| Переменная | Аналог флага |
|---|---|
| `OTG_ADDRESS` | `--address` |
| `OTG_USER` | `--user` |
| `OTG_PASSWORD` | `--password` |
| `OTG_TOKEN` | `--token login:password` (используется, если `OTG_USER`/`OTG_PASSWORD` не заданы) |

> ⚠️ При передаче учётных данных и открытом `http://`-адресе (не `https://`) в stderr печатается
> предупреждение: Basic-авторизация идёт незащищённым текстом — приемлемо только в доверенной LAN.

В командах `settings read` (без `--json`) строковые значения ключей, содержащих `password`
(например `mqtt.password`, `portal.password`), маскируются как `••••`. С `--json` вывод остаётся
сырым и не маскируется.

## Коды возврата

| Код | Смысл |
|---|---|
| `0` | успех |
| `1` | ошибка исполнения / ответа API / сети |
| `2` | ошибка использования (неверные аргументы, неизвестная команда) |

## Команды

| Команда | Назначение |
|---|---|
| `status [--watch <сек>]` | текущее состояние шлюза и котла (`GET /api/vars`): блоки подключения, отопления, ГВС и котла. `--watch N` — обновлять каждые N секунд |
| `settings read [--json]` | показать настройки деревом (`GET /api/settings`) |
| `settings write <file.json>` | отправить JSON‑патч настроек (`POST /api/settings`) |
| `settings set <path> <value>` | изменить одно поле минимальным вложенным патчем, напр. `heating.target 55` |
| `settings reset-fault` / `reset-diag` | сбросить аварию / диагностику котла (`POST /api/vars` с `actions`) |
| `sensor list [--json]` | таблица датчиков со значениями и единицами |
| `sensor set <id> <file.json>` | обновить датчик (`POST /api/sensor?id=<id>`) |
| `sensor enable <id>` / `disable <id>` | включить/выключить датчик |
| `network scan [--json]` | список точек доступа Wi‑Fi (происходит опрос до завершения скана — см. ниже) |
| `network set [flags]` | настройки сети: `--hostname`, `--use-dhcp`, `--static-ip`, `--gateway`, `--subnet`, `--dns` |
| `network info` | состояние подключения к Wi‑Fi |
| `backup save [file]` | скачать резервную копию настроек (по умолчанию `otgateway-backup-<timestamp>.json`) |
| `backup restore <file> [--yes]` | восстановить настройки из файла |
| `upgrade fw <file.bin>` | обновить прошивку (`POST /api/upgrade`, multipart) |
| `upgrade fs <file.bin>` | обновить раздел файловой системы (LittleFS) |
| `version` | сведения о версии прошивки и устройстве (build/chip) |
| `restart [--yes]` | перезагрузить устройство |

Статусы обновления: `2=OK`, `3=Prohibited`, `4=Aborted`, `5=Error on start`,
`6=Error on write`, `7=Error on finish`.

## Примеры

```bash
# Состояние котла с авторизацией
otg -a 192.168.1.50 --user admin --password s3cret status

# Непрерывный мониторинг раз в 2 секунды (Ctrl+C для выхода)
otg -a 192.168.1.50 status --watch 2

# Изменить целевую температуру отопления и ГВС
otg -a 192.168.1.50 settings set heating.target 55
otg -a 192.168.1.50 settings set dhw.enabled false

# Сбросить аварию котла
otg -a 192.168.1.50 settings reset-fault

# Просмотр и включение датчиков
otg -a 192.168.1.50 sensor list
otg -a 192.168.1.50 sensor enable 0x1

# Сканирование Wi-Fi и смена имени хоста
otg -a 192.168.1.50 network scan
otg -a 192.168.1.50 network set --hostname kotyol --use-dhcp true

# Резервная копия и восстановление
otg -a 192.168.1.50 backup save config.json
otg -a 192.168.1.50 backup restore config.json --yes

# Обновление прошивки и файловой системы
otg -a 192.168.1.50 upgrade fw firmware.bin
otg -a 192.168.1.50 upgrade fs data.bin

# Перезагрузка без подтверждения
otg -a 192.168.1.50 restart --yes
```

## Примечания по протоколу

- Все эндпоинты лежат под `http://<address>/api/…`.
- Имя поля `emergency.tresholdTime` содержит опечатку (`treshold`) — это имя поля API,
  сохранено намеренно.
- `POST /api/settings` и `POST /api/sensor` принимают частичный (partial) JSON. Поэтому
  `settings set` отправляет минимальный вложенный патч (`{"heating":{"target":55}}`) вместо всего
  раздела — соседние поля (в том числе секреты вроде `mqtt.password`) не перезаписываются.
- `GET /api/network/scan`: пока поиск сетей на устройстве не завершён, прошивка отвечает `404`
  (и запускает скан). Клиент считает `404`/`202`/пустой ответ признаком «ещё идёт» и опрашивает
  эндпоинт каждые 3 секунды, пока не придёт `200` со списком. Общий бюджет ожидания берётся из
  `--timeout`, а если флаг не задан — 60 секунд. Ошибки авторизации (`401`/`403`) и прочие
  HTTP-ошибки не молчатся, а пробрасываются сразу.
- `POST /api/backup/restore` принимает тела от 5 до 2536 байт (снаружи — `406`/`413`) и при
  успешном изменении настроек перезагружает устройство. Клиент проверяет размер и валидность JSON
  локально до отправки.

## Сборка и установка (Ubuntu)

### Вариант 1: `dotnet tool` (нужен .NET 8 SDK на машине установки)

Собрать пакет (можно с Windows, на Linux или macOS):

```bash
dotnet pack OTGateway.Cli -c Release -o ./build/nupkg
```

Установить глобально в систему:

```bash
sudo apt install dotnet-sdk-8.0   # если SDK ещё нет
dotnet tool install --global OTGateway.Cli --add-source ./build/nupkg
```

Команда станет доступна как `otg` (добавь `~/.dotnet/tools` в `PATH`, если подсказывает shell).
Обновление: `dotnet tool update --global OTGateway.Cli --add-source ./build/nupkg`.

### Вариант 2: автономный бинарник (рантайм на машине не нужен)

```bash
dotnet publish OTGateway.Cli -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true -o ./publish
sudo cp ./publish/otg /usr/local/bin/ && otg --help
```

Для ARM- машин (Raspberry Pi, Armbian) — `-r linux-arm64`. Публикация кросс-платформенная:
собирать под Linux можно и с Windows. Файл `otg` исполняемый, ~65 МБ (включает .NET-рантайм).

### Вариант 3: без самодостаточности (легче файл, нужен рантайм)

```bash
dotnet publish OTGateway.Cli -c Release -r linux-x64 --self-contained false -p:PublishSingleFile=true -o ./publish
sudo cp ./publish/otg /usr/local/bin/   # на машине должен быть установлен .NET 8 Runtime
```
