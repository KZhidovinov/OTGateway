# Project: OTGateway

## 🎯 Purpose
Proprietary software — a microcontroller-based device for controlling gas boilers via the **OpenTherm** protocol, with an embedded web UI, MQTT and Home Assistant integration. Current firmware version: **1.6.0**.

## 🔧 Firmware (C/C++)

| Component | Details |
|---|---|
| **Platform** | PlatformIO |
| **Framework** | Arduino (ESP32 builds use the [pioarduino](https://github.com/pioarduino/platform-espressif32) platform, Arduino core 3.x / ESP-IDF 5.x) |
| **Microcontrollers** | ESP8266 (`d1_mini`, `d1_mini_lite`, `d1_mini_pro`, `nodemcu_8266`) and ESP32 family (`nodemcu_32`, `nodemcu_32_160mhz`, `d1_mini32`, `s2_mini`, `s3_mini`, `c3_mini`, `esp32_c6`, `otthing`) |
| **Boiler interface** | OpenTherm protocol via GPIO (custom fork of `ihormelnyk/opentherm_library`, plus `lib/CustomOpenTherm`) |
| **Sensors** | DS18B20 (OneWireNg + DallasTemperature), NTC 10k, BLE sensors (NimBLE-Arduino, only on targets with `USE_BLE`) |
| **Protocols** | MQTT (`ArduinoMqttClient` + `lib/MqttWriter`), BLE, embedded HTTP/JSON server (`/api/*`, `lib/WebServerHandlers`, `lib/BufferedWebServer`), Telnet, serial debug |
| **Home Assistant** | auto-discovery via `lib/HomeAssistantHelper` + HA blueprints in the repo |
| **Scheduling** | `ESP8266Scheduler` (ESP8266) / `laxilef/ESP32Scheduler` (ESP32) |
| **PID / control** | GyverPID, equitherm algorithm, freeze protection |
| **Data storage** | FileData (settings), ArduinoJson (payloads), LittleFS (assets) |
| **Logging** | TinyLogger, GyverBlinker |

### Firmware source layout (`src/`)
`main.cpp` plus task headers: `MainTask`, `OpenThermTask`, `MqttTask`, `PortalTask`, `SensorsTask`, `RegulatorTask`; support headers: `Settings`, `Sensors`, `HaHelper`, `CrashRecorder`, `defines`, `utils`, `strings`.

### Local libraries (`lib/`)
`BufferedWebServer`, `CustomOpenTherm`, `HomeAssistantHelper`, `MqttWriter`, `NetworkUtils`, `WebServerHandlers`.

## 🖥️ Web interface (embedded UI)

| Component | Details |
|---|---|
| **Build** | Gulp (`gulpfile.js`) → minified + gzipped assets into `data/` |
| **CSS** | Pico.css, Iconly, PostCSS + cssnano |
| **JS** | Vanilla JS (i18n, utils), Chart.js |
| **Localization** | JSON locale files in `src_data/locales/`: `en`, `ru`, `it`, `nl`, `cn` |
| **Storage** | LittleFS on chip (uploaded separately from firmware) |

## 🧰 CLI client (C#/.NET)

| Component | Details |
|---|---|
| **Project** | `OTGateway.Cli/` — `otg` tool for the gateway's HTTP/JSON API (.NET 8, no external arg-parsing deps) |
| **Tests** | `OTGateway.Cli.Tests/` — xUnit suite |
| **Solution** | `OTGateway.sln` (CLI + tests only; firmware is built by PlatformIO) |
| **Features** | status/settings/sensor/network/backup/upgrade commands, Basic auth, proxy support, partial settings patches |
| **Docs** | `OTGateway.Cli/README.md` (Russian) |

## 🛠️ Tools and infrastructure

| Tool | Purpose |
|---|---|
| PlatformIO | firmware compile & flash (`pio run -e <env>`) |
| Gulp | web asset build (minification, gzip) |
| PVS-Studio | optional C/C++ static analysis (`check_tool` in `platformio.ini`, disabled by default) |
| GitHub Actions | repo automation (see `.github/`) |

## 📂 Structure

| Directory | Purpose |
|---|---|
| `src/` | firmware C/C++ sources |
| `lib/` | local PlatformIO libraries |
| `src_data/` | web UI sources (HTML, CSS, JS, locales, fonts, images) |
| `data/` | built static assets for LittleFS |
| `tools/` | PlatformIO post-build scripts (`build.py`, `esp32.py`) |
| `OTGateway.Cli/`, `OTGateway.Cli.Tests/` | .NET CLI client and tests |
| `build/` | build artifacts |
| `assets/` | README/poster images |

## ⚙️ Configuration

- `platformio.ini` — main config: per-board envs inherit `[esp8266_defaults]` / `[esp32_defaults]`; per-board default GPIO pins are set via `DEFAULT_*_GPIO` build flags.
- `secrets.default.ini` — factory defaults (Wi-Fi, MQTT, hostname, portal credentials, log level), injected as build flags via `extra_configs`; switch to `secrets.ini` for personal defaults.
- `esp32_partitions.csv` — ESP32 partition table.
- `sdkconfig.defaults` — ESP-IDF options (used by the `esp32_c6` env, `framework = arduino, espidf`).

## 💡 Context for AI assistants

- Firmware and UI are decoupled: `src/` is embedded C++, `src_data/` are web assets; assets are bundled into `data/` by Gulp and flashed to LittleFS.
- The CLI (`OTGateway.Cli`) is a separate .NET solution — do not mix it with PlatformIO builds.
- Some API field names contain intentional typos kept for compatibility (e.g. `emergency.tresholdTime`); see `OTGateway.Cli/README.md`.
- UI code and docs are largely Russian; locale strings live in `src_data/locales/`.
- Wiki and repo: `https://github.com/Laxilef/OTGateway`
- Telegram support channel: `t.me/otgateway`
