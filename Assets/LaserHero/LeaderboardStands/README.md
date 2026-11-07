# Стенды лидербордов

В сцене: `LaserHeroWorld/Safe LEGO Lobby/Leaderboard Stands`.

Три независимых prefab: `LeaderboardPower`, `LeaderboardMoney`, `LeaderboardPlaytime`. Перемещайте и вращайте корень отдельного стенда. World Space Canvas масштабирован 0.01; вместе с рамкой он перемещается целиком.

Внутри каждого: `WorldCanvas/Rows/Row01` … `Row07`. Строки содержат обычные TMP-тексты `Rank`, `PlayerName`, `Record`. Это тестовые данные; их можно менять вручную или позже заполнять из своего лидерборда. Заголовки — `WorldCanvas/Header/Title`, имена колонок — `WorldCanvas/Columns`.

Сетевых вызовов, получения рекордов и игровых обработчиков нет. Canvas предназначен для отображения и не перехватывает ввод. Исходная сцена до добавления стендов сохранена в `Assets/Scenes/Backups/Lobby_BeforeLeaderboardStands.unity`.
