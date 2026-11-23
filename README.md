# Sporty - Уеб приложение за организиране на спортни игри

„Sporty“ е уеб платформа, в която потребителите създават групи за любителски спорт с дата, час, място и минимален брой участници, а други се включват в тях. Системата извършва автоматично потвърждение или отмяна на група в зависимост от записаните хора до 24 часа преди събитието.

## Използвани технологии
* **Бекенд и Фронтенд:** C# / .NET 10, ASP.NET Core MVC
* **Сигурност и Роли:** ASP.NET Core Identity
* **Тестване:** xUnit
* **Интерфейс:** HTML, CSS, JavaScript, Bootstrap
* **Външни услуги:** Nominatim API (Геокодиране) & OpenWeatherMap API (Метеорологична прогноза с кеширане)

## Инструкции за стартиране

### Изисквания
* Инсталиран [.NET 10 SDK](https://dotnet.microsoft.com/)
* Docker и Docker Compose
* Entity Framework Core CLI (`dotnet-ef`)

### 1. Стартиране на уеб приложението
За да стартирате уеб частта на проекта локално, отворете терминал в главната директория на хранилището и изпълнете:
```bash
dotnet run --project Sporty.Web
```

---

### 2. Конфигуриране на базата данни (PostgreSQL)
Sporty използва PostgreSQL като система за управление на бази данни и Entity Framework Core за управление на схемата и миграциите.

1. **Създаване на `.env` файл**
В основната директория на проекта създайте файл с име `.env` (на същото ниво като `compose.yaml`).
Добавете следния ред:
```env
POSTGRES_PASSWORD=your_password
```

*(Заменете `your_password` с избрана от вас парола.)*

**Важно:** Не качвайте .env файла в Git. Той съдържа поверителни данни.

2. **Стартиране на PostgreSQL контейнера**

От основната директория на проекта изпълнете:
```bash
docker compose up -d
```

3. **Конфигуриране на връзката към базата данни**

Задайте connection string-а `DefaultConnection` чрез променлива на средата:
```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=sportyDB;Username=sporty_user;Password=your_password"
```
*(Заменете `your_password` със същата парола, която сте задали във файла `.env`)*

---

### 3. Конфигуриране на OpenWeatherMap API ключ (User Secrets)
За да работи интеграцията за реално време и прогнозата за времето спрямо часа на събитието, приложението използва .NET User Secrets:

1. Отворете терминал в папката на уеб проекта:
```bash
cd Sporty.Web
```

2. Инициализирайте User Secrets:
```bash
dotnet user-secrets init
```

3. Задайте вашия личен API ключ:
```bash
dotnet user-secrets set "OpenWeatherMap:ApiKey" "ВАШИЯТ_API_КЛЮЧ_ТУК"
```
*(Ако все още нямате ключ, можете да си генерирате напълно безплатно, като се регистрирате на [OpenWeatherMap](https://openweathermap.org/) и посетите секцията **My API Keys** във вашия профил).*

---

### 4. Прилагане на миграциите

От основната директория на проекта изпълнете:
```bash
dotnet ef database update \
    --project Sporty.Data \
    --startup-project Sporty.Web \
    --context SportyDbContext
```

---

### 5. Администратор и демо потребител

При стартиране приложението създава ролите `User` и `Admin`, както и два потребителя, с които може да се влезе веднага:

* **Администратор**: `admin@sporty.bg` / `Admin123!`
* **Демо потребител**: `user@sporty.bg` / `User123!`

---

### 6. Спиране на базата данни

Когато приключите работа, можете да спрете PostgreSQL контейнера със:
```bash
docker compose down
```