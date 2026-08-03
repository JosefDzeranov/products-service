# products-service

Каталог товаров на ASP.NET Core. Хранит товары в PostgreSQL, кэширует список в Redis и за отзывами
к товару ходит по сети в соседний сервис `reviews-service`. Один из сервисов микросервисной системы
курса PRO C#. Docker. Собирает карточку товара из двух источников, своих данных и чужого сервиса.

## Что умеет

- отдать весь каталог товаров (с кэшем в Redis)
- отдать один товар вместе с его отзывами (отзывы приходят из reviews-service по HTTP)
- добавить товар

## Как products-service обращается к reviews-service

В `Program.cs` регистрируется типизированный HTTP-клиент. Его базовый адрес задается переменной
окружения `ReviewsService__Url`, внутри системы это `http://reviews-service:8080`. Двойное
подчеркивание ASP.NET читает как двоеточие, то есть `ReviewsService:Url` в конфигурации. За списком
отзывов клиент дергает `GET /reviews/{productId}` у соседнего сервиса. Если reviews-service временно
недоступен, товар все равно отдается, просто без отзывов (graceful degradation).

## Структура проекта

- `Program.cs` точка входа, подключение базы, Redis, HTTP-клиента к reviews-service, создание таблиц
- `Models/` модели данных (Product, CreateProductRequest, ReviewDto, ProductDetails)
- `Data/ProductDbContext.cs` контекст EF Core, одна таблица товаров
- `Services/` работа с базой (IProductRepository) и клиент к сервису отзывов (IReviewsClient)
- `Controllers/ProductsController.cs` HTTP-эндпоинты
- `Dockerfile` многостадийная сборка образа
- `.github/workflows/ci.yml` сборка образа в GHCR и выкатка своего контейнера на сервер

## Переменные окружения

- `ConnectionStrings__Postgres` — строка подключения к своей базе `products`
- `Redis__Connection` — адрес Redis (`redis:6379`)
- `ReviewsService__Url` — адрес сервиса отзывов (`http://reviews-service:8080`)

Задаются они не здесь, а в топологии системы. См. репозиторий **`dockercourse-deploy`** — там
`docker-compose.yml` всей системы (db + redis + reviews-service + products-service + nginx) и
описание, как все поднимается и катится.

## Как катится

Пуш в этот репозиторий собирает образ `ghcr.io/josefdzeranov/products-service:latest`, пушит его в
GHCR и обновляет на сервере ТОЛЬКО контейнер products-service (`docker compose up -d --no-deps
products-service`). reviews-service, db и redis при этом не трогаются. Свой образ у каждого сервиса —
поэтому они выкатываются независимо друг от друга.

## Локальный запуск

Сервис зависит от базы, Redis и reviews-service, поэтому в одиночку не запускается. Чтобы поднять всю
систему локально, возьми `docker-compose.yml` из репозитория `dockercourse-deploy`.

Через `dotnet run` (нужны свои база, Redis и запущенный reviews-service) задай адреса переменными
окружения или в `appsettings.json` (`Host=localhost`, `Redis:Connection=localhost:6379`,
`ReviewsService:Url` на запущенный reviews-service).

```
dotnet run
```
