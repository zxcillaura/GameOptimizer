# GAMEOPTIMIZ 1.0 — ПОЛНЫЙ КОНТЕКСТ + ФАЙНАЛЬНЫЙ ПЛАН
_Этот документ самодостаточен: кто угодно (человек или ИИ), прочитав его, должен сразу
понимать, что за проект, где всё лежит, как собирается, что сделано, что сломано и что делать дальше.
Обновлено: 09.10.2026, v2: доведено до максимума. Сверено с реальным кодом и исследованиями._

---

## A. О ПОЛЬЗОВАТЕЛЕ
- **Имя/ник:** zxcillaura (Windows-юзер `C:\Users\zxcillaura`, машина `HOME-PC`). Обращаться неформально («дружище»), отвечать по-русски.
- **Кто это:** автор проекта, геймер (CS2, Dota 2, FACEIT/Vanguard в контексте), хочет «серьёзный инструмент, а не прога от вайбкодера».
- **Главные требования:**
  1. **Оптимизация — лучшая:** ПК, FPS, задержка (импут-лаг мыши/клавы), интернет/пинг/джиттер. Брать лучшее из интернета, проверять применимость к железу.
  2. **Интерфейс — «бомба-пушка»:** тёмный, красивый, ПОНЯТНЫЙ. Каждый раздел и каждый твик подписан простым человеческим языком. Пользователь с проблемой в ПК не должен «ебалить себе голову»: выбирает категорию (только ПК / только мышь / только сеть) и применяет.
  3. **Кросс-ПК:** программа должна идеально работать не только на его машине, а на любом ПК (детект железа, неприменимые твики прячутся с объяснением).
  4. Безопасность: бэкапы, точки восстановления, обратимость, «вернуть к заводским».
- **Брендинг:** «Optimization by zxcillaura». Подзаголовок логотипа: `1.0 • by zxcillaura`.

## B. О ПРОЕКТЕ
**GameOptimizer (GAMEOPTIMIZ)** — desktop-оптимизатор для игр на Windows 10/11:
твики реестра/сервисов/задач/сети/питания, профили применения с бэкапом и откатом,
глубокая очистка мусора, мониторинг (FPS/DPC/температуры), бенчмарк, анти-чит диагностика,
конфиги для CS2/Dota2, CLI-режим.

**Позиционирование:** не «ещё один тюнер», а инструмент уровня pro: кросс-железо,
честные риски (Safe/Caution/Risk), подписи простым языком, полная обратимость.

**Варианты запуска:**
- GUI (WinForms, требуется администратор, single-instance).
- CLI: `GAMEOPTIMIZ1.0.exe <команда>` — команды: `help`, `apply`, `revert`, `scan`, `status`,
  `tweaks` (дамп каталога), `bench`, `latency`, `purge`. (`/doctor` — в планах, M9.)

## C. СТЕК И СБОРКА
- **C# / .NET 8.0-windows, WinForms** (не WPF). Single-file self-contained, win-x64 (~63 МБ).
- **SDK:** `E:\Programs\dotnet\dotnet.exe` (если есть), иначе системный `dotnet` (см. `build.bat`).
- **Сборка (publish):**
  `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true`
- **Контроль качества:** сборка обязана давать **0 ошибок / 0 предупреждений**.
- **Манифест:** `app.manifest` — `requireAdministrator`. Мутекс: `Global\GAMEOPTIMIZ1.0_SingleInstance`.
- **Версии в csproj:** `AssemblyName=GAMEOPTIMIZ1.0`, `Version 1.0.0`, Product/Title «GAMEOPTIMIZ 1.0».
- **Окружение:** Windows, **PowerShell 5.1** (нет `&&`, использовать `;`; UTF-8 явно).
  Binaries чистить только в **Корзину**, никогда `rm`/`Remove-Item -Recurse`.

## D. КАРТА КОДА (C:\Users\zxcillaura\MY PROJECT\GAMEOPTIMIZ1.0)
```
Program.cs (126)            вход: CLI или GUI, mutex, проверка прав
Cli\CliRunner.cs (360)      CLI-команды (help/apply/revert/scan/status/tweaks/bench/latency/purge)
app.manifest, AppIcon.ico   манифест админа, иконка
build.bat (27)              publish single-file
────────── Core ──────────
Core\Tweaks\TweakItem.cs (221)      БАЗА твика: Id/Title/Desc/Group/Note/Risk(Safe|Caution|Risk),
                                    Apply/Revert/Backup, NotForReason (причина неприменимости);
                                    в этом же файле class TweakRunner (строка 152) — движок
                                    применения списков твиков с бэкапом
Core\Tweaks\TweakCatalog.cs (1161)  33 статических твика (power/gfx/gpu/lat/mk/net/perf/risk/game)
Core\Tweaks\TweakCatalogSystem.cs (250) 21 служба + 13 задач, генерируются из ServiceTable/TaskTable
Core\Tweaks\SystemTweaks.cs (356)   низкоуровневые хелперы: powercfg, netsh, реестр, IsAdmin()
Core\Tweaks\TcpParams.cs (105)      наборы TCP-флагов (netsh int tcp set global ...)
Core\Tweaks\StateCache.cs (79)      кэш состояний твиков
Core\Hardware\HardwareScanner.cs (433)  кросс-железо: вендор CPU/GPU, ноут/десктоп, SSD/NVMe,
                                    RAM канал, Windows build → RigReport
Core\Health\HealthScore.cs (104)    оценка 0–100 + буква + Reasons (топ-ограничители)
Core\Profiles\ProfileRunner.cs (144)  НОВЫЕ профили: Categories (ПК/Сеть/Мышь-клава/Система)
                                    × ProfileKind (Factory/Balanced/Extreme), маппинг Group→категория
Core\Profiles\ProfileEngine.cs (141)  СТАРЫЕ профили (Safe/Balanced/Extreme) — ещё используются CLI/Домашней
Core\Cleanup\DeepCleaner.cs (201)   НОВЫЙ очиститель: CleanTarget{Key,Title,Desc,Measure,Apply,
                                    NeedsConfirm,DefaultOn}, Targets()/Preview()/Clean() —
                                    ⚠ СЕЙЧАС ТОЛЬКО 4 ЦЕЛИ (дефект, см. G)
Core\Cleanup\SafeCleanupService.cs (39)   хелпер безопасной очистки
Core\SystemCleaner.cs (163)       СТАРЫЙ очиститель 0.9 (temp/prefetch/update) — ещё живёт в CLI
Core\Backup\BackupManager.cs (427) снапшоты: Meta (значения реестра/сервисов/задач), latest.json;
                                    ⚠ Root = %ProgramData%\GAMEOPTIMIZv7 — LEGACY-ИМЯ, НАМЕРЕННО
                                    (бэкапы общие между версиями, не переименовывать без миграции!)
Core\Latency\LatencyKeeper.cs (173) таймер 0.5 мс, purge standby-памяти, Status
Core\Games\GameConfigWriter.cs (225)  CS2/Dota2 autoexec-конфиги
Core\Games\GameSession.cs (214)       слежение за запущенной игрой
Core\Games\SteamLaunchOptions.cs (269)  параметры запуска Steam
Core\Monitor\SystemMonitor.cs (257)   real-time: FPS/DPC/CPU/RAM/темп
Core\Bench\BenchmarkStore.cs (265)    бенчмарк и история
Core\Diagnostics\ADDiagnostic.cs (217) диагностика анти-читов (Faceit/Vanguard)
Core\Diagnostics\NetworkDiagnostics.cs (49)  пинг/jitter/loss
Core\Diagnostics\SystemScanner.cs (87)       скан состояния
Core\AntiCheatMonitor.cs (15)       монитор анти-читов
Core\OptimizerEngine.cs (281), AdvancedOptimizer.cs (266), NetworkOptimizer.cs (131)  СТАРЫЕ движки 0.9
Core\RegistryManager.cs (140), Core\ServiceManager.cs (79), Core\WinApi.cs (51)
Core\Themes\Theme.cs (139)          дизайн-токены (цвета, шрифты)
────────── UI ──────────
UI\MainForm.cs (460)         сайдбар + 9 страниц, логотип, статус-бар
UI\Controls\  CardPanel, FlatButton, IconPainter, LiveChart, RingGauge, Toast, TweakRow
UI\UserControls\
  HomeControl.cs (364)       Главная: HealthScore-кольцо + железо + быстрые действия
  AutoControl.cs (254)       ОПТИМИЗАЦИЯ: ProfileRunner, профили × категории (переработан)
  TweakCenterControl.cs (473) ЦЕНТР ТВИКОВ: группы, поиск (есть), риск-бейджи (есть), подпись каждого
  NetworkControl.cs (334)    СЕТЬ: адаптеры, DNS-выпадашка (Cloudflare/Google/AdGuard/МТС),
                             пинг/jitter/loss; ⚠ НЕТ «до/после»
  CleanerControl.cs (208)    ОЧИСТИТЕЛЬ: DeepCleaner, предпросмотр «освободится ~X ГБ» (переработан)
  GamesControl.cs (259)      ИГРЫ: профили под CS2/Dota2
  FaceitControl.cs (176), VanguardControl.cs (178), AntiCheatControl.cs (82)  анти-читы
  MonitorControl.cs (224)    МОНИТОР
  BenchControl.cs (296)      БЕНЧМАРК
UI\GaugeControl.cs, Sparkline.cs, RoundedPanel.cs, DarkMode.cs, UIHelpers.cs
Utils\  AdminChecker, GraphicsExtensions, ProcessRunner
```

## E. КЛЮЧЕВЫЕ МЕХАНИЗМЫ
1. **Твик:** наследник `TweakItem` с `Apply()`, `Revert()`, `Backup()` (значения до/после в Meta
   снапшота), `Risk`, `NotForReason` (→ твик прячется с подписью «почему»). `TweakRunner` применяет
   списки: бэкап → apply → снапшот в BackupManager.
2. **Профили (новые, 1.0):** `ProfileRunner.Run(profile, categories[], withRestorePoint, log)`.
   Категории: `ПК / Сеть / Мышь и клава / Система`; каждый Group каталога замаплен в категорию.
   Factory = revert по категориям; Balanced = Safe+Caution; Extreme = всё включая Risk.
3. **Бэкап:** `BackupManager.Begin(profile)` → снапшот → `backups/latest.json` + Meta. Откат —
   `Revert` по снапшоту. Точка восстановления Windows создаётся перед Баланс/Экстрим.
4. **Очиститель:** `DeepCleaner.Preview()` считает размеры → UI показывает «~X ГБ» → `Clean(targets)`.
   `NeedsConfirm` (WinSxS, Windows.old) — отдельное подтверждение.
5. **Железо/здорожье:** `HardwareScanner` → `RigReport` → `HealthScore.Evaluate` → 0-100 + Reasons.
6. **CLI** = та же логика без UI (проверка на чужих машинах, CI).

## F. ИСТОРИЯ ВЕРСИЙ
- **0.1–0.7** (ранее v1–v7, переименованы 09.10.2026): ранние итерации.
- **0.8–0.9:** зрелость: 88 детектируемых твиков, 0 предупреждений, релизы в `...PROJECTreliz`
  с `WHATSNEW-0.9.txt`, аудит CA-предупреждений закрыт.
- **1.0 (ТЕКУЩАЯ):** план «ИМБА». Готов: движок железа+HealthScore, новые твики (mk-группа,
  MSI-mode, NTFS, Power Throttling, offload/TEECP, mitigations, memory compression),
  ProfileRunner с категориями, DeepCleaner (частичный!), UI «Профили»+«Очиститель» переработаны,
  брендинг 1.0.0, сборка 0/0, релиз выложен, CLI /tweaks exit 0 (88 целей, 51 включено).

## G. ТЕКУЩЕЕ СОСТОЯНИЕ И ИЗВЕСТНЫЕ ДЕФЕКТЫ (проверено 09.10.2026)
**ДЕФЕКТЫ (исправить в первую очередь):**
1. ⚠ **DeepCleaner = 4 цели** (dns, recycle, winsxs, winold). По сравнению с 0.9 **потеряны**
   temp / windows-temp / prefetch / update-cache (они живут только в старом SystemCleaner и не
   видны в UI!). Целевое состояние — 16 целей (таблица K).
2. Нет отдельной вкладки **«Мышь и клава»** (твики мыши доступны только в Центре твиков и Профилях).
3. Вкладка **Сеть**: нет теста «до/после» с вердиктом.
4. **Главная**: нет «ТОП-3 ограничителя» с кнопкой «Исправить» (данные есть в HealthScore.Reasons).
5. CLI: нет `/doctor` (самодиагностика для чужих машин).
6. Каталог: нет твиков M1/M2 из раздела I (HAGS, Game Mode, Fast Startup, SvcHostSplit, анимации,
   PCIe, USB suspend, QoS, EEE, MTU, Filter/Sticky keys, KeyboardResponse, BT-off).

**НЕ ДЕФЕКТЫ (не трогать):**
- `ProfileEngine` (старый) и `SystemCleaner` (старый) — используются CLI и Домашней; коэзист.
- `%ProgramData%\GAMEOPTIMIZv7` — legacy-имя намеренное (общие бэкапы между версиями).
- 21 служба и 13 задач в каталоге — это нормально (ServiceTable/TaskTable генерация).

## H. ПРАВИЛА РАЗРАБОТКИ (обязательные)
1. Весь пользовательский текст — **по-русски**, простым языком. Технические id — английский
   (кеbab-case: `mk-filter-keys-off`).
2. У каждого твика: `Title`, `Desc` (что делает простыми словами), `Risk`, `Revert`, `NotForReason`.
3. **Risk=Risk** — только в профиле Экстрим и в Центре твиков, никогда в Балансе.
4. Перед применением профиля — бэкап + (Баланс/Экстрим) точка восстановления. Всё обратимо.
5. Никаких хардкодов путей/железа: `%TEMP%`, `Environment.SpecialFolder`, детект через HardwareScanner.
6. Каждая новая цель очистки — только Safe-пути; данные пользователя (cookies/пароли/профили)
   не трогаем никогда.
7. Сборка: **0 ошибок, 0 предупреждений** — критерий завершения каждой вёрстки.
8. Проверка: CLI `/tweaks` exit 0 + запуск GUI. Релиз: exe →
   `C:\Users\zxcillaura\MY PROJECTreliz\GAMEOPTIMIZ1.0reliz\` + `WHATSNEW-1.0.txt`.
9. Удалять файлы только в Корзину. Работать из `GAMEOPTIMIZ1.0` (не из папок 0.9 и ниже — они архив).

## I. ОСТАЛЬНОЕ — М1–M9 (порядок действий)
- **M1 (ПК-твики, 9 шт.):**
  | id | Что | Механизм | Риск |
  |---|---|---|---|
  | perf-game-mode | Игровой режим Windows ON | HKCU\Software\Microsoft\GameBar AutoGamesense=1 | Safe |
  | perf-svchost-split | SvcHostSplitThresholdInKB=256 | HKLM\SYSTEM\CurrentControlSet\Control | Safe |
  | gpu-hags | HAGS — аппаратное планирование GPU (двусторонний тумблер) | HKLM\...\GraphicsDrivers HwSchMode=2/0. Исследования: универсальной пользы НЕТ, в Win11 обычно ON — делаем тумблер с подсказкой «если микрофризы — попробуй OFF, зобенчмаркни до/после» | Caution |
  | power-fast-startup-off | Быстрый запуск OFF | HKLM\...\Power FastStartupEnabled=0 | Safe |
  | power-boost-on | Буст ЦП ON | powercfg ProcessorPerformanceBoostMode=1 | Safe |
  | power-pcie-aspm-off | PCIe Link State Power Mgmt Off | powercfg 501a4d13... sub 0 | Safe |
  | power-usb-suspend-off | USB selective suspend Off | powercfg 238c1ec5...  | Safe |
  | ui-anim-off | Анимации окон OFF | HKCU\...\Window Shell\Animations AnimEnable=0 | Safe |
  | ui-transparency-off | Полупрозрачность OFF | HKCU\...\Explorer EnableTransparency=0 | Safe |
  Плюс: **net-qos-gaming** (QoS/ToS для игр), **net-eee-off** (energyconversiomode=disabled),
  проверить TcpParams: входят ли chimney/dca/netdma/timesync/transmit_pacing в net-offload-off —
  недостающее вынести в **net-full-tcp-off**; **net-mtu-auto**.
- **M2 (мышь/клава, 4 шт.):**
  | id | Что | Механизм |
  |---|---|---|
  | mk-filter-keys-off | Фильтрация клавиш OFF | HKCU\Control Panel\Accessibility\Keyboard Response\FilterKeys Flags=510 + UserConfigFilterKeys=0 |
  | mk-sticky-keys-off | Прилипающие клавиши OFF | \StickyKeys Flags=510 + UserConfigStickyKeys=0 |
  | mk-kbd-response | Клавиатура: задержка 0, скорость max | HKCU\Control Panel\Keyboard: KeyboardResponse=1, KeyboardDelay=0, KeyboardSpeed=31 |
  | mk-bt-off | Bluetooth-радио OFF (BT-мышь = +500 мс) | Set-NetAdapter/Disable-PnpDevice, NotFor: нет BT |
  Проверить: mk-accel-1to1 (MarkC-fix) уже пишет MouseSpeed=0/MouseThreshold1/2=0 — если нет, дописать.
- **M3 (Очиститель 4→16 целей):** таблицу целей см. раздел K.
- **M4 (вкладка «Мышь и клава»):** крупные тумблеры по твикам M2+mk-группы; **тест задержки
  ввода** (клик→обработка, мс, медиана за N кликов); **polling rate** из драйвера мыши (SetupAPI,
  если доступно) + подсказка «поставь 1000Hz в софте мыши»; блок «что такое импут-лаг» простыми
  словами; кнопка «Применить всё для импут-лага».
- **M5 (Сеть):** DNS-выпадашка уже есть — проверить что применяет; **тест 2.0**: замер ДО →
  применить категории Сети → замер ПОСЛЕ → вердикт «пинг 24→19 мс (−21%), стало лучше ✓»;
  **поэтапный тест**: пинг до роутера (LAN) → DNS → интернет: показывает где проблема
  (локально: кабель/адаптер/Wi-Fi или провайдер); **скорость линка**: адаптер на 100 Мбит —
  подсказка (кабель/порт/настройка); MTU-проверка; «Сброс сети» с предупреждением про перезагрузку.
- **M6 (Главная):** блок «ТОП-3 ОГРАНИЧИТЕЛЯ» из HealthScore.Reasons: имя проблемы простым языком +
  кнопка «ИСПРАВИТЬ» (применяет конкретный твик или переводит во вкладку); карточки полного железа
  (CPU/ядра/нагрузка, GPU/память/темп, RAM, диск тип/свободно, Windows build, адаптер/скорость).
- **M7 (Центр твиков):** проверить и довести: поиск по названию+подписи, риск-бейджи (зелёный/жёлтый/
  красный), статус (ON/OFF/неприменимо+почему), кнопки Применить/Откатить, счётчики групп.
  Все новые твики M1/M2 появляются автоматически.
- **M8 (дизайн-пасс, «бомба-пушка»):** единая тема по Core\Themes\Theme.cs: фон #0B0E13,
  карточки #12161D, бордер #1E2633, акцент #4CC2FF, успех #4ADE80, warning #FBBF24, danger #F87171;
  сетка 8px, радиус 12; RingGauge для HealthScore/RAM/диска; Toast-уведомления; статус-иконки;
  все 9 вкладок привести к единому стилю; тексты — простые, русские.
- **M9 (финал, выполнять ПОСЛЕ M10–M12):** CLI `/doctor` (железо + применимость твиков +
  HealthScore, без GUI); apply/revert-тест каждого НОВОГО твика (с бэкапом); сборка 0/0;
  `/tweaks` exit 0; GUI-запуск; выкладка в reliz; **WHATSNEW-1.0.txt** (обновить); скриншоты
  вкладок (PowerShell `Graphics.CopyFromScreen` при запущенном приложении) → в reliz/скриншоты.

## I+. МАКСИМУМ: что ещё добавляем (добивка до идеала, v2)

### ПК / FPS
- **BIOS-советник (только советы, ничего не пишет в BIOS) — карточки на Главной:**
  - **XMP/EXPO-чек:** `Win32_PhysicalMemory.Speed` (текущая) против JEDEC-базы (DDR4 < 2133,
    DDR5 < 4800) → «Память, похоже, работает не на своей частоте. Включи XMP/EXPO в BIOS —
    это +10–15% FPS». (Подтверждено: Windows отдаёт фактическую и номинальную скорость.)
  - **ReBAR/Resizable BAR:** если GPU поддерживает (NVIDIA Ampere+/AMD RX6000+) — чеклист.
  - **Чеклист BIOS** простым языком: CSM off, Above 4G Decoding, PCIe Gen Auto, XMP.
- **Дисплей: максим. частота обновления** — P/Invoke `EnumDisplaySettings`/
  `ChangeDisplaySettingsEx` (в `Core\WinApi.cs`): текущий Hz против максимума для резолушена;
  если меньше — карточка «Монитор работает на 60 из 144 Гц» + кнопка «Включить 144 Гц»
  (обратимо, только дисплей).
- **Визуальные эффекты: «быстро для работы»** — `UserPreferenceMask`/`UserVisualUXSetting`
  (один твик поверх ui-anim/ui-transparency, в категорию «Система»).
- **Менеджер автозагрузки** — HKLM/HKCU Run + RunOnce + задачи автозагрузки: список с
  тумблерами (выкл = Safe, обратимо) + «что тормозит запуск».
- **Топ-5 DPC-проблемников** (Монитор) — PDH-счётчик `Process\DPC Writes`: кто именно
  (драйвер/процесс) колет фреймы.
- **Здоровье SSD/NVMe** (Главная) — WMI: износ, температура, TRIM работает.
- **G-Sync/FreeSync-чек** — статус из реестра NVIDIA, если выкл — совет (не авто-включение,
  конфигурация индивидуальная).
- **Пауза обновлений Windows** — кнопка «Приостановить на 1 неделю» (без вечерних рестартов), Safe.

### Мышь / клавиатура
- **«Игровой режим Win-клавиши» — подсказка** (вкладка Мышь и клава): отключается в софте
  конкретного устройства (Razer/Logitech/Corsair…) — текст с инструкцией.
- **Тень/шлейф курсора off** — мелкий твик визуалки.
- Тест задержки + polling rate — как M4.

### Сеть
- **LRO off (на адаптер, эксперимент)** — вендор-специфичные значения под `NetCfgInstanceId`;
  только при опознанном классе адаптера, тег «экспериментально», обязательно Revert.
  (Intel community: offload/комбинация пакетов = +латентность в играх.)
- **Wi-Fi: 5/6 ГГц + Transmit Power = Max** — совет + вендор-реестр при наличии ключа.

### Идеальность интерфейса (то, что отделяет «имбще» от «нормально»)
- **PREVIEW (dry run) перед применением профиля** — ключевая фича доверия:
  «Будет применено: 42 твика (38 безопасно, 3 осторожно, 1 риск)» + раскрываемый список
  с подписями → кнопка «Применить».
- **«Точки восстановления»** (в Настройках): список бэкапов (дата, профиль, состав) +
  «Восстановить этот» / «Вернуть ВСЁ».
- **Экспорт отчёта** — одной кнопкой txt: железо, оценка, все твики ON/OFF, тесты, дата.
  Для шары в поддержку/друзьям.
- **«Полная диагностика» на Главной** — скан + сетевой тест + тест ввода → отчёт с
  топ-ограничителями и кнопками «Исправить».
- **Онбординг при первом запуске** (30 секунд): «Твоя цель? Игры / Работа / Просто чистка»
  → рекомендованные действия.
- **Вкладка «Настройки» (маленькая):** акцент, показ логов, авто-бэкап перед каждым
  применением, запоминание размера/позиции окна.
- **High-DPI** (manifest + AutoScaleDpi), мин. размер окна 1200×750.
- **Кнопка «Объяснить»** на карточке твика: расширенное описание простым языком +
  «что будет при откате».
- **Никаких тихих ошибок:** любая ошибка = тост + запись в лог + «что делать».
- **Прогресс-бар** в статус-баре для долгих операций (чистка, профили, тесты).

## J. ЦЕЛИ ОЧИСТИТЕЛЯ (M3) — 16 шт.
| key | Название | Путь/действие | NeedsConfirm |
|---|---|---|---|
| temp | Временные файлы пользователя | %TEMP% (всех профилей) | нет |
| wtemp | Временные файлы Windows | C:\Windows\Temp | нет (admin) |
| prefetch | Prefetch | C:\Windows\Prefetch | нет |
| update-cache | Кэш скачанных обновлений | C:\Windows\SoftwareDistribution\Download | нет |
| crashdumps | Дампы падений | C:\Windows\MEMORY.DMP, C:\Windows\Minidump, %LocalAppData%\CrashDumps | нет (может быть ГБ) |
| wer | Отчёты об ошибках WER | C:\ProgramData\Microsoft\Windows\WER\* | нет |
| logs | Старые логи Windows | C:\Windows\Logs (кроме CBS), C:\Windows\Panther | нет |
| delivery | Delivery Optimization | C:\Windows\SoftwareDistribution\DeliveryOptimization | нет |
| browsers | Кэш браузеров | Chrome/Edge/Firefox/Opera — **только** Cache/Code Cache/Service Worker | нет (никогда профили!) |
| thumbs | Кэш миниатюр | %LocalAppData%\Microsoft\Windows\Explorer\thumbcache_* | нет |
| inetc | Кэш Windows/Internet | %LocalAppData%\Microsoft\Windows\INetCache | нет |
| cbstemp | CBS-временные | C:\Windows\CbsTemp | нет (admin) |
| winbt | Остатки обновления Windows | C:\$WINDOWS.~BT | нет (admin) |
| etl | Старые трассировки | %TEMP%\*.etl*, C:\Windows\Temp\*.etl | нет |
| dns / recycle / winsxs / winold | (уже есть) | | winsxs+winold — ДА |
| hiberfile | Отключить файл гибернации | powercfg /h off (~размер ОЗУ ГБ) — отдельный тумблер | ДА (Caution) |

Группы в UI: «Мелкий мусор» (temp,wtemp,prefetch,etl,inetc,cbstemp) · «Обновления»
(update-cache,delivery,winbt,winsxs,winold) · «Приложения» (browsers,thumbs,wer,crashdumps) ·
«Система» (recycle,dns,hiberfile). Кнопка «Очистить всё безопасное».

## K. ИСТОЧНИКИ ИССЛЕДОВАНИЯ (проверено 09.10.2026)
- Windows 11 gaming optimization 2025/2026: switchbladegaming.com, frozentweaks.com,
  windowscentral.com (top-21), windowsforum.com, windowscentral (6 registry tweaks).
- Input lag: отключение Filter Keys (Settings→Accessibility→Keyboard), KeyboardResponse/
  KeyboardDelay (harper29, MS Q&A 4308260, reddit).
- Сеть/пинг: EEE «Energy Efficient Ethernet» off (reddit xboxone tech), netsh TCP-флаги
  (speedguide.net TCP/IP Tweaks #5077, SG-Gaming-Tweaks), NIC power management.
- Очистка: WER/crashdumps/delivery/temp (windowsforum 2025, MS Q&A).
- HAGS: универсальной пользы нет, в Win11 обычно ON → делаем тумблер + бэнч (reddit
  buildapc, Acer blog 2026, techpowerup, steamcommunity 2025).
- XMP/EXPO: фактическая vs номинальная скорость памяти доступны через WMI
  (rankedram.com 2026, tech-insider.org 2026).
- LRO/offload и EEE на уровне адаптера: Intel community (NIC settings low latency),
  overclock.net (латентность при offload/комбинации пакетов).
- Частота обновления: EnumDisplaySettings/ChangeDisplaySettingsEx (learn.microsoft.com,
  Win32 API) — подтверждённый механизм.
- Все механизмы перед написанием кода перепроверять по MSDN/Microsoft Learn; каждый твик —
  только один проверенный механизм (реестр ИЛИ powercfg ИЛИ netsh, без дублей).

## L. СТАТУС КОДА (обновлено 09.10.2026)

Что уже в коде (не дублировать):
- Каталог 96 твиков (TweakCatalog + TweakCatalogSystem), каждый с детектором,
  бэкапом и откатом; GUI и CLI работают через один движок ProfileRunner
  (старый ProfileEngine удалён, лежит в корзине Windows).
- Профили: Безопасный / Баланс / Экстрим / Вернуть к заводским +
  ProfileRunner.Preview (dry-run) + кнопка «ПРЕДПРОСМОТР» на вкладке Профили.
  Запуск профилей без прав администратора заблокирован (частичное применение
  исключено).
- /doctor в CLI: железо + HealthScore + применимость каталога.
- Сеть: тест LAN→DNS→интернет со сравнением ДО/ПОСЛЕ (NetworkControl,
  NetworkDiagnostics.RunSuite).
- Главная: HealthScore с ТОП-3 ограничителями и кнопкой «Исправить».
- Вкладка «Мышь и клава» (InputControl) с тестом очереди событий.
- Очиститель: 19 целей, безопасные значения по умолчанию (prefetch и D3DSCache —
  выключены по умолчанию, корзина — отдельное подтверждение, Service Worker из
  браузерной очистки исключён).

Исправлено при пересборке:
- net-eee-off теперь пишет в под-ключи устройств
  Class\{4d36e972-...}\NNNN, где драйвер знает EnergyEfficientEthernet;
  корневой Class-ключ не работал.
- Автозагрузка (sys-autostart-cleanup): обобщённое правило IsJunkStartup
  без хардкода GUID конкретного пользователя.
- Убран мёртвый опасный код (bcdedit-флаги, VBS-блоки) — все его действия
  уже существуют как отдельные контролируемые твики каталога.
- HealthScore: убраны гарантированные цифры потерь («5-15%», «до 8%»);
  спорные пункты (HAGS/VBS/питание) требуют бенчмарка.

Осознанно не сделано:
- «Тумблер» частоты монитора в BIOS-уровне (только чтение через WMI).
- Очистка Windows.old в корзину (размер ~размер системы, корзина не резиновая) —
  остаётся DISM/ручной с подтверждением.
