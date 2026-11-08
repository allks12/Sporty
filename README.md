# Sporty - Уеб приложение за организиране на спортни игри

„Sporty“ е уеб платформа, в която потребителите създават групи за любителски спорт с дата, час, място и минимален брой участници, а други се включват в тях. Системата извършва автоматично потвърждение или отмяна на група в зависимост от записаните хора до 24 часа преди събитието.

## Използвани технологии
* **Бекенд и Фронтенд:** C# / .NET 10, ASP.NET Core MVC[cite: 1]
* **Сигурност и Роли:** ASP.NET Core Identity[cite: 1]
* **Тестване:** xUnit[cite: 1]
* **Интерфейс:** HTML, CSS, JavaScript, Bootstrap[cite: 1]

## Инструкции за стартиране

### Изисквания
* Инсталиран [.NET 10 SDK](https://dotnet.microsoft.com/)[cite: 1]

### 1. Стартиране на уеб приложението
За да стартирате уеб частта на проекта локално, отворете терминал в главната директория на хранилището и изпълнете:
```bash
dotnet run --project Sporty.Web

Настройка на базата данни

Sporty използва PostgreSQL като система за управление на бази данни и Entity Framework Core за управление на схемата и миграциите.

Необходими предварителни инсталации

Преди да стартирате проекта, уверете се, че имате инсталирани:

Docker и Docker Compose

.NET 10 SDK

Entity Framework Core CLI (dotnet-ef)

1. Конфигуриране на паролата за базата данни

В основната директория на проекта създайте файл с име .env, на същото ниво като compose.yaml.

Добавете следния ред:

POSTGRES_PASSWORD=your_password

Заменете your_password с избрана от вас парола.

Файлът .env се използва от Docker Compose за задаване на паролата на PostgreSQL потребителя.

Важно: Не качвайте .env файла в Git. Той съдържа поверителни данни.

2. Стартиране на PostgreSQL базата данни

От основната директория на проекта изпълнете:

docker compose up -d

3. Конфигуриране на връзката към базата данни

Задайте connection string-а DefaultConnection чрез променлива на средата:

export ConnectionStrings__DefaultConnection="Host=localhost;Port=5433;Database=sportyDB;Username=sporty_user;Password=your_password"

Заменете your_password със същата парола, която сте задали във файла .env.

Тази променлива позволява на приложението да се свърже с PostgreSQL базата данни.

4. Прилагане на миграциите

От основната директория на проекта изпълнете:

dotnet ef database update \
    --project Sporty.Data \
    --startup-project Sporty.Web \
    --context SportyDbContext

6. Спиране на базата данни

Когато приключите работа, можете да спрете PostgreSQL контейнера със:

docker compose down