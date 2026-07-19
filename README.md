# 🚀 GameOptimizer — Максимальный FPS для Windows

[![Version](https://img.shields.io/badge/version-3.0-brightgreen)](https://github.com/zxcillaura/GameOptimizer/releases)
[![C#](https://img.shields.io/badge/C%23-100%25-blue)](https://github.com/zxcillaura/GameOptimizer)
[![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D6)](https://github.com/zxcillaura/GameOptimizer)
[![Downloads](https://img.shields.io/github/downloads/zxcillaura/GameOptimizer/total)](https://github.com/zxcillaura/GameOptimizer/releases)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

## 📖 О проекте

**GameOptimizer** — мощная утилита для оптимизации Windows 10/11 под игры. Повышает FPS, снижает задержки (input lag), отключает ресурсоемкие службы и настраивает сеть.

> 🎯 **Цель:** Максимальная производительность в играх с минимальными усилиями.

---

## ✨ Возможности

| Функция | Описание |
|---------|----------|
| ⚡ **Режимы оптимизации** | Жесткая (макс FPS) и Мягкая (безопасная) |
| 🌐 **DNS Jumper** | 30+ DNS серверов с тестом пинга |
| 🛡️ **Управление античитами** | FACEIT (Hyper-V/VBS) и Riot Vanguard (TPM/Secure Boot) |
| 🧹 **Очистка системы** | Кэш, Prefetch, временные файлы, шейдерный кэш |
| 📊 **Диагностика** | Информация о ПК и статус сервисов |
| 🎮 **Профили игр** | Готовая оптимизация для CS2, Valorant, Apex |
| 📥 **Профили настроек** | Сохраняйте и загружайте свои конфигурации |
| 🔄 **Система отката** | Откатите любые изменения одной кнопкой |

---

## 📥 Скачать

### Последние версии

| Версия | Дата | Скачать | Что нового |
|--------|------|---------|-----------|
| **v3.0** 🔥 | 19.07.2026 | [⬇️ Скачать](https://github.com/zxcillaura/GameOptimizer/releases/latest) | Новая панель диагностики, Fluent Design UI |
| v2.0 | 15.07.2026 | [⬇️ Скачать](https://github.com/zxcillaura/GameOptimizer/releases/tag/v2.0) | DNS Jumper, Управление античитами |
| v1.0 | 11.07.2026 | [⬇️ Скачать](https://github.com/zxcillaura/GameOptimizer/releases/tag/v1.0) | Первый релиз |

---

## 🛠️ Установка

### Способ 1: Готовый .exe (Рекомендуется)

1. **Скачайте** последнюю версию из [Releases](https://github.com/zxcillaura/GameOptimizer/releases)
2. **Запустите** `GameOptimizer.exe` **от имени администратора**
   - Кликните правой кнопкой → «Запуск от имени администратора»
3. **Выберите** профиль и нажмите «Применить»

> ⚠️ **Важно:** Перед применением твиков создайте точку восстановления системы!

### Способ 2: Сборка из исходников

```bash
# Установите .NET SDK 8.0+
# https://dotnet.microsoft.com/download

# Клонируйте репозиторий
git clone https://github.com/zxcillaura/GameOptimizer.git
cd GameOptimizer

# Способ 1: Используйте батник (Windows)
build.bat

# Способ 2: Или вручную
cd GameOptimizer
dotnet restore
dotnet build -c Release
```

---

## 📂 Структура проекта

```
GameOptimizer/
├── 📁 Core/                          # Основная логика
│   ├── Tweaks/                       # Система твиков
│   │   ├── TweakBase.cs              # Базовый класс твика
│   │   ├── RegistryTweak.cs          # Твики реестра
│   │   └── ServiceTweak.cs           # Твики сервисов
│   ├── Themes/                       # Система тем (Fluent Design)
│   │   ├── Theme.cs                  # Базовая тема
│   │   ├── DarkTheme.cs              # Темная тема
│   │   └── LightTheme.cs             # Светлая тема
│   ├── Profiles/                     # Система профилей
│   │   ├── ProfileManager.cs         # Менеджер профилей
│   │   └── Profile.cs                # Структура профиля
│   ├── Preview/                      # Предпросмотр изменений
│   │   └── TweakPreview.cs           # Менеджер предпросмотра
│   └── Services/                     # Системные сервисы
│       ├── RegistryService.cs        # Работа с реестром
│       ├── DNSService.cs             # DNS операции
│       └── SystemService.cs          # Информация о системе
│
├── 📁 UI/                            # Пользовательский интерфейс (WinForms)
│   ├── MainForm.cs                   # Главное окно
│   ├── MainForm.Designer.cs          # Дизайнер формы
│   └── UserControls/                 # Компоненты UI
│       ├── DashboardControl.cs       # Панель диагностики
│       ├── SystemControl.cs          # Вкладка System
│       ├── NetworkControl.cs         # Вкладка Network (DNS)
│       ├── GamesControl.cs           # Вкладка Games
│       ├── FaceitControl.cs          # Вкладка FACEIT
│       ├── VanguardControl.cs        # Вкладка Vanguard
│       └── CleanerControl.cs         # Вкладка Очистка
│
├── 📁 Utils/                         # Утилиты и помощники
│   ├── RoundedPanel.cs               # Компонент с закруглеными углами
│   ├── UIHelpers.cs                  # Функции для UI
│   └── Logger.cs                     # Логирование операций
│
├── 📁 Resources/                     # Ресурсы
│   ├── tweaks.json                   # База твиков (JSON)
│   ├── dns-servers.json              # Список DNS серверов
│   └── icons/                        # Иконки приложения
│
├── 📁 Properties/                    # Свойства проекта
│   └── AssemblyInfo.cs               # Информация о версии
│
├── Program.cs                        # Точка входа приложения
├── GameOptimizer.csproj              # Файл проекта
├── build.bat                         # Батник для сборки
├── quick-build.bat                   # Быстрая сборка
└── run.bat                           # Запуск приложения
```

### Описание папок

- **Core/** — вся бизнес-логика: твики, профили, управление системой
- **UI/** — WinForms интерфейс с 7 вкладками функциональности  
- **Utils/** — вспомогательные компоненты и функции
- **Resources/** — данные в JSON (твики, DNS серверы, иконки)

---

## 🎮 Как использовать

### Базовое использование

1. **Откройте** GameOptimizer.exe от администратора
2. **Выберите** один из встроенных профилей:
   - Competitive Gaming — максимум FPS (CS2, Valorant, Apex)
   - Single Player — баланс для одиночных игр
   - Streaming — оптимизация для трансляции
   - System Cleanup — очистка системы
3. **Нажмите** «Применить»
4. **Перезагрузитесь** (рекомендуется)

### Специальные операции

#### DNS Jumper
- Вкладка Network → выберите DNS сервер из списка
- Автоматический тест пинга
- Применяется сразу без перезагрузки

#### Управление Античитами
- **FACEIT**: Отключение Hyper-V и VBS
- **Vanguard**: Отключение TPM и Secure Boot

#### Очистка системы
- Удаление файлов кэша
- Очистка Prefetch
- Удаление временных файлов
- Очистка шейдерного кэша

---

## ⚙️ Продвинутые настройки

### Кастомный профиль

1. Выберите нужные твики вручную
2. Нажмите «Сохранить профиль»
3. Назовите свой профиль
4. Используйте его в будущем

### Предпросмотр изменений

Перед применением любого профиля:
- Зелёные твики — безопасные
- Жёлтые твики — требуют перезагрузки
- Красные твики — опасные (требуют бэкапа)

### Откат изменений

Все изменения можно откатить:
- Кнопка «Откат» (Undo) в меню
- Автоматическое создание резервной копии реестра

---

## ⚠️ Важные предупреждения

### Безопасность

- 🛡️ **Антивирусы** могут ложно срабатывать — это нормально для ПО, работающего с реестром
- 🔓 **Исходный код** полностью открыт на GitHub, вы можете проверить каждую строку
- 🔄 **Все изменения обратимы** — есть полная система отката

### Рекомендации

- ⚠️ Создайте **точку восстановления** перед применением
- 📸 Если что-то сломалось, используйте **откат** или **System Restore**
- 📝 Прочитайте описание каждого твика перед применением
- 🔴 Красные твики применяйте только если знаете что делаете

---

## 📝 Скриншоты

### Главное окно

```
┌─────────────────────────────────────────┐
│  GameOptimizer v3.0                     │
├──────┬──────────────────────────────────┤
│Panel │ 🎮 Competitive Gaming             │
│Sys  │ Максимум FPS для конкурентных игр │
│Net  │                                    │
│Game │ ✓ Отключить Prefetch              │
│...  │ ✓ Очистить кэш                    │
│     │ ✓ Отключить Telemetry            │
│     │                                    │
│     │ [Предпросмотр] [Применить]        │
└──────┴──────────────────────────────────┘
```

---

## 🔧 Разработка

### Требования

- Windows 10/11
- .NET SDK 8.0+
- Visual Studio 2022 или VS Code

### Сборка проекта

```bash
# Через батник (рекомендуется)
build.bat

# Или вручную
cd GameOptimizer
dotnet restore
dotnet build -c Release
```

### Структура твика (пример)

```csharp
public class DisablePrefetchTweak : RegistryTweak
{
    public override string Name => "Отключить Prefetch";
    public override string Description => "Отключает сервис Prefetch для ускорения загрузки";
    public override string Category => "System";
    
    public override void Apply()
    {
        // HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management
        Registry.SetValue("EnablePrefetcher", 0);
    }
    
    public override void Revert()
    {
        Registry.SetValue("EnablePrefetcher", 3);
    }
}
```

---

## 🤝 Участие в разработке

Вы можете помочь улучшить проект:

1. **Создайте Issue** с описанием проблемы или идеи
2. **Сделайте Fork** репозитория
3. **Создайте Pull Request** с улучшениями
4. **Обсудите** идеи в Discussions

---

## 📊 Примеры результатов

**До оптимизации:**
- FPS: ~120 fps (Valorant на Medium)
- Ping: 25ms
- Система: множество фоновых процессов

**После GameOptimizer:**
- FPS: ~200+ fps (Valorant на Medium)
- Ping: 15ms
- Система: минимум фона, максимум ресурсов для игр

*Результаты зависят от характеристик вашего ПК*

---

## 📄 Лицензия

Проект распространяется под лицензией **MIT** — вы можете использовать его свободно в личных и коммерческих целях.

---

## 📞 Контакты

- **Email:** godkotbot@gmail.com
- **GitHub:** [@zxcillaura](https://github.com/zxcillaura)
- **Discord:** zxcillaura

---

## ❤️ Благодарности

Спасибо всем, кто помогал в разработке, тестировании и улучшении GameOptimizer!

Если проект полезен — **поставьте звезду** ⭐

---

## 📚 Дополнительно

- [CHANGELOG.md](CHANGELOG.md) — история изменений всех версий
- [CONTRIBUTING.md](CONTRIBUTING.md) — как участвовать в разработке  
- [ARCHITECTURE.md](ARCHITECTURE.md) — архитектура проекта для разработчиков
- [SECURITY.md](SECURITY.md) — политика безопасности

---

**Made with ❤️ by zxcillaura**

Last updated: 19.07.2026
