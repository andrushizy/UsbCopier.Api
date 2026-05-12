# UsbCopier.Api

REST API для UsbCopier. Хранит профили бэкапа, известные флешки, историю запусков. Полностью независимый продукт — может работать на отдельном сервере.

## Стек
- ASP.NET Core 8 Web API
- EF Core 8 + Pomelo MySQL
- Swagger / Swashbuckle

## Запуск

1. **БД**. Откройте phpMyAdmin → «Импорт» → выберите `database/schema.sql` → импортируйте. Будет создана БД `usbcopier` с таблицами и одним профилем «По умолчанию».

2. **API**. Откройте `UsbCopier.Api.sln` в Visual Studio и нажмите F5. По умолчанию слушает `http://localhost:5000`.

3. **Swagger** доступен на `http://localhost:5000/swagger`.

## Connection string

В `appsettings.json` строка подключения настроена для **MySQL без пароля** (как в XAMPP по умолчанию):

```
Server=localhost;Port=3306;Database=usbcopier;User=root;Password=;
```

Если у вас MySQL с паролем — поменяйте поле `Password=`.

## Эндпоинты

```
GET    /api/profiles               список профилей
GET    /api/profiles/{id}          конкретный профиль с категориями и расписанием
POST   /api/profiles               создать профиль
PUT    /api/profiles/{id}          обновить
DELETE /api/profiles/{id}          удалить (нельзя удалить последний)

GET    /api/devices                список знакомых флешек
POST   /api/devices                upsert по (volumeSerial, volumeLabel)
DELETE /api/devices/{id}           удалить

GET    /api/history?profileId=&take=   история бэкапов
POST   /api/history                записать результат запуска

GET    /api/health                 проверка API + соединения с БД
```
