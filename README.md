# 🚀 GameOptimizer — Максимальный FPS для Windows

[![Version](https://img.shields.io/badge/version-3.0-brightgreen)](https://github.com/zxcillaura/GameOptimizer/releases)
[![C#](https://img.shields.io/badge/C%23-100%25-blue)](https://github.com/zxcillaura/GameOptimizer)
[![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D6)](https://github.com/zxcillaura/GameOptimizer)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

---

## 📖 О проекте

**GameOptimizer** — мощная утилита для оптимизации Windows 10/11 под игры. Повышает FPS, снижает задержки (input lag), отключает ресурсоемкие службы и настраивает сеть.

> 🎯 **Цель:** Максимальная производительность в играх с минимальными усилиями.

---

## ✨ Возможности

| Функция | Описание |
|---------|----------|
| ⚡ **2 режима оптимизации** | «Жесткая» (макс FPS) и «Мягкая» (безопасная) |
| 🌐 **DNS Jumper** | 30+ DNS серверов с тестом пинга |
| 🛡️ **Античиты** | Управление FACEIT (Hyper-V/VBS) и Riot Vanguard (TPM/Secure Boot) |
| 🧹 **Очистка** | Кэш, Prefetch, временные файлы, шейдерный кэш |
| 📊 **Диагностика** | Характеристики ПК, статус античитов |
| 🎮 **Игры** | Оптимизация CS2, Valorant |

---

## 📥 Скачать

| Версия | Дата | Скачать | Изменения |
|--------|------|---------|-----------|
| **v3.0** (🔥 Latest) | 19.07.2026 | [⬇️ Скачать](https://github.com/zxcillaura/GameOptimizer/releases/latest) | Новая панель диагностики, улучшенная оптимизация |
| v2.0 | 15.07.2026 | [⬇️ Скачать](https://github.com/zxcillaura/GameOptimizer/releases/tag/v2.0) | Добавлен DNS Jumper, управление античитами |
| v1.0 | 11.07.2026 | [⬇️ Скачать](https://github.com/zxcillaura/GameOptimizer/releases/tag/v1.0) | Первый релиз |

---

## 🛠️ Установка

1. **Скачай** последнюю версию из [Releases](https://github.com/zxcillaura/GameOptimizer/releases)
2. **Запусти** `GameOptimizer.exe` **от имени администратора** (ПКМ → Запуск от имени администратора)
3. **Выбери** профиль оптимизации и нажми «Применить»

> ⚠️ **Важно:** Перед применением твиков создайте точку восстановления системы!

---

## 📂 Структура проекта
GameOptimizer/
├── 📁 src/
│ ├── 📁 v1.0/ # Исходники первой версии
│ ├── 📁 v2.0/ # Исходники второй версии
│ └── 📁 v3.0/ # Исходники третьей версии (актуальная)
├── 📁 releases/ # Скомпилированные .exe файлы
├── 📄 README.md # Описание проекта
├── 📄 CHANGELOG.md # История изменений
└── 📄 .gitignore # Игнорируемые файлы

---

## 🔧 Сборка из исходников

```bash
# Установи .NET SDK (https://dotnet.microsoft.com/download)
dotnet restore
dotnet build -c Release
⚠️ Предупреждения
🛡️ Антивирусы могут ложно срабатывать — это нормально для программ, изменяющих реестр

🔓 Исходный код полностью открыт и безопасен

🔄 Все изменения обратимы (есть кнопка восстановления)

📝 История изменений
Подробный список в CHANGELOG.md

📞 Контакты
📧 Email: godkotbot@gmail.com

🐙 GitHub: zxcillaura

⭐ Если проект полезен — поставь звезду! ⭐
