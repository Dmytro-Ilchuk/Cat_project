# NineLives

Студентська 2D гра про кота з дев’ятьма життями. Поточний етап: v0.0.1 — запуск Bootstrap → MainMenu і кнопка виходу. Ігрових механік ще немає.

## Відкрити проєкт

1. Клонувати цей репозиторій через Git / GitHub Desktop.
2. У Unity Hub додати кореневу папку репозиторію. Назва папки `Cat_project` не заважає: назва продукту в Unity — `NineLives`.
3. Використовувати **Unity 6000.6.1f1**. Це погоджена версія всієї команди; попередню пропозицію 6.3 LTS скасовано. Не відкривати проєкт старішою версією.
4. Дочекатися імпорту пакетів. Windows x64 збираємо через **Mono**; IL2CPP та його C++ toolchain на цьому етапі не потрібні.
5. Відкрити `Assets/_Game/Scenes/Bootstrap.unity` і натиснути Play. Має з’явитися MainMenu. «Вийти» закриває гру; в Editor — зупиняє Play Mode. Кнопка працює мишею та через Enter / gamepad Submit.

## Збірка Windows

У File → Build Profiles вибрати **Windows Development**, натиснути Switch Profile, якщо профіль не активний. Development Build і Script Debugging увімкнені. Сцени: Bootstrap, потім MainMenu.

Збирати в `Builds/WindowsDevelopment/NineLives.exe`. Передавати іншому учаснику **всю папку збірки**, включно з `NineLives_Data`, `UnityPlayer.dll` та рештою файлів. Unity Editor для запуску готової гри не потрібний.

Через підключений Unity CLI:

```powershell
unity command build --target StandaloneWindows64 --profileName "Windows Development" --outputPath Builds/WindowsDevelopment/NineLives.exe --options '["Development","AllowDebugging","DetailedBuildReport"]' --confirm true
unity command build_status
```

## Перевірка

- Меню **NineLives → Validate Stage 1** перевіряє сцени, URP 2D, ввід, шари, матрицю зіткнень і кнопку виходу. Або `unity command eval 'NineLives.Editor.StageOneValidation.Validate();'`.
- Запустити Bootstrap в Editor: з’являється MainMenu без помилок у Console; «Вийти» завершує Play Mode.
- Запустити Windows-збірку й перевірити кнопку виходу.
- Учасник Б окремо перевіряє чистий clone та готову збірку на своєму ПК. Цю перевірку не замінює локальний тест.

## Структура та пакети

- `Assets/_Game/Scenes` — Bootstrap, MainMenu.
- `Assets/_Game/Scripts` — ігровий код; початкові папки систем із плану.
- `Assets/_Game/Settings/NineLives.inputactions` — Gameplay/UI.
- `Assets/_Game/Editor` — перевірка етапу 1, не потрапляє в гру.
- `Assets/_Game/Art`, `Audio`, `Data`, `Prefabs` — майбутній контент.
- `Assets/Settings` — налаштування URP, 2D Renderer і Build Profile.
- `Docs` — дизайн і план розробки.

Точні версії зафіксовано в `Packages/manifest.json` та `Packages/packages-lock.json`: URP 17.6.0, Input System 1.20.0, uGUI 2.6.0, Test Framework 1.8.0, Pipeline 0.7.0-exp.1. Потрібні для етапу пакети вже встановлені. Нових залежностей не додано. Просте меню використовує uGUI Text зі штатним шрифтом; фінальне оформлення — пізніше.

У Renderer2D вимкнено **Post-processing → Enabled**, оскільки ефекти поки не використовуються. Увімкнути, коли з’явиться потреба в ефектах Volume. CLI працює з Editor; runtime Pipeline у готовій грі не налаштований. Єдине попередження збірки про відсутній RuntimePipelineConfig очікуване.

## Ввід

| Gameplay | Клавіатура / миша | Gamepad |
| --- | --- | --- |
| Move | WASD / стрілки | Left stick / D-pad |
| Jump | Space | South |
| Attack | Ліва кнопка миші | West |
| Dash | Left Shift | Right shoulder |
| Hiss | Q | North |
| Interact | E | East |
| Pause | Escape | Start |

Gameplay — підготовлені прив’язки для наступних етапів; у меню вони не керують грою. UI використовує окрему карту із шаблону Input System.

## Фізика 2D

Шари 8–13: Ground, Player, Enemy, PlayerHitbox, EnemyHitbox, Interactable. Увімкнені лише такі пари за участю цих шарів:

- Ground ↔ Player, Enemy.
- Player ↔ EnemyHitbox, Interactable.
- Enemy ↔ PlayerHitbox.

Player ↔ Enemy вимкнено: тіла не штовхають одне одного. Hitbox та Interactable надалі отримають trigger-колайдери; сама матриця шкоду не реалізує. Наявні стандартні шари між собою не змінено.

## Git

Зберігати `Assets` разом із `.meta`, `Packages`, `ProjectSettings`, `Docs` і README. Порожні початкові папки збережені в Git через `.gitkeep`. Ресурси переміщувати через Editor або разом із `.meta`.

Увімкнено Visible Meta Files і Force Text. `.gitignore` виключає Library, Temp, Logs, Obj, UserSettings, Build/Builds та згенеровані файли IDE. Збірки передавати окремо, не додавати в Git. Підключення GitHub-плагіна та авторизація локального Git — окремі способи доступу.
