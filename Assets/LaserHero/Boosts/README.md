# Постоянные улучшения лазера

В сцене Lobby: `Permanent Laser Boosts`, две независимые редактируемые площадки справа от центральной дорожки. Компонент `LaserBoostTrigger` позволяет выбрать бонус через enum. При входе открывается IAP один раз, после отмены повторный вход доступен после выхода с площадки. Купленная площадка полностью скрывается во время игры: подиум, иконка, надписи и коллайдеры. Это также применяется при загрузке сохранения и восстановлении покупки.

Каталог: `F:/LaserHero/Assets/LaserHero/Settings/GooglePlayCatalog.asset`. Два товара `Boost`:

| Index | Бонус | Product ID | Preview Price |
| --- | --- | --- | --- |
| 0 | Размер лазера ×2 | laser_hero_double_laser_size | 3.99 |
| 1 | Скорость ударов ×2 | laser_hero_double_hit_speed | 2.99 |

Оба товара — NonConsumable. В Editor работает Unity Fake Store. Для реальных покупок нужно создать эти товары в Google Play Console и завершить существующее подключение приложения/лицензионного ключа. На устройстве цена берётся из магазина; Preview Price используется для предпросмотра и тестового магазина. Числа отображаются без значка валюты.

`LaserSave.ownedBoosts` сохраняет покупки. Размер применяется после ограничения базового радиуса, а визуальная ширина луча также удваивается. Скорость делит текущий интервал лазерных ударов на два. Множители сочетаются с обычными улучшениями и супер-ударом. Повторная доставка/восстановление покупки не увеличивает множитель повторно. Перерождение сохраняет оба бонуса.

## Сгенерированные иконки

Built-in `image_gen`, прозрачный фон; оригиналы сохранены, копии импортированы как Sprite.

- `F:/LaserHero/Assets/LaserHero/Boosts/DoubleLaserSize.png`
- `F:/LaserHero/Assets/LaserHero/Boosts/DoubleHitSpeed.png`

### Final prompt: размер

Use case: stylized-concept. Asset type: transparent Unity game upgrade icon. Primary request: a bold cartoon laser size upgrade icon, a bright horizontal cyan laser beam with a brilliant white center and short blue ends, crossed in front by a large vertical white double-headed arrow pointing up and down. Thick near-black outline around arrow, thin cyan outer glow. Clean simple Roblox-style game symbol, no text, no numbers, no platform, no scene. Centered square composition with generous transparent padding, sharp readable silhouette, genuinely transparent background.

### Final prompt: скорость

Use case: stylized-concept. Asset type: transparent Unity game upgrade icon matching bold cartoon game symbols. Primary request: laser attack speed icon, exactly three large right-pointing chevrons in a row >>>. White chevron interiors, strong red inner edging, thick near-black outlines, vivid red glow and short horizontal red laser speed streak behind. No words, no numbers, no platform, no background scene. Centered square canvas, symbol occupies middle horizontal band, generous genuinely transparent padding. Simple sharp clean Roblox-style game icon.
