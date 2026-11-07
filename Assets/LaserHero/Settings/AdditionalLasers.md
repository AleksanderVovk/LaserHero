# Дополнительные товары магазина лазеров

Новые индексы 6–13 добавлены после прежних шести лазеров. Старые биты владения и индексы экипировки сохраняются. Карточки находятся в сцене Lobby: HUD Canvas / Laser shop / Scroll / Viewport / Content (объекты Laser 6–Laser 13).

| Товар | Индекс | Игровая цена | Базовая энергия за тик | IAP previewPrice | Product ID |
|---|---:|---:|---:|---:|---|
| Белый огонь | 6 | 6 600 000 | 7 000 | 12.99 | laser_hero_laser_tier_7_white |
| Оранжевый огонь | 7 | 33 000 000 | 35 000 | 14.99 | laser_hero_laser_tier_8_orange |
| Золотистый огонь | 8 | 330 000 000 | 170 000 | 17.99 | laser_hero_laser_tier_9_gold |
| Голубой огонь | 9 | 2 600 000 000 | 800 000 | 19.99 | laser_hero_laser_tier_10_cyan |
| Индиговый огонь | 10 | 4 100 000 000 | 3 800 000 | 24.99 | laser_hero_laser_tier_11_indigo |
| Изумрудный огонь | 11 | 7 700 000 000 | 21 600 000 | 29.99 | laser_hero_laser_tier_12_emerald |
| Малиновый огонь | 12 | 18 200 000 000 | 102 000 000 | 39.99 | laser_hero_laser_tier_13_crimson |
| Серебристый огонь | 13 | 40 600 000 000 | 480 000 000 | 49.99 | laser_hero_laser_tier_14_silver |

Все IAP — постоянные NonConsumable. Каталог: GooglePlayCatalog.asset в этой папке. Editor и Windows используют существующий Fake Store; реальная цена Android загружается из Google Play. Для настоящих платежей нужно создать эти Product ID в Google Play Console, назначить цены и заполнить публичный ключ каталога согласно GooglePlaySetup.md. Товары в консоли Google Play автоматически не создавались.

Names, Colors, Prices, BaseDamage и EnergyGain находятся в LaserProgress.cs. Видимая мощность «+N» — базовая энергия за тик EnergyGain; на фактический прирост также влияют перерождения, аура и тренажёр. Урон разрушения задаётся отдельным BaseDamage, как у прежних лазеров.
