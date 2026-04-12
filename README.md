<h1 align="center">Mini Macro</h1>

<p align="center">
  Лёгкая программа для записи и воспроизведения макросов клавиатуры и мыши под Windows.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/C%23-.NET%2010-512BD4?style=flat-square" alt="C# .NET 10">
  <img src="https://img.shields.io/badge/WPF-Windows-0078D4?style=flat-square" alt="WPF">
  <img src="https://img.shields.io/badge/лицензия-MIT-1ED895?style=flat-square" alt="MIT">
</p>

---

## Возможности

- **Запись** — захват событий клавиатуры и мыши с точностью до временных меток
- **Воспроизведение** — точное повторение записанных действий
- **Библиотека макросов** — папка с `.mmacro` файлами, горячие клавиши, управление записями
- **Настройки** — количество повторов (до 50), скорость воспроизведения (до 5x)
- **Сохранение / загрузка** — собственный формат `.mmacro`

## Версии

| Версия  | Платформа              | ОС             |
|---------|------------------------|----------------|
| Modern  | .NET 10.0              | Windows 10/11  |
| Legacy  | .NET Framework 4.8     | Windows 7 SP1+ |

## Сборка

**Требования:** .NET 10.0 SDK (или .NET Framework 4.8), Windows

```bash
git clone https://github.com/NoVate911/Mini-Macro.git
cd "Mini Macro"

# Modern (.NET 10.0, Windows 10/11)
dotnet build -f net10.0-windows -c Release

# Legacy (.NET Framework 4.8, Windows 7 SP1+)
dotnet build -f net48 -c Release
```

Исполняемые файлы появятся в `bin\Release\net10.0-windows\` и `bin\Release\net48\` соответственно.

## Использование

1. Нажмите **Запись** — выполните нужные действия
2. Нажмите **Стоп** — запись сохраняется
3. Нажмите **Воспроизвести** — макрос повторяется

Для сохранения: **Файл → Сохранить** (`.mmacro`).  
Для библиотеки: укажите папку в **Настройках**.

## Лицензия

[MIT](LICENSE.txt) © NoVate Source
